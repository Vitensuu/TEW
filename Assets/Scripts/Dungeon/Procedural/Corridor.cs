using System.Collections.Generic;
using UnityEngine;

namespace Dungeon.Procedural
{
    /// <summary>
    /// Коридор между двумя комнатами. Хранит список клеток пола (уже с учётом
    /// ширины прохода) и две двери-конца. Геометрия — Г-образная (один излом),
    /// ширина настраивается (минимум 2 клетки по требованиям).
    ///
    /// Коридор НЕ ставит стены сам: он лишь вырезает пол. Стены вокруг всего
    /// пола (комнаты + коридоры) строит DungeonGrid.BuildWalls() одним проходом,
    /// поэтому проход физически не может оказаться перекрытым стеной.
    /// </summary>
    public class Corridor
    {
        public readonly Room A;
        public readonly Room B;
        public readonly DoorCell DoorA;
        public readonly DoorCell DoorB;

        /// <summary>Все клетки пола коридора (включая ширину).</summary>
        public readonly HashSet<Vector2Int> FloorCells = new HashSet<Vector2Int>();

        public Corridor(Room a, Room b, DoorCell doorA, DoorCell doorB)
        {
            A = a; B = b;
            DoorA = doorA; DoorB = doorB;
        }

        /// <summary>
        /// Прокладывает Г-образный путь width-клеток шириной от центра двери A
        /// до центра двери B. Сначала по X, потом по Y (или наоборот — случайно).
        /// </summary>
        public void Build(int width, System.Random rng)
        {
            Vector2Int from = DoorA.cell;
            Vector2Int to   = DoorB.cell;

            Vector2Int corner = rng.NextDouble() < 0.5
                ? new Vector2Int(to.x, from.y)
                : new Vector2Int(from.x, to.y);

            CarveThick(from, corner, width);
            CarveThick(corner, to,   width);
        }

        /// <summary>Толстая прямая линия (горизонталь или вертикаль).</summary>
        void CarveThick(Vector2Int from, Vector2Int to, int width)
        {
            int half = Mathf.Max(1, width) / 2;

            if (from.y == to.y) // горизонталь
            {
                int minX = Mathf.Min(from.x, to.x);
                int maxX = Mathf.Max(from.x, to.x);
                for (int x = minX; x <= maxX; x++)
                    for (int w = -half; w < width - half; w++)
                        FloorCells.Add(new Vector2Int(x, from.y + w));
            }
            else // вертикаль
            {
                int minY = Mathf.Min(from.y, to.y);
                int maxY = Mathf.Max(from.y, to.y);
                for (int y = minY; y <= maxY; y++)
                    for (int w = -half; w < width - half; w++)
                        FloorCells.Add(new Vector2Int(from.x + w, y));
            }
        }
    }
}
