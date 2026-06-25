using UnityEngine;

namespace Game.Dungeon
{
    /// <summary>
    /// Логическая сетка проходимости для A* (заменяет старый DungeonGrid).
    /// Не зависит от способа генерации — строится из РЕАЛЬНОЙ геометрии сцены
    /// (коллайдеров стен/препятствий) через <see cref="NavGridBuilder"/>.
    /// Хранит мировой origin и cellSize, поэтому корректно конвертит world↔cell
    /// для собранных вручную комнат с произвольным расположением.
    /// </summary>
    public class NavGrid
    {
        public int Width  { get; }
        public int Height { get; }
        public float CellSize { get; }
        public Vector2 Origin { get; }   // мировая позиция нижнего-левого угла клетки (0,0)

        readonly bool[,] _walkable;

        public NavGrid(int width, int height, float cellSize, Vector2 origin)
        {
            Width = width; Height = height;
            CellSize = cellSize; Origin = origin;
            _walkable = new bool[width, height];
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public bool IsWalkable(int x, int y) => InBounds(x, y) && _walkable[x, y];
        public bool IsWalkable(Vector2Int c) => IsWalkable(c.x, c.y);

        public void SetWalkable(int x, int y, bool value)
        {
            if (InBounds(x, y)) _walkable[x, y] = value;
        }

        public Vector2Int WorldToCell(Vector3 world) => new Vector2Int(
            Mathf.FloorToInt((world.x - Origin.x) / CellSize),
            Mathf.FloorToInt((world.y - Origin.y) / CellSize));

        public Vector3 CellToWorldCenter(Vector2Int c) => new Vector3(
            Origin.x + (c.x + 0.5f) * CellSize,
            Origin.y + (c.y + 0.5f) * CellSize, 0f);
    }
}
