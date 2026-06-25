using UnityEngine;

namespace Game.Dungeon
{
    /// <summary>
    /// Стороны света для дверей/соединений (ТЗ «Система дверей»: North/South/East/West).
    /// Двери стыкуются «противоположностями»: North↔South, East↔West.
    /// </summary>
    public enum Direction { North, South, East, West }

    public static class DirectionExtensions
    {
        /// <summary>Противоположное направление (для стыковки дверей).</summary>
        public static Direction Opposite(this Direction d) => d switch
        {
            Direction.North => Direction.South,
            Direction.South => Direction.North,
            Direction.East  => Direction.West,
            _               => Direction.East,
        };

        /// <summary>Единичный вектор направления в мире (top-down, Y вверх).</summary>
        public static Vector2Int ToVector(this Direction d) => d switch
        {
            Direction.North => Vector2Int.up,
            Direction.South => Vector2Int.down,
            Direction.East  => Vector2Int.right,
            _               => Vector2Int.left,
        };

        /// <summary>Угол поворота (град) для трансформа, смотрящего в это направление.</summary>
        public static float ToAngle(this Direction d) => d switch
        {
            Direction.North => 0f,
            Direction.East  => -90f,
            Direction.South => 180f,
            _               => 90f,
        };

        /// <summary>
        /// На сколько шагов по часовой стрелке (×90°) повернуть, чтобы from стало to.
        /// Используется ассемблером для вращения комнаты при стыковке дверей.
        /// </summary>
        public static int StepsTo(this Direction from, Direction to)
        {
            int Idx(Direction d) => d switch
            {
                Direction.North => 0, Direction.East => 1,
                Direction.South => 2, _ => 3
            };
            return (Idx(to) - Idx(from) + 4) % 4;
        }

        /// <summary>Повернуть направление на step×90° по часовой стрелке.</summary>
        public static Direction Rotate(this Direction d, int stepsCW)
        {
            int Idx(Direction x) => x switch
            {
                Direction.North => 0, Direction.East => 1,
                Direction.South => 2, _ => 3
            };
            Direction[] order = { Direction.North, Direction.East, Direction.South, Direction.West };
            return order[(Idx(d) + stepsCW) % 4];
        }
    }
}
