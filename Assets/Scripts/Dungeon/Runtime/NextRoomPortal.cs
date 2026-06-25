using UnityEngine;

namespace Game.Dungeon
{
    /// <summary>
    /// Портал на следующий этаж (замена старого NextFloorTrigger).
    /// Ставится RoomManager'ом на Exit-комнату. Активен только когда босс этажа
    /// зачищен (если он есть). При входе игрока — собирает следующий этаж.
    /// </summary>
    [RequireComponent(typeof(RoomInstance))]
    public class NextRoomPortal : MonoBehaviour
    {
        RoomInstance _room;
        bool _used;

        public void Init(RoomInstance room)
        {
            _room = room;
            var trigger = gameObject.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;

            // Триггер у точки выхода (или по центру комнаты).
            Vector3 center = _room.ExitPoint != null ? _room.ExitPoint.position : _room.WorldBounds.center;
            trigger.size = Vector2.one * 1.5f;
            trigger.offset = transform.InverseTransformPoint(center);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_used || !other.CompareTag("Player")) return;
            if (!ExitUnlocked()) return;

            _used = true;
            RoomManager.Instance?.AdvanceFloor();
        }

        bool ExitUnlocked()
        {
            // Если на этаже есть босс — выход открыт только после его зачистки.
            var boss = RoomManager.Instance?.Graph?.Boss;
            return boss == null || boss.Cleared;
        }
    }
}
