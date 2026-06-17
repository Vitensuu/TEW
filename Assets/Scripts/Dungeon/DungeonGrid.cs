using UnityEngine;

namespace Dungeon
{
    public enum TileType : byte { None = 0, Floor = 1, Wall = 2 }

    /// <summary>
    /// Двумерная логическая сетка подземелья. Содержит "вырезание" комнат,
    /// прокладку коридоров и построение стен вокруг пола.
    /// Никак не зависит от Tilemap — это чистая логика, её можно
    /// при желании покрыть unit-тестами отдельно от Unity-сцены.
    /// </summary>
    public class DungeonGrid
    {
        public int Width { get; }
        public int Height { get; }
        private readonly TileType[,] _cells;

        public DungeonGrid(int width, int height)
        {
            Width = width;
            Height = height;
            _cells = new TileType[width, height];
        }

        public TileType this[int x, int y]
        {
            get => IsInBounds(x, y) ? _cells[x, y] : TileType.None;
            set { if (IsInBounds(x, y)) _cells[x, y] = value; }
        }

        public bool IsInBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public void CarveRoom(RectInt area)
        {
            for (int x = area.x; x < area.xMax; x++)
                for (int y = area.y; y < area.yMax; y++)
                    this[x, y] = TileType.Floor;
        }

        /// <summary>
        /// Прокладывает Г-образный коридор заданной ширины между двумя точками.
        /// </summary>
        public void CarveCorridor(Vector2Int from, Vector2Int to, int width, System.Random rng)
        {
            Vector2Int corner = rng.NextDouble() < 0.5
                ? new Vector2Int(to.x, from.y)
                : new Vector2Int(from.x, to.y);

            CarveLine(from, corner, width);
            CarveLine(corner, to, width);
        }

        private void CarveLine(Vector2Int from, Vector2Int to, int width)
        {
            int half = width / 2;
            if (from.y == to.y)
            {
                int minX = Mathf.Min(from.x, to.x);
                int maxX = Mathf.Max(from.x, to.x);
                for (int x = minX; x <= maxX; x++)
                    for (int w = -half; w < width - half; w++)
                        this[x, from.y + w] = TileType.Floor;
            }
            else
            {
                int minY = Mathf.Min(from.y, to.y);
                int maxY = Mathf.Max(from.y, to.y);
                for (int y = minY; y <= maxY; y++)
                    for (int w = -half; w < width - half; w++)
                        this[from.x + w, y] = TileType.Floor;
            }
        }

        /// <summary>
        /// Помечает стенами все пустые клетки, граничащие с полом.
        /// Вызывать один раз, после того как вырезаны все комнаты и коридоры.
        /// </summary>
        public void BuildWalls()
        {
            var snapshot = (TileType[,])_cells.Clone();

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (snapshot[x, y] != TileType.None) continue;
                    if (HasFloorNeighbor(snapshot, x, y))
                        _cells[x, y] = TileType.Wall;
                }
            }
        }

        private bool HasFloorNeighbor(TileType[,] snapshot, int x, int y)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= Width || ny >= Height) continue;
                    if (snapshot[nx, ny] == TileType.Floor) return true;
                }
            }
            return false;
        }
    }
}
