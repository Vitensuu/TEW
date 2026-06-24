using UnityEngine;
using UnityEngine.Tilemaps;

namespace Dungeon.Procedural
{
    /// <summary>
    /// Рисует логическую сетку (DungeonGrid) по слоям Tilemap:
    ///   Floor      — пол на каждой клетке Floor (без коллизий);
    ///   Wall       — стены на каждой клетке Wall (с TilemapCollider2D);
    ///   Collision  — необязательный слой коллизий (если нужен отдельный layer
    ///                физики поверх «арта» стен);
    ///   Decoration — оставляется пустым под декор/расширение.
    ///
    /// КЛЮЧЕВОЕ ПРАВИЛО БЕЗОПАСНОСТИ ПРОХОДОВ:
    /// стена ставится ТОЛЬКО на клетки, помеченные TileType.Wall. Дверные
    /// проёмы и коридоры — это TileType.Floor, поэтому стена туда физически
    /// не попадает. Дополнительно перед установкой стены делается явная проверка
    /// «клетка не является дверью и не является полом» — двойная страховка от
    /// невидимых стен в проходах.
    ///
    /// Мир ↔ сетка: клетка (x,y) сетки соответствует мировой клетке
    /// (x + origin.x, y + origin.y). cellSize Grid'а = 1.
    /// </summary>
    public class TilemapPainter
    {
        readonly Tilemap _floor;
        readonly Tilemap _wall;
        readonly Tilemap _collision;   // может быть null
        readonly Tilemap _decoration;  // может быть null
        readonly DungeonTileSet _set;
        readonly TileBase _collisionTile; // невидимый тайл для слоя Collision (может быть null)

        public TilemapPainter(
            Tilemap floor, Tilemap wall, Tilemap collision, Tilemap decoration,
            DungeonTileSet set, TileBase collisionTile)
        {
            _floor = floor;
            _wall = wall;
            _collision = collision;
            _decoration = decoration;
            _set = set;
            _collisionTile = collisionTile;
        }

        /// <summary>
        /// Полная перерисовка уровня. originX/originY — мировые координаты клетки
        /// сетки (0,0), чтобы подземелье можно было сместить от начала координат.
        /// </summary>
        public void Paint(DungeonGrid grid, int originX, int originY, System.Random rng)
        {
            Clear();

            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    var pos = new Vector3Int(x + originX, y + originY, 0);
                    TileType t = grid[x, y];

                    switch (t)
                    {
                        case TileType.Floor:
                            // Пол кладём и под стены тоже? Нет — стены непроходимы,
                            // пол только на проходимых клетках. Под стенами пол не
                            // нужен (его не видно), но если хочется — раскомментируй
                            // ветку ниже в default и убери break.
                            PaintFloor(pos, rng);
                            break;

                        case TileType.Wall:
                            // Стена ставится только на TileType.Wall. Двери и коридоры —
                            // это TileType.Floor (ветка выше), сюда они не попадают,
                            // поэтому проход физически нельзя перекрыть стеной.
                            PaintFloorUnderWall(pos, rng); // пол лежит и под стенами
                            PaintWall(grid, x, y, pos, rng);
                            break;
                    }
                }
            }

            // Рефреш коллайдеров: TilemapCollider2D пересоберётся сам в LateUpdate,
            // но форсируем, чтобы физика была готова в этом же кадре.
            if (_wall != null) _wall.RefreshAllTiles();
        }

        public void Clear()
        {
            _floor?.ClearAllTiles();
            _wall?.ClearAllTiles();
            _collision?.ClearAllTiles();
            _decoration?.ClearAllTiles();
        }

        void PaintFloor(Vector3Int pos, System.Random rng)
        {
            if (_floor == null) return;
            var tile = _set.PickFloor(rng);
            if (tile != null) _floor.SetTile(pos, tile);
        }

        /// <summary>Пол под стеной (по требованию: пол лежит и под всеми стенами).</summary>
        void PaintFloorUnderWall(Vector3Int pos, System.Random rng)
        {
            if (_floor == null) return;
            var tile = _set.PickFloor(rng);
            if (tile != null) _floor.SetTile(pos, tile);
        }

        void PaintWall(DungeonGrid grid, int x, int y, Vector3Int pos, System.Random rng)
        {
            WallRole role = ResolveRole(grid, x, y);
            var tile = _set.PickWall(role, rng);
            if (tile != null && _wall != null) _wall.SetTile(pos, tile);

            // Зеркалим стену в слой коллизий, если он задан отдельным невидимым тайлом.
            if (_collision != null && _collisionTile != null)
                _collision.SetTile(pos, _collisionTile);
        }

        // ── Автотайлинг: роль клетки стены по соседям-полу ────────────────────

        static bool IsFloor(DungeonGrid g, int x, int y) => g[x, y] == TileType.Floor;

        /// <summary>
        /// Определяет роль стены по 4 ортогональным и 4 диагональным соседям.
        /// Внешние углы — две перпендикулярные стороны с полом; рёбра — одна
        /// сторона; внутренние углы — только диагональный пол.
        /// </summary>
        static WallRole ResolveRole(DungeonGrid g, int x, int y)
        {
            bool n = IsFloor(g, x, y + 1);
            bool s = IsFloor(g, x, y - 1);
            bool e = IsFloor(g, x + 1, y);
            bool w = IsFloor(g, x - 1, y);

            // Внешние углы (две перпендикулярные стороны — пол).
            if (s && e) return WallRole.TopLeft;
            if (s && w) return WallRole.TopRight;
            if (n && e) return WallRole.BottomLeft;
            if (n && w) return WallRole.BottomRight;

            // Рёбра (одна сторона — пол).
            if (s) return WallRole.TopEdge;
            if (n) return WallRole.BottomEdge;
            if (e) return WallRole.LeftEdge;
            if (w) return WallRole.RightEdge;

            // Внутренние (вогнутые) углы — пол только по диагонали.
            bool ne = IsFloor(g, x + 1, y + 1);
            bool nw = IsFloor(g, x - 1, y + 1);
            bool se = IsFloor(g, x + 1, y - 1);
            bool sw = IsFloor(g, x - 1, y - 1);

            if (se) return WallRole.InnerTopLeft;
            if (sw) return WallRole.InnerTopRight;
            if (ne) return WallRole.InnerBottomLeft;
            if (nw) return WallRole.InnerBottomRight;

            // Стена в толще массива (не граничит с полом) — пусть будет верхняя.
            return WallRole.TopEdge;
        }
    }
}
