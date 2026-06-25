using System.Collections.Generic;
using UnityEngine;
using Game.Data;

namespace Game.Dungeon
{
    /// <summary>
    /// Сборщик этажа из ГОТОВЫХ комнат-префабов (ТЗ — DungeonAssembler).
    /// НЕ создаёт геометрию комнат. Делает: абстрактный граф (ЭТАП 2) → выбор
    /// префабов (ЭТАП 3) → стыковка через двери с вращением и без наложения
    /// (ЭТАП 4) → строит RoomGraph + NavGrid (ЭТАП 6). Популяцию запускает
    /// отдельно RoomManager/RoomPopulator (ЭТАП 5).
    ///
    /// Это полная замена старого процедурного DungeonGenerator.
    /// </summary>
    public class DungeonAssembler : MonoBehaviour
    {
        [Header("Библиотека и конфиги")]
        [SerializeField] RoomLibrary library;
        [Tooltip("Конфиги этажей по индексу (этаж 1 = element 0). Если пусто/коротко — берётся последний.")]
        [SerializeField] List<FloorConfig> floorConfigs = new List<FloorConfig>();

        [Header("Навигация")]
        [Tooltip("Слой стен/препятствий для построения NavGrid и проверки прохода")]
        [SerializeField] LayerMask obstacleMask;
        [SerializeField] float navCellSize = 1f;

        [Header("Сборка")]
        [Tooltip("Зазор между состыкованными дверями (0 = вплотную)")]
        [SerializeField] float doorGap = 0f;
        [SerializeField] int maxAssembleAttempts = 12;
        [SerializeField] int perRoomPlacementAttempts = 8;

        [Header("Сид / игрок")]
        [SerializeField] bool useRandomSeed = true;
        [SerializeField] int seed = 12345;
        [SerializeField] Transform player;

        [Header("Запуск")]
        [SerializeField] bool assembleOnStart = true;

        // ── Состояние ────────────────────────────────────────────────────────────
        System.Random _rng;
        readonly List<RoomInstance> _placed = new List<RoomInstance>();
        Transform _root;

        public RoomGraph Graph { get; private set; }
        public NavGrid   Nav   { get; private set; }
        public int FloorNumber { get; private set; } = 1;
        public Vector3 StartWorldPosition { get; private set; }
        public Vector3 ExitWorldPosition  { get; private set; }
        public IReadOnlyList<RoomInstance> Rooms => _placed;
        public LayerMask ObstacleMask => obstacleMask;
        public FloorConfig CurrentConfig { get; private set; }

        public System.Action<int> OnFloorAssembled; // floorNumber

        void Start() { if (assembleOnStart) Assemble(); }

        public void AdvanceFloor()
        {
            Game.Core.EventBus.TriggerFloorComplete(FloorNumber);
            FloorNumber++;
            Assemble();
        }

        /// <summary>
        /// Выбрать префаб типа type; если таких нет в библиотеке — деградируем
        /// к Normal (а затем к любому). Позволяет собирать этаж, даже когда
        /// дизайнер сделал не все 10 типов комнат (Elite/Treasure/Shop/Shrine → Normal).
        /// </summary>
        /// <summary>Диагностика содержимого библиотеки (для отладки сборки).</summary>
        string DescribeLibrary()
        {
            if (library == null) return "RoomLibrary == null";
            var all = library.All;
            if (all == null || all.Count == 0) return "ПУСТАЯ (0 RoomData) — заполни список Rooms в RoomLibrary";

            var counts = new Dictionary<RoomType, int>();
            int noPrefab = 0;
            foreach (var rd in all)
            {
                if (rd == null) continue;
                counts.TryGetValue(rd.roomType, out int c);
                counts[rd.roomType] = c + 1;
                if (rd.prefab == null) noPrefab++;
            }
            var sb = new System.Text.StringBuilder();
            sb.Append($"{all.Count} RoomData [");
            foreach (var kv in counts) sb.Append($"{kv.Key}:{kv.Value} ");
            sb.Append($"] без префаба: {noPrefab}");
            return sb.ToString();
        }

