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
            var tilemaps = GetComponentsInChildren<Tilemap>(true);
            Tilemap wall = null, floor = null;
            foreach (var tm in tilemaps)
            {
                if (wall == null && tm.name.ToLower().Contains(wallTilemapNameHint.ToLower())) wall = tm;
                if (floor == null && tm.name.ToLower().Contains(floorTilemapNameHint.ToLower())) floor = tm;
            }
            if (wall == null && tilemaps.Length > 0) wall = tilemaps[0];
            if (floor == null && tilemaps.Length > 1) floor = tilemaps[1];

            // 1. Коллайдер стен → слой препятствий (для физики и NavGrid).
            //    Простой TilemapCollider2D достаточен и не требует Rigidbody2D/Composite.
            if (wall != null)
            {
                if (!wall.TryGetComponent<TilemapCollider2D>(out _))
                    wall.gameObject.AddComponent<TilemapCollider2D>();

                int layer = LayerMask.NameToLayer(obstacleLayerName);
                if (layer >= 0) wall.gameObject.layer = layer;
            }

            // 2. Габариты и центр — по тайлмапу пола.
            Tilemap sizeSource = floor != null ? floor : wall;
            if (sizeSource != null && boundsBox == null)
            {
                sizeSource.CompressBounds();
                Bounds lb = sizeSource.localBounds;
                Vector3 worldCenter = sizeSource.transform.TransformPoint(lb.center);
                _localCenter = transform.InverseTransformPoint(worldCenter);
                footprint = new Vector2(Mathf.Abs(lb.size.x), Mathf.Abs(lb.size.y));
            }

            // 3. Авто-точки стыковки по центрам сторон, если их нет.
            if (GetComponentsInChildren<RoomConnectionPoint>(true).Length == 0)
                GenerateConnectionPoints();
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
