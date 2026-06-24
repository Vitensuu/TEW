using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Dungeon; // DungeonGrid, TileType — общая логическая сетка + A* врагов

namespace Dungeon.Procedural
{
    /// <summary>
    /// Оркестратор тайловой процедурной генерации (Soul Knight-style).
    ///
    /// КОНВЕЙЕР (см. отдельные классы):
    ///   1. Room Placement  — случайные непересекающиеся комнаты (RectInt).
    ///   2. Graph + MST     — связываем центры комнат минимальным остовом,
    ///                        затем добавляем несколько случайных рёбер (циклы).
    ///   3. Corridor Gen    — Г-образные коридоры ширины ≥2 между дверями.
    ///   4. Logical grid    — вырезаем пол комнат+коридоров, строим кольцо стен.
    ///   5. Validator (BFS) — каждая комната и каждая дверь достижимы из старта;
    ///                        иначе — перегенерация (до maxRegenAttempts).
    ///   6. TilemapPainter  — рисует Floor/Wall (двери и коридоры — это пол,
    ///                        стена туда не ставится → проходы всегда открыты).
    ///   7. CollisionBuilder— TilemapCollider2D+Composite на стенах, пол без коллизий.
    ///   8. Place start/exit— игрок в старте, выход в самой дальней комнате (босс).
    ///
    /// Публичная поверхность совместима с EnemyAI / RoomPopulator / NextFloorTrigger:
    /// GetGrid, GetRooms, WorldToCell, CellToWorldCenter, GenerateNextFloor,
    /// FloorNumber, StartWorldPosition, ExitWorldPosition.
    /// </summary>
    public class DungeonGenerator : MonoBehaviour
    {
        [Header("Tilemaps (Grid ▸ Floor/Wall/Collision/Decoration)")]
        [SerializeField] Tilemap floorTilemap;
        [SerializeField] Tilemap wallTilemap;
        [SerializeField] Tilemap collisionTilemap;   // необязательно
        [SerializeField] Tilemap decorationTilemap;  // необязательно

        [Header("Тайлы")]
        [SerializeField] DungeonTileSet tileSet;
        [Tooltip("Невидимый тайл для отдельного слоя коллизий (необязательно)")]
        [SerializeField] TileBase collisionTile;

        [Header("Размер карты (клетки)")]
        [SerializeField] int mapWidth  = 80;
        [SerializeField] int mapHeight = 80;
        [Tooltip("Запас от краёв карты под кольцо стен")]
        [SerializeField] int mapPadding = 3;

        [Header("Комнаты")]
        [SerializeField] int minRooms = 8;
        [SerializeField] int maxRooms = 14;
        [SerializeField] int minRoomSize = 7;
        [SerializeField] int maxRoomSize = 14;
        [Tooltip("Минимальный зазор между комнатами (клетки), ≥2 для стен")]
        [SerializeField] int roomSpacing = 2;
        [SerializeField] int roomPlacementAttempts = 80;

        [Header("Коридоры")]
        [Tooltip("Ширина прохода в клетках (минимум 2 по требованиям)")]
        [SerializeField] int corridorWidth = 2;
        [Tooltip("Шанс добавить лишнее соединение сверх MST (циклы)")]
        [Range(0f, 1f)] [SerializeField] float extraConnectionChance = 0.15f;

        [Header("Типы комнат (веса для не-старт/не-босс)")]
        [Range(0f, 1f)] [SerializeField] float treasureChance = 0.12f;
        [Range(0f, 1f)] [SerializeField] float shopChance     = 0.10f;
        [Range(0f, 1f)] [SerializeField] float eliteChance    = 0.15f;

        [Header("Надёжность")]
        [SerializeField] int maxRegenAttempts = 20;

        [Header("Сид")]
        [SerializeField] bool useRandomSeed = true;
        [SerializeField] int  seed;

        [Header("Игрок и выход")]
        [SerializeField] Transform  player;
        [SerializeField] GameObject exitPrefab;

        [Header("Запуск")]
        [SerializeField] bool generateOnStart = true;