        RoomData PickRoom(RoomType type)
        {
            var rd = library.Pick(type, FloorNumber, _rng);
            if (rd == null && type != RoomType.Normal) rd = library.Pick(RoomType.Normal, FloorNumber, _rng);
            return rd;
        }

        FloorConfig ConfigForFloor(int floor)
        {
            if (floorConfigs == null || floorConfigs.Count == 0) return null;
            int idx = Mathf.Clamp(floor - 1, 0, floorConfigs.Count - 1);
            return floorConfigs[idx];
        }

        // ── Главный конвейер ───────────────────────────────────────────────────
        [ContextMenu("Assemble Floor")]
        public void Assemble()
        {
            if (library == null) { Debug.LogError("[DungeonAssembler] Не назначен RoomLibrary."); return; }

            Debug.Log($"[DungeonAssembler] Библиотека: {DescribeLibrary()}");

            var cfg = ConfigForFloor(FloorNumber);
            CurrentConfig = cfg;

            for (int attempt = 0; attempt < maxAssembleAttempts; attempt++)
            {
                int usedSeed = useRandomSeed ? Random.Range(int.MinValue, int.MaxValue) : seed + attempt;
                _rng = new System.Random(usedSeed);
                library.ResetHistory();

                if (TryAssembleOnce(cfg, out string err))
                {
                    Finish(usedSeed, attempt);
                    return;
                }
                Debug.LogWarning($"[DungeonAssembler] Попытка {attempt + 1} провалена: {err}. Перегенерация.");
            }
            Debug.LogError($"[DungeonAssembler] Не удалось собрать этаж за {maxAssembleAttempts} попыток. " +
                           "Проверь RoomLibrary (хватает ли префабов нужных типов и дверей).");
        }

        bool TryAssembleOnce(FloorConfig cfg, out string error)
        {
            ClearPlaced();
            Graph = new RoomGraph();

            var spec = LevelGraphBuilder.Build(cfg, FloorNumber, _rng);

            // Узлы графа создаём 1:1 со spec, чтобы parentIndex совпадал с индексом узла.
            var nodes = new RoomNode[spec.Count];

            // ЭТАП 3+4: размещаем по порядку (родитель всегда раньше ребёнка).
            for (int i = 0; i < spec.Count; i++)
            {
                var s = spec[i];
                RoomData rd = PickRoom(s.type);
                if (rd == null) { error = $"нет ни одного префаба (тип {s.type}/Normal) для этажа {FloorNumber}"; return false; }

                if (i == 0) // Start — в начало координат
                {
                    var startInst = InstantiateRoom(rd);
                    var node = Graph.AddNode(s.type);
                    Bind(node, startInst);
                    nodes[i] = node;
                    continue;
                }

                RoomNode parentNode = nodes[s.parentIndex];
                if (parentNode == null || parentNode.Instance == null) { error = "родитель не размещён"; return false; }

                if (!PlaceChild(parentNode, s.type, out RoomInstance childInst, out Direction dirFromParent))
                {
                    error = $"не удалось пристыковать {s.type} к узлу {s.parentIndex}";
                    return false;
                }

                var childNode = Graph.AddNode(s.type);
                Bind(childNode, childInst);
                Graph.Connect(parentNode, childNode, dirFromParent);
                nodes[i] = childNode;
            }

            Graph.RecomputeDepth();

            bool requireBoss = cfg != null && (cfg.hasBoss || FloorNumber % 5 == 0);
            if (!RoomValidator.Validate(Graph, requireBoss, true, out error)) return false;

            error = null;
            return true;
        }

