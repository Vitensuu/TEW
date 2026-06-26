using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game.Dungeon
{
    /// <summary>
    /// Корневой компонент готовой комнаты-префаба (ТЗ — «RoomPrefab»).
    /// Висит на корне Room Prefab (у визуальных префабов из maps/ это объект с Grid +
    /// тайлмапами Floor/Wall). Собирает двери/сокеты/точки спавна и отдаёт их
    /// ассемблеру и популятору. Сам комнату НЕ генерирует.
    ///
    /// АВТО-КОНФИГ (для «голых» визуальных префабов без ручной разметки):
    /// при autoConfigure=true в Awake — добавит коллайдер на тайлмап стен (слой
    /// obstacleLayer), вычислит габариты из тайлмапа пола и, если сокетов нет,
    /// сгенерирует 4 точки стыковки по центрам сторон (N/S/E/W). Так префабы из
    /// maps/ работают сразу; дизайнер может потом расставить двери/точки вручную.
    /// </summary>
    [DisallowMultipleComponent]
    public class RoomInstance : MonoBehaviour
    {
        [Header("Данные")]
        public RoomData data;

        [Header("Авто-конфигурация визуального префаба")]
        [SerializeField] bool autoConfigure = true;
        [Tooltip("Имя тайлмапа со стенами (по подстроке имени объекта)")]
        [SerializeField] string wallTilemapNameHint = "Wall";
        [SerializeField] string floorTilemapNameHint = "Floor";
        [SerializeField] string obstacleLayerName = "Obstacle";
        [Tooltip("Ширина прохода (зазор в BoxCollider2D-стене у каждой точки соединения)")]
        [SerializeField] float doorGapWidth = 3f;
        [Tooltip("Толщина граничного коллайдера стен")]
        [SerializeField] float wallThickness = 0.5f;

        [Header("Габариты (если нет тайлмапа пола)")]
        public Vector2 footprint = new Vector2(16, 12);
        [SerializeField] BoxCollider2D boundsBox;

        // вычисленный локальный центр комнаты (по тайлмапу пола), смещение от корня
        Vector3 _localCenter;

        // ── Собранные дочерние элементы ─────────────────────────────────────────
        public readonly List<RoomConnectionPoint> Connections = new List<RoomConnectionPoint>();
        public readonly List<DoorController> Doors = new List<DoorController>();
        public readonly List<SpawnPoint> EnemyPoints = new List<SpawnPoint>();
        public readonly List<SpawnPoint> ChestPoints = new List<SpawnPoint>();
        public readonly List<SpawnPoint> NpcPoints = new List<SpawnPoint>();
        public readonly List<SpawnPoint> DecorationPoints = new List<SpawnPoint>();
        public readonly List<SpawnPoint> TrapPoints = new List<SpawnPoint>();
        public Transform EntryPoint { get; private set; }
        public Transform ExitPoint  { get; private set; }

        public int RotationStepsCW { get; private set; }
        public RoomType Type => data != null ? data.roomType : RoomType.Normal;

        [System.NonSerialized] public RoomNode Node;

        void Awake()
        {
            // AutoConfigure не должен ронять Awake — иначе Collect() не выполнится
            // и комната останется без точек стыковки.
            if (autoConfigure)
            {
                try { AutoConfigure(); }
                catch (System.Exception e) { Debug.LogWarning($"[RoomInstance] AutoConfigure: {e.Message}", this); }
            }
            Collect();
        }

        // ── Авто-конфиг визуального префаба ──────────────────────────────────────
        void AutoConfigure()
        {
            // 1. Найти тайлмап пола для вычисления габаритов.
            var tilemaps = GetComponentsInChildren<Tilemap>(true);
            Tilemap floor = null;
            foreach (var tm in tilemaps)
            {
                if (floor == null && tm.name.ToLower().Contains(floorTilemapNameHint.ToLower()))
                    floor = tm;
            }
            if (floor == null)
            {
                if (tilemaps.Length > 1)       floor = tilemaps[1];
                else if (tilemaps.Length == 1) floor = tilemaps[0];
            }

            // 2. Габариты и центр — по тайлмапу пола.
            if (floor != null && boundsBox == null)
            {
                floor.CompressBounds();
                Bounds lb = floor.localBounds;
                Vector3 worldCenter = floor.transform.TransformPoint(lb.center);
                _localCenter = transform.InverseTransformPoint(worldCenter);
                footprint = new Vector2(Mathf.Abs(lb.size.x), Mathf.Abs(lb.size.y));
            }

            // 3. Авто-точки стыковки по центрам сторон, если их нет.
            if (GetComponentsInChildren<RoomConnectionPoint>(true).Length == 0)
                GenerateConnectionPoints();

            // 4. BoxCollider2D периметр с зазорами для проходов вместо TilemapCollider2D.
            //    Так проходы остаются открытыми независимо от тайлмапа стен.
            if (transform.Find("BoundaryWalls") == null)
                GenerateBoundaryColliders();
        }

        void GenerateConnectionPoints()
        {
            var holder = new GameObject("AutoConnections").transform;
            holder.SetParent(transform, false);
            holder.localPosition = Vector3.zero;

            float hx = footprint.x * 0.5f, hy = footprint.y * 0.5f;
            AddConn(holder, "Conn_N", _localCenter + new Vector3(0,  hy, 0), Direction.North);
            AddConn(holder, "Conn_S", _localCenter + new Vector3(0, -hy, 0), Direction.South);
            AddConn(holder, "Conn_E", _localCenter + new Vector3( hx, 0, 0), Direction.East);
            AddConn(holder, "Conn_W", _localCenter + new Vector3(-hx, 0, 0), Direction.West);
        }

        void AddConn(Transform parent, string n, Vector3 localPos, Direction dir)
        {
            var go = new GameObject(n);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.AddComponent<RoomConnectionPoint>().direction = dir;
        }

        void GenerateBoundaryColliders()
        {
            float hx = footprint.x * 0.5f;
            float hy = footprint.y * 0.5f;
            float dh = doorGapWidth * 0.5f;
            int layer = LayerMask.NameToLayer(obstacleLayerName);

            var holder = new GameObject("BoundaryWalls").transform;
            holder.SetParent(transform, false);

            var conns = GetComponentsInChildren<RoomConnectionPoint>(true);
            bool hasN = false, hasS = false, hasE = false, hasW = false;
            foreach (var c in conns)
            {
                if      (c.direction == Direction.North) hasN = true;
                else if (c.direction == Direction.South) hasS = true;
                else if (c.direction == Direction.East)  hasE = true;
                else if (c.direction == Direction.West)  hasW = true;
            }

            AddSideBoxes(holder, _localCenter + new Vector3(0,  hy, 0), true,  hx, dh, layer, hasN);
            AddSideBoxes(holder, _localCenter + new Vector3(0, -hy, 0), true,  hx, dh, layer, hasS);
            AddSideBoxes(holder, _localCenter + new Vector3( hx, 0, 0), false, hy, dh, layer, hasE);
            AddSideBoxes(holder, _localCenter + new Vector3(-hx, 0, 0), false, hy, dh, layer, hasW);
        }

        // Добавляет 1 или 2 BoxCollider2D для одной стороны периметра.
        // horizontal=true → стена горизонтальная (N/S), halfLen = hx.
        // hasDoor=true → разбиваем на 2 сегмента с зазором doorHW×2 по центру.
        void AddSideBoxes(Transform parent, Vector3 wallLocal, bool horizontal,
                          float halfLen, float doorHW, int layer, bool hasDoor)
        {
            if (!hasDoor)
            {
                var sz = horizontal ? new Vector2(halfLen * 2, wallThickness)
                                    : new Vector2(wallThickness, halfLen * 2);
                CreateWallBox(parent, wallLocal, sz, layer);
                return;
            }
            float segLen = halfLen - doorHW;
            if (segLen <= 0.02f) return;
            float offset = (halfLen + doorHW) * 0.5f;
            var segSz = horizontal ? new Vector2(segLen, wallThickness)
                                   : new Vector2(wallThickness, segLen);
            var shift = horizontal ? new Vector3(offset, 0, 0) : new Vector3(0, offset, 0);
            CreateWallBox(parent, wallLocal - shift, segSz, layer);
            CreateWallBox(parent, wallLocal + shift, segSz, layer);
        }

        void CreateWallBox(Transform parent, Vector3 localPos, Vector2 size, int layer)
        {
            var go = new GameObject("WallSeg");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            if (layer >= 0) go.layer = layer;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
        }

        /// <summary>
        /// Закрыть зазор для неиспользованного прохода в указанном локальном направлении.
        /// Вызывается ассемблером после финальной сборки этажа.
        /// </summary>
        public void SealDoor(Direction localDir)
        {
            float hx = footprint.x * 0.5f, hy = footprint.y * 0.5f;
            float dh = doorGapWidth * 0.5f;
            int layer = LayerMask.NameToLayer(obstacleLayerName);

            bool horizontal = (localDir == Direction.North || localDir == Direction.South);
            float axisOff = (localDir == Direction.North || localDir == Direction.East) ? 1f : -1f;
            Vector3 wallCenter = horizontal
                ? _localCenter + new Vector3(0, axisOff * hy, 0)
                : _localCenter + new Vector3(axisOff * hx, 0, 0);
            Vector2 sealSz = horizontal
                ? new Vector2(doorGapWidth, wallThickness)
                : new Vector2(wallThickness, doorGapWidth);

            var holder = transform.Find("BoundaryWalls");
            if (holder == null)
            {
                var h = new GameObject("BoundaryWalls");
                h.transform.SetParent(transform, false);
                holder = h.transform;
            }
            CreateWallBox(holder, wallCenter, sealSz, layer);
        }

        // ── Сбор дочерних элементов ──────────────────────────────────────────────
        public void Collect()
        {
            Connections.Clear(); Doors.Clear();
            EnemyPoints.Clear(); ChestPoints.Clear(); NpcPoints.Clear();
            DecorationPoints.Clear(); TrapPoints.Clear();
            EntryPoint = ExitPoint = null;

            GetComponentsInChildren(true, Connections);
            GetComponentsInChildren(true, Doors);

            var points = new List<SpawnPoint>();
            GetComponentsInChildren(true, points);
            foreach (var p in points)
            {
                switch (p.kind)
                {
                    case SpawnKind.Enemy:      EnemyPoints.Add(p); break;
                    case SpawnKind.Chest:      ChestPoints.Add(p); break;
                    case SpawnKind.NPC:        NpcPoints.Add(p); break;
                    case SpawnKind.Decoration: DecorationPoints.Add(p); break;
                    case SpawnKind.Trap:       TrapPoints.Add(p); break;
                    case SpawnKind.Entry:      EntryPoint = p.transform; break;
                    case SpawnKind.Exit:       ExitPoint  = p.transform; break;
                }
            }
        }

        public IEnumerable<RoomConnectionPoint> FreeConnections()
        {
            foreach (var c in Connections) if (!c.used) yield return c;
        }

        public RoomConnectionPoint FreeConnectionFacing(Direction dir)
        {
            foreach (var c in Connections)
                if (!c.used && c.WorldDirection(RotationStepsCW) == dir) return c;
            return null;
        }

        public void SetRotationSteps(int stepsCW)
        {
            RotationStepsCW = ((stepsCW % 4) + 4) % 4;
            transform.rotation = Quaternion.Euler(0, 0, -90f * RotationStepsCW);
        }

        /// <summary>Мировые габариты комнаты (AABB) с учётом поворота и центра пола.</summary>
        public Bounds WorldBounds
        {
            get
            {
                if (boundsBox != null) return boundsBox.bounds;
                Vector2 size = (RotationStepsCW % 2 == 0)
                    ? footprint : new Vector2(footprint.y, footprint.x);
                Vector3 center = transform.TransformPoint(_localCenter);
                return new Bounds(center, new Vector3(size.x, size.y, 1f));
            }
        }

        /// <summary>Случайная точка внутри пола (для fallback-спавна без точек).</summary>
        public Vector3 RandomFloorPoint(float pad = 1.5f)
        {
            Bounds b = WorldBounds;
            return new Vector3(
                Random.Range(b.min.x + pad, b.max.x - pad),
                Random.Range(b.min.y + pad, b.max.y - pad), 0f);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            var b = WorldBounds;
            Gizmos.DrawWireCube(b.center, b.size);
        }

    }
}