        // ── Состояние ────────────────────────────────────────────────────────
        System.Random _rng;
        DungeonGrid   _grid;
        Vector2Int    _gridOrigin = Vector2Int.zero; // мировая клетка для индекса (0,0)
        GameObject    _spawnedExit;

        readonly List<Room> _rooms = new List<Room>();
        readonly List<Corridor> _corridors = new List<Corridor>();
        readonly List<(int a, int b)> _edges = new List<(int, int)>();

        public int     FloorNumber        { get; private set; } = 1;
        public Vector3 StartWorldPosition { get; private set; }
        public Vector3 ExitWorldPosition  { get; private set; }

        // ── Публичная поверхность (совместимость + навигация врагов) ───────────
        public DungeonGrid GetGrid() => _grid;
        public IReadOnlyList<Room> GetRooms() => _rooms;

        /// <summary>Мир → индекс клетки DungeonGrid (для GridPathfinder).</summary>
        public Vector2Int WorldToCell(Vector3 world) => new Vector2Int(
            Mathf.FloorToInt(world.x) - _gridOrigin.x,
            Mathf.FloorToInt(world.y) - _gridOrigin.y);

        /// <summary>Индекс клетки DungeonGrid → центр клетки в мире.</summary>
        public Vector3 CellToWorldCenter(Vector2Int cell) => new Vector3(
            cell.x + _gridOrigin.x + 0.5f,
            cell.y + _gridOrigin.y + 0.5f, 0f);

        // ── Unity ──────────────────────────────────────────────────────────────
        void Start()
        {
            if (generateOnStart) Generate();
        }

        public void GenerateNextFloor()
        {
            FloorNumber++;
            Generate();
        }

        [ContextMenu("Generate")]
        public void Generate()
        {
            if (tileSet == null)
            {
                Debug.LogError("[DungeonGenerator] Не назначен DungeonTileSet.");
                return;
            }
            if (wallTilemap == null || floorTilemap == null)
            {
                Debug.LogError("[DungeonGenerator] Не назначены Floor/Wall Tilemap.");
                return;
            }

            var validator = new DungeonValidator();

            for (int attempt = 0; attempt < maxRegenAttempts; attempt++)
            {
                int usedSeed = useRandomSeed
                    ? Random.Range(int.MinValue, int.MaxValue)
                    : seed + attempt;
                _rng = new System.Random(usedSeed);

                BuildOnce();

                if (_rooms.Count < minRooms)
                    continue; // не уместилось комнат — другой сид

                if (!validator.Validate(_grid, _rooms, out var distField))
                {
                    Debug.LogWarning($"[DungeonGenerator] Попытка {attempt + 1}: " +
                                     $"невалидно ({validator.LastError}). Перегенерация.");
                    continue;
                }

                Finalize(distField, usedSeed, attempt);
                return;
            }

            Debug.LogError($"[DungeonGenerator] Не удалось построить валидный уровень за " +
                           $"{maxRegenAttempts} попыток. Ослабь параметры комнат/карты.");
        }

        // ── Одна попытка построения логики ─────────────────────────────────────
        void BuildOnce()
        {
            _grid = new DungeonGrid(mapWidth, mapHeight);
            _rooms.Clear();
            _corridors.Clear();
            _edges.Clear();

            PlaceRooms();
            if (_rooms.Count == 0) return;

            BuildMstEdges();
            AddExtraEdges();
            BuildCorridorsAndDoors();
            RasterizeGrid();
        }

        // 1. Размещение непересекающихся комнат ─────────────────────────────────
        void PlaceRooms()
        {
            int target = _rng.Next(minRooms, maxRooms + 1);

            for (int i = 0; i < roomPlacementAttempts && _rooms.Count < target; i++)
            {
                int w = _rng.Next(minRoomSize, maxRoomSize + 1);
                int h = _rng.Next(minRoomSize, maxRoomSize + 1);
                int x = _rng.Next(mapPadding, Mathf.Max(mapPadding + 1, mapWidth  - w - mapPadding));
                int y = _rng.Next(mapPadding, Mathf.Max(mapPadding + 1, mapHeight - h - mapPadding));

                var rect = new RectInt(x, y, w, h);
                var candidate = new Room(rect, RoomType.Normal);

                bool clash = false;
                foreach (var r in _rooms)
                    if (candidate.Overlaps(r, roomSpacing)) { clash = true; break; }
                if (clash) continue;

                candidate.Index = _rooms.Count;
                candidate.type  = candidate.Index == 0 ? RoomType.Start : RollType();
                _rooms.Add(candidate);
            }
        }

