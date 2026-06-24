using UnityEngine;

namespace Dungeon.Procedural
{
    /// <summary>
    /// Четыре стороны соединения дверей. Без диагоналей —
    /// для top-down 2D подземелья со спрайтовыми стенами этого достаточно.
    /// </summary>
    public enum Direction { North, East, South, West }

    public static class DirectionExtensions
    {
        /// <summary>Противоположное направление (для стыковки дверей).</summary>
        public static Direction Opposite(this Direction d) => d switch
        {
            Direction.North => Direction.South,
            Direction.South => Direction.North,
            Direction.East  => Direction.West,
            Direction.West  => Direction.East,
            _               => Direction.North
        };

        /// <summary>Единичное смещение в клетках сетки.</summary>
        public static Vector2Int ToCell(this Direction d) => d switch
        {
            Direction.North => Vector2Int.up,
            Direction.South => Vector2Int.down,
            Direction.East  => Vector2Int.right,
            Direction.West  => Vector2Int.left,
            _               => Vector2Int.zero
        };

        /// <summary>Единичное смещение в мировых координатах (cellSize = 1).</summary>
        public static Vector2 ToWorld(this Direction d)
        {
            var c = d.ToCell();
            return new Vector2(c.x, c.y);
        }
    }
}
