using System.Collections.Generic;
using UnityEngine;

namespace Dungeon.Procedural
{
    /// <summary>
    /// Чистые данные о комнате в КЛЕТОЧНЫХ координатах сетки (cellSize = 1).
    /// Это НЕ MonoBehaviour: генерация работает на данных, а уже потом
    /// результат растеризуется в Tilemap'ы и в DungeonGrid для A*.
    ///
    /// Прямоугольник <see cref="Bounds"/> описывает ПОЛ комнаты (внутреннюю
    /// площадь). Кольцо стен рисуется ВОКРУГ него, на клетках, не входящих
    /// в Bounds, поэтому пол и стены никогда не пересекаются.
    /// </summary>
    public class Room
    {
        /// <summary>Площадь пола комнаты в клетках сетки.</summary>
        public RectInt Bounds;

        /// <summary>Роль комнаты (старт/босс/магазин/…).</summary>
        public RoomType type = RoomType.Normal;

        /// <summary>Индекс комнаты в списке генератора (старт = 0).</summary>
        public int Index;

        /// <summary>Дверные проёмы этой комнаты (клетки на кольце стены).</summary>
        public readonly List<DoorCell> Doors = new List<DoorCell>();

        public Room(RectInt bounds, RoomType type)
        {
            Bounds = bounds;
            this.type = type;
        }

        /// <summary>Центр комнаты в клетках (для графа/MST и спавна выхода).</summary>
        public Vector2Int CenterCell => new Vector2Int(
            Bounds.x + Bounds.width  / 2,
            Bounds.y + Bounds.height / 2);

        /// <summary>
        /// Мировые границы пола (cellSize = 1, мир совпадает с клетками).
        /// Используется RoomPopulator'ом и спавном врагов/лута.
        /// </summary>
        public Bounds WorldBounds => new Bounds(
            new Vector3(Bounds.x + Bounds.width  * 0.5f,
                        Bounds.y + Bounds.height * 0.5f, 0f),
            new Vector3(Bounds.width, Bounds.height, 1f));

        /// <summary>Пересекается ли (с зазором padding) с другой комнатой.</summary>
        public bool Overlaps(Room other, int padding)
        {
            return Bounds.xMin - padding < other.Bounds.xMax &&
                   Bounds.xMax + padding > other.Bounds.xMin &&
                   Bounds.yMin - padding < other.Bounds.yMax &&
                   Bounds.yMax + padding > other.Bounds.yMin;
        }
    }

    /// <summary>
    /// Дверной проём: клетка на кольце стены, через которую проходит коридор.
    /// На этой клетке стена НЕ ставится (см. TilemapPainter / BuildWalls).
    /// </summary>
    public struct DoorCell
    {
        public Vector2Int cell;       // клетка проёма
        public Direction  direction;  // в какую сторону смотрит из комнаты

        public DoorCell(Vector2Int cell, Direction direction)
        {
            this.cell = cell;
            this.direction = direction;
        }
    }
}
