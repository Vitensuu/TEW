using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.Data;

namespace Game.Dungeon
{
    /// <summary>
    /// «Живая комната» (ТЗ — Система боя / Живая комната). При входе игрока:
    /// двери закрываются → начинается Encounter (спавн врагов). После зачистки:
    /// двери открываются, активируются сундуки, разблокируется выход.
    /// Добавляется RoomManager'ом на боевые комнаты в рантайме.
    /// </summary>
    [RequireComponent(typeof(RoomInstance))]
    public class RoomEncounter : MonoBehaviour
    {
        RoomInstance _room;
        RoomPopulator _populator;
        FloorConfig _cfg;
        int _floor;

        bool _started, _cleared;
        readonly List<GameObject> _enemies = new List<GameObject>();

        public bool Cleared => _cleared;
        public System.Action<RoomEncounter> OnCleared;

        public void Init(RoomInstance room, RoomPopulator populator, FloorConfig cfg, int floor)
        {
            _room = room; _populator = populator; _cfg = cfg; _floor = floor;
            EnsureTrigger();
        }

        void Awake() { if (_room == null) _room = GetComponent<RoomInstance>(); }

        void EnsureTrigger()
        {
            // Триггер по габаритам комнаты — ловит вход игрока.
            var trigger = gameObject.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            Bounds b = _room.WorldBounds;
            trigger.size = new Vector2(b.size.x * 0.9f, b.size.y * 0.9f);
            trigger.offset = transform.InverseTransformPoint(b.center);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_started || _cleared) return;
            if (!other.CompareTag("Player")) return;
            Begin();
        }

        void Begin()
        {
            _started = true;
            if (_room.Node != null) _room.Node.Visited = true;

            CloseDoors();                          // ТЗ: двери закрываются
            _enemies.Clear();
            _enemies.AddRange(_populator.PopulateRoom(_room, _cfg, _floor));

            if (_enemies.Count == 0) Clear();      // пустая комната — сразу зачищена
        }

        void Update()
        {
            if (!_started || _cleared) return;
            _enemies.RemoveAll(e => e == null);    // мёртвые враги уничтожаются
            if (_enemies.Count == 0) Clear();
        }

        void Clear()
        {
            _cleared = true;
            if (_room.Node != null) _room.Node.Cleared = true;

            OpenDoors();        // ТЗ: двери открываются
            ActivateChests();   // ТЗ: активируются сундуки

            // Обновить навигацию (двери открылись — клетки стали проходимы).
            RoomManager.Instance?.RefreshNav();

            OnCleared?.Invoke(this);
        }

        void CloseDoors()
        {
            foreach (var d in _room.Doors) if (d.type != DoorType.Boss) d.Close();
            RoomManager.Instance?.RefreshNav();
        }

        void OpenDoors()
        {
            foreach (var d in _room.Doors) d.Open();
        }

        void ActivateChests()
        {
            // Сундуки — это дочерние объекты под точками ChestPoints (выключены),
            // которые дизайнер положил в префаб. Включаем их.
            foreach (var c in _room.ChestPoints)
                for (int i = 0; i < c.transform.childCount; i++)
                    c.transform.GetChild(i).gameObject.SetActive(true);
        }
    }
}
