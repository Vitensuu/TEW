using System.Collections.Generic;
using UnityEngine;

namespace Game.Dungeon
{
    /// <summary>
    /// Библиотека всех готовых комнат (ТЗ ЭТАП 1 + «RoomLibrary»).
    /// SO-ассет: дизайнер кладёт сюда все RoomData. Ассемблер запрашивает
    /// подходящий префаб по типу/этажу с учётом веса и БЕЗ повтора подряд
    /// (ТЗ ЭТАП 3 — «избегать повторения одинаковых комнат подряд»).
    /// </summary>
    [CreateAssetMenu(menuName = "TEW/Rooms/RoomLibrary", fileName = "RoomLibrary")]
    public class RoomLibrary : ScriptableObject
    {
        [SerializeField] List<RoomData> rooms = new List<RoomData>();

        // последний выданный ID по каждому типу — чтобы не повторять подряд
        readonly Dictionary<RoomType, string> _lastByType = new Dictionary<RoomType, string>();

        public IReadOnlyList<RoomData> All => rooms;

        public void ResetHistory() => _lastByType.Clear();

        /// <summary>Все комнаты заданного типа, подходящие под этаж.</summary>
        public List<RoomData> Query(RoomType type, int floor)
        {
            var result = new List<RoomData>();
            foreach (var r in rooms)
                if (r != null && r.roomType == type && r.FitsFloor(floor) && r.prefab != null)
                    result.Add(r);
            return result;
        }

        /// <summary>
        /// Случайная комната типа type для этажа floor (взвешенно), избегая
        /// той же, что выдали прошлый раз для этого типа.
        /// </summary>
        public RoomData Pick(RoomType type, int floor, System.Random rng)
        {
            var pool = Query(type, floor);
            if (pool.Count == 0) return null;

            // Отфильтровать «повтор подряд», если есть альтернатива.
            if (pool.Count > 1 && _lastByType.TryGetValue(type, out var lastId))
                pool.RemoveAll(r => r.RoomID == lastId);

            float total = 0f;
            foreach (var r in pool) total += Mathf.Max(0.0001f, r.spawnWeight);

            double roll = rng.NextDouble() * total;
            float acc = 0f;
            RoomData chosen = pool[pool.Count - 1];
            foreach (var r in pool)
            {
                acc += Mathf.Max(0.0001f, r.spawnWeight);
                if (roll <= acc) { chosen = r; break; }
            }

            _lastByType[type] = chosen.RoomID;
            return chosen;
        }

        public bool HasType(RoomType type)
        {
            foreach (var r in rooms) if (r != null && r.roomType == type) return true;
            return false;
        }
    }
}