        RoomType RollType()
        {
            double r = _rng.NextDouble();
            if (r < treasureChance)                              return RoomType.Treasure;
            if (r < treasureChance + shopChance)                 return RoomType.Shop;
            if (r < treasureChance + shopChance + eliteChance)   return RoomType.Elite;
            return RoomType.Normal;
        }

        // 2. MST (Прим) по евклидову расстоянию между центрами ──────────────────
        void BuildMstEdges()
        {
            int n = _rooms.Count;
            if (n < 2) return;

            var inTree = new bool[n];
            inTree[0] = true;
            int added = 1;

            while (added < n)
            {
                int bestA = -1, bestB = -1;
                long bestD = long.MaxValue;

                for (int a = 0; a < n; a++)
                {
                    if (!inTree[a]) continue;
                    for (int b = 0; b < n; b++)
                    {
                        if (inTree[b]) continue;
                        long d = SqrDist(_rooms[a], _rooms[b]);
                        if (d < bestD) { bestD = d; bestA = a; bestB = b; }
                    }
                }

                if (bestB < 0) break; // на всякий случай
                inTree[bestB] = true;
                _edges.Add((bestA, bestB));
                added++;
            }
        }

        // 3. Несколько лишних соединений (петли) сверх MST ──────────────────────
        void AddExtraEdges()
        {
            int n = _rooms.Count;
            for (int a = 0; a < n; a++)
            {
                for (int b = a + 1; b < n; b++)
                {
                    if (EdgeExists(a, b)) continue;
                    if (_rng.NextDouble() < extraConnectionChance)
                        _edges.Add((a, b));
                }
            }
        }

        bool EdgeExists(int a, int b)
        {
            foreach (var e in _edges)
                if ((e.a == a && e.b == b) || (e.a == b && e.b == a)) return true;
            return false;
        }

        static long SqrDist(Room a, Room b)
        {
            long dx = a.CenterCell.x - b.CenterCell.x;
            long dy = a.CenterCell.y - b.CenterCell.y;
            return dx * dx + dy * dy;
        }

        // 4. Коридоры + дверные проёмы ──────────────────────────────────────────
        void BuildCorridorsAndDoors()
        {
            foreach (var (ai, bi) in _edges)
            {
                Room a = _rooms[ai];
                Room b = _rooms[bi];

                // Доминирующая ось определяет, через какие грани идут двери.
                int dx = b.CenterCell.x - a.CenterCell.x;
                int dy = b.CenterCell.y - a.CenterCell.y;

                DoorCell doorA, doorB;
                if (Mathf.Abs(dx) >= Mathf.Abs(dy))
                {
                    doorA = MakeDoor(a, dx >= 0 ? Direction.East : Direction.West);
                    doorB = MakeDoor(b, dx >= 0 ? Direction.West : Direction.East);
                }
                else
                {
                    doorA = MakeDoor(a, dy >= 0 ? Direction.North : Direction.South);
                    doorB = MakeDoor(b, dy >= 0 ? Direction.South : Direction.North);
                }

                a.Doors.Add(doorA);
                b.Doors.Add(doorB);

                var corridor = new Corridor(a, b, doorA, doorB);
                corridor.Build(corridorWidth, _rng);
                _corridors.Add(corridor);
            }
        }

        /// <summary>
        /// Дверь СТРОГО по центру выбранной грани, на клетке кольца стены
        /// (одна клетка снаружи пола). Не в углу (центр грани) при размере ≥4.
        /// </summary>
        static DoorCell MakeDoor(Room room, Direction dir)
        {
            RectInt r = room.Bounds;
            int cx = r.x + r.width  / 2;
            int cy = r.y + r.height / 2;

            Vector2Int cell = dir switch
            {
                Direction.East  => new Vector2Int(r.xMax,     cy),
                Direction.West  => new Vector2Int(r.xMin - 1, cy),
                Direction.North => new Vector2Int(cx,         r.yMax),
                Direction.South => new Vector2Int(cx,         r.yMin - 1),
                _               => new Vector2Int(cx, cy)
            };
            return new DoorCell(cell, dir);
        }

