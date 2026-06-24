using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Dungeon
{
    /// <summary>
    /// Генерирует подземелье через BSP-разбиение, рисует результат в Tilemap,
    /// ставит игрока в стартовую комнату и спавнит выход на следующий этаж
    /// в самой дальней (по графу коридоров) комнате.
    ///
    /// СЛОИ TILEMAP (назначить в инспекторе или создадутся автоматически):
    ///   floorTilemap  — пол (sortingOrder 0)
    ///   wallTilemap   — нижняя часть стены / WallFace_Bot (sortingOrder 1)
    ///   ledgeTilemap  — верхняя часть стены / крыша WallFace_Top (sortingOrder 2)
    /// </summary>
    public class DungeonGenerator : MonoBehaviour
    {
        [Header("Размер подземелья (в тайлах)")]
        [SerializeField] private int dungeonWidth  = 60;
        [SerializeField] private int dungeonHeight = 44;

        [Header("BSP")]
        [Tooltip("Минимальный размер листа BSP. Должен быть заметно больше maxRoomSize, " +
                 "чтобы комната помещалась с отступом.")]
        [SerializeField] private int minLeafSize  = 18;
        [Tooltip("Отступ от края BSP-листа до края комнаты")]
        [SerializeField] private int roomPadding  = 1;

        [Header("Комнаты (размер в тайлах по каждой стороне)")]
        [SerializeField] private int minRoomSize = 6;
        [SerializeField] private int maxRoomSize = 15;

        [Header("Коридоры")]
        [SerializeField] private int corridorWidth = 2;

        [Header("Сид генерации")]
        [SerializeField] private bool useRandomSeed = true;
        [SerializeField] private int  seed;

        [Header("Tilemap — можно оставить пустым, создастся автоматически")]
        [SerializeField] private Tilemap floorTilemap;
        [SerializeField] private Tilemap wallTilemap;
        [Tooltip("Верхний слой: крыша коробки и верхняя часть лицевой стены")]
        [SerializeField] private Tilemap ledgeTilemap;

        [Header("Тайлы пола (один или несколько — выбирается по позиции)")]
        [SerializeField] private TileBase[] floorTiles;

        [Header("Стены")]
        [Tooltip("ScriptableObject с тайлами. Создать: ПКМ → Create → Dungeon → Wall Tile Set")]
        [SerializeField] private DungeonWallTileSet wallTileSet;
        [Tooltip("Запасной тайл стены если wallTileSet не назначен")]
        [SerializeField] private TileBase wallTileFallback;

        [Header("Игрок и выход")]
        [SerializeField] private Transform  player;
        [Tooltip("Если не назначен — создастся простая заглушка-триггер")]
        [SerializeField] private GameObject exitPrefab;

        [Header("Запуск")]
        [SerializeField] private bool generateOnStart = true;

        // ── Внутреннее состояние ────────────────────────────────────────────
        private System.Random              _rng;
        private DungeonGrid                _grid;
        private readonly List<RoomInfo>    _rooms       = new List<RoomInfo>();
        private readonly List<(int a, int b)> _connections = new List<(int a, int b)>();
        private GameObject                 _spawnedExit;

        public int     FloorNumber         { get; private set; } = 1;
        public Vector3 StartWorldPosition  { get; private set; }
        public Vector3 ExitWorldPosition   { get; private set; }

        public DungeonGrid GetGrid()  => _grid;
        public IReadOnlyList<RoomInfo> GetRooms() => _rooms;
        public Tilemap FloorTilemap   => floorTilemap;

        // ── Unity ───────────────────────────────────────────────────────────
        private void Start()
        {
            if (generateOnStart) Generate();
        }

        // ── Публичный API ───────────────────────────────────────────────────

        [ContextMenu("Generate")]
        public void Generate()
        {
            _rng = useRandomSeed ? new System.Random() : new System.Random(seed);

            EnsureTilemaps();

            _rooms.Clear();
            _connections.Clear();

            floorTilemap.ClearAllTiles();
            wallTilemap.ClearAllTiles();
            ledgeTilemap.ClearAllTiles();

            if (_spawnedExit != null) Destroy(_spawnedExit);

            _grid = new DungeonGrid(dungeonWidth, dungeonHeight);

            var root = new BSPNode(new RectInt(0, 0, dungeonWidth, dungeonHeight));
            // Отступ 3 тайла от края — место для стен по периметру
            const int border = 3;
            var root = new BSPNode(new RectInt(border, border, dungeonWidth - border * 2, dungeonHeight - border * 2));
            root.Split(minLeafSize, _rng);

            BuildRoomsAndCorridors(root);
            _grid.BuildWalls();

            PaintTilemap();
            PlaceStartAndExit();
            GetComponent<RoomPopulator>()?.Populate();
        }

        public void GenerateNextFloor()
        {
            FloorNumber++;
            Generate();
        }

        // ── BSP → комнаты и коридоры ────────────────────────────────────────

        private (RoomInfo room, int index) BuildRoomsAndCorridors(BSPNode node)
        {
            if (node.IsLeaf)
            {
                RoomInfo room = CreateRoom(node.Bounds);
                _grid.CarveRoom(room.Area);
                _rooms.Add(room);
                return (room, _rooms.Count - 1);
            }

            var left  = BuildRoomsAndCorridors(node.Left);
            var right = BuildRoomsAndCorridors(node.Right);

            _grid.CarveCorridor(left.room.Center, right.room.Center, corridorWidth, _rng);
            _connections.Add((left.index, right.index));

            return _rng.NextDouble() < 0.5 ? left : right;
        }

        private RoomInfo CreateRoom(RectInt leaf)
        {
            int maxW = Mathf.Max(minRoomSize, Mathf.Min(maxRoomSize, leaf.width  - roomPadding * 2));
            int maxH = Mathf.Max(minRoomSize, Mathf.Min(maxRoomSize, leaf.height - roomPadding * 2));

            int w = Mathf.Clamp(_rng.Next(minRoomSize, maxRoomSize + 1), minRoomSize, maxW);
            int h = Mathf.Clamp(_rng.Next(minRoomSize, maxRoomSize + 1), minRoomSize, maxH);

            int freeX = leaf.width  - w - roomPadding * 2;
            int freeY = leaf.height - h - roomPadding * 2;

            int x = leaf.x + roomPadding + (freeX > 0 ? _rng.Next(0, freeX + 1) : 0);
            int y = leaf.y + roomPadding + (freeY > 0 ? _rng.Next(0, freeY + 1) : 0);

            return new RoomInfo(new RectInt(x, y, w, h));
        }

        // ── Tilemap ─────────────────────────────────────────────────────────

        private void PaintTilemap()
        {
            // Пол
            for (int x = 0; x < _grid.Width; x++)
            for (int y = 0; y < _grid.Height; y++)
            {
                if (_grid[x, y] != TileType.Floor) continue;
                floorTilemap.SetTile(new Vector3Int(x, y, 0), PickFloor(x, y));
            }

            // Стены
            if (wallTileSet != null)
            {
                DungeonWallPainter.Paint(_grid, wallTileSet, wallTilemap, ledgeTilemap, _rooms);
            }
            else
            {
                // Запасной вариант — один тайл на все стены
                for (int x = 0; x < _grid.Width; x++)
                for (int y = 0; y < _grid.Height; y++)
                {
                    if (_grid[x, y] != TileType.Wall) continue;
                    wallTilemap.SetTile(new Vector3Int(x, y, 0), wallTileFallback);
                }
            }
        }

        private TileBase PickFloor(int x, int y)
        {
            if (floorTiles == null || floorTiles.Length == 0) return null;
            if (floorTiles.Length == 1) return floorTiles[0];
            int hash = x * 73856093 ^ y * 19349663;
            return floorTiles[Mathf.Abs(hash) % floorTiles.Length];
        }

        // ── Старт / выход ───────────────────────────────────────────────────

        private void PlaceStartAndExit()
        {
            if (_rooms.Count == 0) return;

            int startIndex = 0;
            int exitIndex  = FindFurthestRoom(startIndex);

            StartWorldPosition = CellToWorldCenter(_rooms[startIndex].Center);
            ExitWorldPosition  = CellToWorldCenter(_rooms[exitIndex].Center);

            if (player != null)
                player.position = StartWorldPosition;

            _spawnedExit = exitPrefab != null
                ? Instantiate(exitPrefab, ExitWorldPosition, Quaternion.identity)
                : CreateDefaultExitMarker(ExitWorldPosition);

            var trigger = _spawnedExit.GetComponent<NextFloorTrigger>();
            if (trigger == null) trigger = _spawnedExit.AddComponent<NextFloorTrigger>();
            trigger.Init(this, player);
        }

        private int FindFurthestRoom(int startIndex)
        {
            var adjacency = new List<int>[_rooms.Count];
            for (int i = 0; i < adjacency.Length; i++) adjacency[i] = new List<int>();
            foreach (var (a, b) in _connections)
            {
                adjacency[a].Add(b);
                adjacency[b].Add(a);
            }

            var distance = new int[_rooms.Count];
            for (int i = 0; i < distance.Length; i++) distance[i] = -1;
            distance[startIndex] = 0;

            var queue = new Queue<int>();
            queue.Enqueue(startIndex);
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                foreach (int next in adjacency[current])
                {
                    if (distance[next] != -1) continue;
                    distance[next] = distance[current] + 1;
                    queue.Enqueue(next);
                }
            }

            int best = startIndex;
            for (int i = 0; i < distance.Length; i++)
                if (distance[i] > distance[best]) best = i;

            return best;
        }

        private Vector3 CellToWorldCenter(Vector2Int cell)
            => floorTilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));

        // ── Автосоздание слоёв ──────────────────────────────────────────────

        private void EnsureTilemaps()
        {
            if (floorTilemap != null && wallTilemap != null && ledgeTilemap != null) return;

            var gridGO = new GameObject("Grid (Dungeon)");
            var grid   = gridGO.AddComponent<Grid>();
            grid.cellSize = Vector3.one;

            if (floorTilemap  == null) floorTilemap  = CreateTilemapLayer(gridGO.transform, "Floor",  0);
            if (wallTilemap   == null) wallTilemap   = CreateTilemapLayer(gridGO.transform, "Wall",   1);
            if (ledgeTilemap  == null) ledgeTilemap  = CreateTilemapLayer(gridGO.transform, "Ledge",  2);
        }

        private static Tilemap CreateTilemapLayer(Transform parent, string layerName, int sortingOrder)
        {
            var go = new GameObject(layerName);
            go.transform.SetParent(parent);
            var tilemap  = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;
            return tilemap;
        }

        private static GameObject CreateDefaultExitMarker(Vector3 position)
        {
            var go = new GameObject("Exit (auto)");
            go.transform.position = position;

            var texture = Texture2D.whiteTexture;
            var sr      = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), texture.width);
            sr.color        = new Color(1f, 0.85f, 0.1f, 0.85f);
            sr.sortingOrder = 10;
            go.transform.localScale = Vector3.one * 0.8f;

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            return go;
        }
    }
}
