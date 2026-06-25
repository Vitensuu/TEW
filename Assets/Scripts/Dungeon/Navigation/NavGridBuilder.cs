using UnityEngine;

namespace Game.Dungeon
{
    /// <summary>
    /// Строит <see cref="NavGrid"/> сканированием реальных коллайдеров сцены
    /// (ТЗ: «A* должен учитывать стены, препятствия, двери, динамические объекты»).
    /// Клетка проходима, если в ней НЕТ коллайдера со слоя obstacleMask.
    /// Поскольку двери — это коллайдеры (DoorController.blocker), закрытая дверь
    /// автоматически делает клетку непроходимой; открытая — проходимой после Refresh.
    /// </summary>
    public static class NavGridBuilder
    {
        /// <summary>
        /// world bounds — суммарные габариты собранного этажа; obstacleMask — слой
        /// стен/препятствий; cellSize — обычно 1 (тайл). pad — запас по краям.
        /// </summary>
        public static NavGrid Build(Bounds worldBounds, LayerMask obstacleMask,
            float cellSize = 1f, float pad = 1f)
        {
            Vector2 origin = new Vector2(worldBounds.min.x - pad, worldBounds.min.y - pad);
            int w = Mathf.CeilToInt((worldBounds.size.x + pad * 2f) / cellSize);
            int h = Mathf.CeilToInt((worldBounds.size.y + pad * 2f) / cellSize);
            w = Mathf.Max(1, w); h = Mathf.Max(1, h);

            var grid = new NavGrid(w, h, cellSize, origin);
            Scan(grid, obstacleMask);
            return grid;
        }

        /// <summary>Перепроверить проходимость (после открытия дверей / движения объектов).</summary>
        public static void Refresh(NavGrid grid, LayerMask obstacleMask) => Scan(grid, obstacleMask);

        static void Scan(NavGrid grid, LayerMask obstacleMask)
        {
            float half = grid.CellSize * 0.45f;
            var box = new Vector2(half * 2f, half * 2f);

            for (int x = 0; x < grid.Width; x++)
                for (int y = 0; y < grid.Height; y++)
                {
                    Vector3 center = grid.CellToWorldCenter(new Vector2Int(x, y));
                    bool blocked = Physics2D.OverlapBox(center, box, 0f, obstacleMask) != null;
                    grid.SetWalkable(x, y, !blocked);
                }
        }
    }
}