        // ── ЭТАП 4: стыковка ребёнка к родителю ─────────────────────────────────
        bool PlaceChild(RoomNode parentNode, RoomType type,
            out RoomInstance child, out Direction dirFromParent)
        {
            child = null;
            dirFromParent = Direction.North;
            RoomInstance parent = parentNode.Instance;

            // Несколько попыток: разные префабы и разные сокеты.
            for (int attempt = 0; attempt < perRoomPlacementAttempts; attempt++)
            {
                RoomData rd = PickRoom(type);
                if (rd == null) return false;

                var candidate = InstantiateRoom(rd);

                // Перебираем свободные сокеты родителя в случайном порядке.
                foreach (var ps in Shuffled(new List<RoomConnectionPoint>(parent.FreeConnections())))
                {
                    Direction worldDir = ps.WorldDirection(parent.RotationStepsCW);
                    Direction needChildDir = worldDir.Opposite();

                    foreach (var cs in candidate.FreeConnections())
                    {
                        int steps;
                        if (candidate.data != null && candidate.data.canRotate)
                            steps = cs.direction.StepsTo(needChildDir);
                        else if (cs.direction == needChildDir)
                            steps = 0;
                        else
                            continue;

                        candidate.SetRotationSteps(steps);

                        // Совмещаем сокеты дверь-к-двери: childSocket.world == parentSocket.world + dir*gap
                        // (gap=0 → комнаты впритык по краю пола; авто-сокеты стоят на кромке пола).
                        Vector3 desired = ps.transform.position
                                          + (Vector3)((Vector2)worldDir.ToVector() * doorGap);
                        Vector3 delta = desired - cs.transform.position;
                        candidate.transform.position += delta;

                        if (!RoomValidator.AnyOverlap(_placed, candidate))
                        {
                            ps.used = true; cs.used = true;
                            _placed.Add(candidate);
                            child = candidate;
                            dirFromParent = worldDir;
                            return true;
                        }
                    }
                }

                // Не подошёл ни один сокет — пробуем другой префаб.
                DestroyRoom(candidate);
            }
            return false;
        }

        // ── Финал: NavGrid, игрок, выход ────────────────────────────────────────
        void Finish(int usedSeed, int attempt)
        {
            Nav = NavGridBuilder.Build(TotalBounds(), obstacleMask, navCellSize);

            var startInst = Graph.Start?.Instance;
            var exitInst  = (Graph.Exit ?? Graph.Boss)?.Instance;

            StartWorldPosition = startInst != null && startInst.EntryPoint != null
                ? startInst.EntryPoint.position
                : (startInst != null ? startInst.transform.position : Vector3.zero);

            ExitWorldPosition = exitInst != null && exitInst.ExitPoint != null
                ? exitInst.ExitPoint.position
                : (exitInst != null ? exitInst.transform.position : StartWorldPosition);

            if (player != null) player.position = StartWorldPosition;

            OnFloorAssembled?.Invoke(FloorNumber);
            Game.Core.EventBus.TriggerFloorGenerated(FloorNumber);

            Debug.Log($"[DungeonAssembler] Этаж {FloorNumber}: {_placed.Count} комнат, сид {usedSeed}, попытка {attempt + 1}.");
        }

        Bounds TotalBounds()
        {
            if (_placed.Count == 0) return new Bounds(Vector3.zero, Vector3.one * 10f);
            Bounds b = _placed[0].WorldBounds;
            for (int i = 1; i < _placed.Count; i++) b.Encapsulate(_placed[i].WorldBounds);
            return b;
        }

        // ── Вспомогательное ────────────────────────────────────────────────────
        void Bind(RoomNode node, RoomInstance inst)
        {
            node.Instance = inst;
            inst.Node = node;
        }

        RoomInstance InstantiateRoom(RoomData rd)
        {
            if (_root == null)
            {
                _root = new GameObject("~AssembledFloor").transform;
                _root.SetParent(transform, false);
            }
            var go = Instantiate(rd.prefab, _root);
            var inst = go.GetComponent<RoomInstance>();
            if (inst == null) inst = go.AddComponent<RoomInstance>();
            if (inst.data == null) inst.data = rd;
            inst.Collect();
            inst.SetRotationSteps(0);
            go.transform.position = Vector3.zero;
            return inst;
        }

        void DestroyRoom(RoomInstance inst)
        {
            if (inst != null) Destroy(inst.gameObject);
        }

        void ClearPlaced()
        {
            foreach (var r in _placed) if (r != null) Destroy(r.gameObject);
            _placed.Clear();
            if (_root != null) { Destroy(_root.gameObject); _root = null; }
        }

        List<T> Shuffled<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list;
        }
    }
}
