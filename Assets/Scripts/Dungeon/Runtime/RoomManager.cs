using UnityEngine;
using Game.Core;

namespace Game.Dungeon
{
    /// <summary>
    /// Оркестратор собранного этажа (ТЗ — RoomManager). Связывает ассемблер,
    /// популятор, граф и навигацию. После сборки этажа навешивает Encounter на
    /// боевые комнаты и портал — на выход. Отдаёт NavGrid/Graph для AI и миникарты
    /// (ЭТАП 6: навигация, соседи, открытие дверей).
    /// </summary>
    public class RoomManager : Singleton<RoomManager>
    {
        [SerializeField] DungeonAssembler assembler;
        [SerializeField] RoomPopulator populator;

        public DungeonAssembler Assembler => assembler;
        public RoomGraph Graph => assembler != null ? assembler.Graph : null;
        public NavGrid   Nav   => assembler != null ? assembler.Nav   : null;
        public RoomNode  ActiveRoom { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;
            if (assembler == null) assembler = FindFirstObjectByType<DungeonAssembler>();
            if (populator == null) populator = FindFirstObjectByType<RoomPopulator>();
            if (assembler != null) assembler.OnFloorAssembled += OnFloorAssembled;
        }

        protected override void OnDestroy()
        {
            if (assembler != null) assembler.OnFloorAssembled -= OnFloorAssembled;
            base.OnDestroy();
        }

        void OnFloorAssembled(int floor)
        {
            if (assembler == null) return;
            var cfg = assembler.CurrentConfig;

            foreach (var room in assembler.Rooms)
            {
                if (room == null) continue;

                if (IsCombatRoom(room.Type))
                {
                    var enc = room.GetComponent<RoomEncounter>() ?? room.gameObject.AddComponent<RoomEncounter>();
                    enc.Init(room, populator, cfg, floor);
                }

                if (room.Type == RoomType.Exit || (room.Type == RoomType.Boss && Graph.Exit == null))
                {
                    var portal = room.GetComponent<NextRoomPortal>() ?? room.gameObject.AddComponent<NextRoomPortal>();
                    portal.Init(room);
                }
            }

            ActiveRoom = Graph?.Start;
        }

        static bool IsCombatRoom(RoomType t)
            => t == RoomType.Normal || t == RoomType.Elite || t == RoomType.Boss;

        public void AdvanceFloor() => assembler?.AdvanceFloor();

        // ── Навигация для AI ──────────────────────────────────────────────────
        public Vector2Int WorldToCell(Vector3 world)
            => Nav != null ? Nav.WorldToCell(world) : Vector2Int.zero;

        public Vector3 CellToWorld(Vector2Int cell)
            => Nav != null ? Nav.CellToWorldCenter(cell) : Vector3.zero;

        public void RefreshNav()
        {
            if (Nav != null && assembler != null)
                NavGridBuilder.Refresh(Nav, assembler.ObstacleMask);
        }
    }
}
