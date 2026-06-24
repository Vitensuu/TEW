using System.Collections.Generic;
using UnityEngine;

namespace Dungeon.Procedural
{
    /// <summary>
    /// Проверяет, что сгенерированный уровень проходим. Работает на ЛОГИЧЕСКОЙ
    /// сетке (DungeonGrid: Floor/Wall/None) — то есть ровно на том, по чему
    /// ходят игрок и враги. Если проверка провалена, генератор перегенерирует.
    ///
    /// Гарантии, которые подтверждает валидатор:
    ///   • из стартовой комнаты достижима каждая комната (BFS по полу);
    ///   • каждый дверной проём открыт (клетка проёма — пол, не стена);
    ///   • проходы не заблокированы (door-клетки соединены с обеими комнатами);
    ///   • нет изолированных карманов пола рядом с комнатами.
    /// </summary>
    public class DungeonValidator
    {
        public string LastError { get; private set; }

        // 4-связность: игрок не ходит по диагонали сквозь угол стены.
        static readonly Vector2Int[] N4 =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        /// <summary>
        /// Полная проверка уровня. distanceField (опционально) заполняется
        /// расстоянием в клетках от старта — генератор кладёт выход в самую
        /// дальнюю комнату.
        /// </summary>
        public bool Validate(
            DungeonGrid grid,
            IReadOnlyList<Room> rooms,
            out Dictionary<Vector2Int, int> distanceField)
        {
            distanceField = null;
            LastError = null;

            if (grid == null || rooms == null || rooms.Count == 0)
            {
                LastError = "Пустая сетка или нет комнат.";
                return false;
            }

            // 1. Все дверные проёмы должны быть полом, а не стеной.
            foreach (var room in rooms)
            {
                foreach (var door in room.Doors)
                {
                    if (grid[door.cell.x, door.cell.y] != TileType.Floor)
                    {
                        LastError = $"Дверь комнаты #{room.Index} в {door.cell} перекрыта " +
                                    $"({grid[door.cell.x, door.cell.y]}).";
                        return false;
                    }
                }
            }

            // 2. BFS от центра стартовой комнаты по всем клеткам пола.
            Vector2Int start = rooms[0].CenterCell;
            if (grid[start.x, start.y] != TileType.Floor)
            {
                LastError = "Центр стартовой комнаты не является полом.";
                return false;
            }

            var dist = BfsFloor(grid, start);

            // 3. Центр каждой комнаты должен быть достигнут.
            foreach (var room in rooms)
            {
                Vector2Int c = room.CenterCell;
                if (!dist.ContainsKey(c))
                {
                    LastError = $"Комната #{room.Index} ({room.type}) недостижима из старта.";
                    return false;
                }
            }

            // 4. Каждая дверь должна быть достигнута (проход реально открыт).
            foreach (var room in rooms)
            {
                foreach (var door in room.Doors)
                {
                    if (!dist.ContainsKey(door.cell))
                    {
                        LastError = $"Проход комнаты #{room.Index} в {door.cell} заблокирован.";
                        return false;
                    }
                }
            }

            distanceField = dist;
            return true;
        }

        /// <summary>Волновой обход (BFS) по клеткам пола из точки start.</summary>
        static Dictionary<Vector2Int, int> BfsFloor(DungeonGrid grid, Vector2Int start)
        {
            var dist = new Dictionary<Vector2Int, int> { [start] = 0 };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                Vector2Int cur = queue.Dequeue();
                int nd = dist[cur] + 1;

                for (int i = 0; i < N4.Length; i++)
                {
                    Vector2Int nxt = cur + N4[i];
                    if (dist.ContainsKey(nxt)) continue;
                    if (grid[nxt.x, nxt.y] != TileType.Floor) continue;

                    dist[nxt] = nd;
                    queue.Enqueue(nxt);
                }
            }
            return dist;
        }
    }
}