        // 5. Растеризация логики в DungeonGrid ──────────────────────────────────
        void RasterizeGrid()
        {
            // Пол комнат.
            foreach (var room in _rooms)
                _grid.CarveRoom(room.Bounds);

            // Пол коридоров.
            foreach (var corridor in _corridors)
                foreach (var c in corridor.FloorCells)
                    _grid[c.x, c.y] = TileType.Floor;

            // Дверные клетки — гарантированно пол (страховка от перекрытия стеной).
            foreach (var room in _rooms)
                foreach (var d in room.Doors)
                    _grid[d.cell.x, d.cell.y] = TileType.Floor;

            // Кольцо стен вокруг всего пола одним проходом.
            _grid.BuildWalls();
        }

        // ── Финализация: рисуем, коллизии, старт/выход ──────────────────────────
        void Finalize(Dictionary<Vector2Int, int> distField, int usedSeed, int attempt)
        {
            var painter = new TilemapPainter(
                floorTilemap, wallTilemap, collisionTilemap, decorationTilemap,
                tileSet, collisionTile);
            painter.Paint(_grid, _gridOrigin.x, _gridOrigin.y, _rng);

            // Коллизии: на слой стен (или отдельный collision-слой), пол без них.
            Tilemap solid = (collisionTilemap != null && collisionTile != null)
                ? collisionTilemap : wallTilemap;
            CollisionBuilder.Configure(solid, floorTilemap);

            PlaceStartAndExit(distField);

            Debug.Log($"[DungeonGenerator] Этаж {FloorNumber}: {_rooms.Count} комнат, " +
                      $"{_corridors.Count} коридоров, сид {usedSeed}, попытка {attempt + 1}.");
        }

        void PlaceStartAndExit(Dictionary<Vector2Int, int> distField)
        {
            Room startRoom = _rooms[0];

            // Самая дальняя комната по полю расстояний → выход и босс.
            Room exitRoom = startRoom;
            int best = -1;
            foreach (var room in _rooms)
            {
                if (distField.TryGetValue(room.CenterCell, out int d) && d > best)
                {
                    best = d;
                    exitRoom = room;
                }
            }
            if (exitRoom != startRoom) exitRoom.type = RoomType.Boss;

            StartWorldPosition = CellToWorldCenter(startRoom.CenterCell);
            ExitWorldPosition  = CellToWorldCenter(exitRoom.CenterCell);

            if (player != null) player.position = StartWorldPosition;

            if (_spawnedExit != null) Destroy(_spawnedExit);
            _spawnedExit = exitPrefab != null
                ? Instantiate(exitPrefab, ExitWorldPosition, Quaternion.identity)
                : CreateDefaultExitMarker(ExitWorldPosition);

            var trigger = _spawnedExit.GetComponent<NextFloorTrigger>()
                          ?? _spawnedExit.AddComponent<NextFloorTrigger>();
            trigger.Init(this, player);
        }

        static GameObject CreateDefaultExitMarker(Vector3 position)
        {
            var go = new GameObject("Exit (auto)");
            go.transform.position = position;

            var sr = go.AddComponent<SpriteRenderer>();
            var tex = Texture2D.whiteTexture;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), tex.width);
            sr.color = new Color(1f, 0.85f, 0.1f, 0.85f);
            sr.sortingOrder = 10;
            go.transform.localScale = Vector3.one * 0.8f;

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            return go;
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (_rooms == null) return;
            foreach (var r in _rooms)
            {
                if (r == null) continue;
                Gizmos.color = r.type switch
                {
                    RoomType.Start    => Color.cyan,
                    RoomType.Boss     => Color.red,
                    RoomType.Treasure => Color.yellow,
                    RoomType.Shop     => Color.green,
                    RoomType.Elite    => Color.magenta,
                    _                 => Color.gray
                };
                Gizmos.DrawWireCube(r.WorldBounds.center, r.WorldBounds.size);
            }
        }
#endif
    }
}
