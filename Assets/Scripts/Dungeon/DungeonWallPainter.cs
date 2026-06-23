using UnityEngine;
using UnityEngine.Tilemaps;

namespace Dungeon
{
    /// <summary>
    /// Расставляет тайлы стен на основе соседей в DungeonGrid.
    ///
    /// СТРУКТУРА КОРОБКИ (тип 1) — 4 строки по Y, пол под строками 1-2:
    ///
    ///   Y+3: [CornerTopLeft]  [WallTop]      [CornerTopRight]   ← строка 1 (крыша)
    ///   Y+2: [WallLeft]       [Floor!]        [WallRight]       ← строка 2 (боковые)
    ///   Y+1: [FaceLeft_Top]   [WallFace_Top]  [FaceRight_Top]   ← строка 3 (лицо верх)
    ///   Y+0: [FaceLeft_Bot]   [WallFace_Bot]  [FaceRight_Bot]   ← строка 4 (лицо низ)
    ///
    ///   Пол (floorTilemap) рисуется на Y+3 и Y+2 поверх wallTilemap.
    ///
    /// СЛОИ Tilemap (sortingOrder):
    ///   floorTilemap  (0) — пол
    ///   wallTilemap   (1) — лицевая стена (строки 3-4) + тип2 стены
    ///   ledgeTilemap  (2) — крыша и боковые коробки (строки 1-2)
    ///
    /// ЛОГИКА ОПРЕДЕЛЕНИЯ:
    ///   Тип 1 (снаружи) — стена с полом на ЮГЕ (y-1). Игрок смотрит снизу.
    ///   Тип 2 (изнутри) — стена с полом на СЕВЕРЕ (y+1). Игрок внутри.
    /// </summary>
    public static class DungeonWallPainter
    {
        public static void Paint(
            DungeonGrid        grid,
            DungeonWallTileSet ts,
            Tilemap            wallTilemap,
            Tilemap            ledgeTilemap)
        {
            for (int x = 0; x < grid.Width; x++)
            for (int y = 0; y < grid.Height; y++)
            {
                if (grid[x, y] != TileType.Wall) continue;
                PaintCell(grid, ts, wallTilemap, ledgeTilemap, x, y);
            }
        }

        private static void PaintCell(
            DungeonGrid grid, DungeonWallTileSet ts,
            Tilemap wall, Tilemap ledge,
            int x, int y)
        {
            bool fN  = IsFloor(grid, x,     y + 1);
            bool fS  = IsFloor(grid, x,     y - 1);
            bool fE  = IsFloor(grid, x + 1, y    );
            bool fW  = IsFloor(grid, x - 1, y    );

            bool wN  = IsWall(grid, x,     y + 1);
            bool wS  = IsWall(grid, x,     y - 1);
            bool wE  = IsWall(grid, x + 1, y    );
            bool wW  = IsWall(grid, x - 1, y    );

            // Пол на две клетки южнее (строка 2 коробки)
            bool fS2 = IsFloor(grid, x, y - 2);
            // Стена на две клетки южнее (строка 3 — лицевая)
            bool wS2 = IsWall(grid, x, y - 2);

            var pos = new Vector3Int(x, y, 0);

            // ══════════════════════════════════════════════════════════════════
            // ТИП 1 — СТРОКА 4: лицевая стена низ (y, пол на y-1)
            // Это самая нижняя видимая строка коробки
            // ══════════════════════════════════════════════════════════════════
            if (fS && !fN)
            {
                // Левый угол лицевой низ
                if (!wW && wE)
                {
                    wall.SetTile(pos, ts.t1_FaceLeft_Bot);
                    return;
                }
                // Правый угол лицевой низ
                if (wW && !wE)
                {
                    wall.SetTile(pos, ts.t1_FaceRight_Bot);
                    return;
                }
                // Середина лицевой низ
                wall.SetTile(pos, DungeonWallTileSet.Pick(ts.t1_WallFace_Bot, x, y));
                return;
            }

            // ══════════════════════════════════════════════════════════════════
            // ТИП 1 — СТРОКА 3: лицевая стена верх (y, пол на y-2)
            // ══════════════════════════════════════════════════════════════════
            if (!fS && !fN && fS2 && wS)
            {
                // Левый угол лицевой верх
                if (!wW && wE)
                {
                    wall.SetTile(pos, ts.t1_FaceLeft_Top);
                    return;
                }
                // Правый угол лицевой верх
                if (wW && !wE)
                {
                    wall.SetTile(pos, ts.t1_FaceRight_Top);
                    return;
                }
                // Середина лицевой верх
                wall.SetTile(pos, DungeonWallTileSet.Pick(ts.t1_WallFace_Top, x, y));
                return;
            }

            // ══════════════════════════════════════════════════════════════════
            // ТИП 1 — СТРОКА 2: боковые стены + пол под ними
            // (y, пол на y-3 или стена-лицевая на y-1 и y-2)
            // ══════════════════════════════════════════════════════════════════
            if (!fS && !fN && !fE && !fW && wS && IsWall(grid, x, y - 1))
            {
                // Левая боковая стена коробки
                if (!wW && wE)
                {
                    ledge.SetTile(pos, DungeonWallTileSet.Pick(ts.t1_WallLeft, x, y));
                    return;
                }
                // Правая боковая стена коробки
                if (wW && !wE)
                {
                    ledge.SetTile(pos, DungeonWallTileSet.Pick(ts.t1_WallRight, x, y));
                    return;
                }
            }

            // ══════════════════════════════════════════════════════════════════
            // ТИП 1 — СТРОКА 1: верх коробки / крыша
            // (y, стена на y-1 которая является боковой строкой 2)
            // ══════════════════════════════════════════════════════════════════
            if (!fS && !fN && !fE && !fW && wS && IsWall(grid, x, y - 2) && IsWall(grid, x, y - 1))
            {
                // Верхний левый угол
                if (!wW && wE)
                {
                    ledge.SetTile(pos, ts.t1_CornerTopLeft);
                    return;
                }
                // Верхний правый угол
                if (wW && !wE)
                {
                    ledge.SetTile(pos, ts.t1_CornerTopRight);
                    return;
                }
                // Верхняя стена (крыша)
                ledge.SetTile(pos, DungeonWallTileSet.Pick(ts.t1_WallTop, x, y));
                return;
            }

            // ══════════════════════════════════════════════════════════════════
            // ТИП 2 — верхняя стена изнутри (пол с севера, y+1)
            // ══════════════════════════════════════════════════════════════════
            if (fN && !fS)
            {
                // Верхний левый угол изнутри
                if (!wW && wE)
                {
                    wall.SetTile(pos, ts.t2_CornerTopLeft);
                    return;
                }
                // Верхний правый угол изнутри (зеркало, пока тот же тайл)
                if (wW && !wE)
                {
                    wall.SetTile(pos, ts.t2_CornerTopLeft);
                    return;
                }
                // Середина верхней стены изнутри
                wall.SetTile(pos, DungeonWallTileSet.Pick(ts.t1_WallFace_Top, x, y));
                return;
            }

            // ══════════════════════════════════════════════════════════════════
            // ТИП 2 — нижняя стена изнутри (1 тайл, пол на y+2)
            // ══════════════════════════════════════════════════════════════════
            if (!fN && IsFloor(grid, x, y + 2) && wN)
            {
                if (!wW && wE)
                {
                    wall.SetTile(pos, ts.t2_CornerBotLeft);
                    return;
                }
                if (wW && !wE)
                {
                    wall.SetTile(pos, ts.t2_CornerBotRight);
                    return;
                }
                wall.SetTile(pos, DungeonWallTileSet.Pick(ts.t2_WallBot, x, y));
                return;
            }

            // ══════════════════════════════════════════════════════════════════
            // ТИП 2 — боковые стены изнутри
            // ══════════════════════════════════════════════════════════════════
            if (fE && !fW && !fN && !fS)
            {
                bool isTop = IsWall(grid, x, y + 1) && IsFloor(grid, x, y + 2);
                wall.SetTile(pos, isTop ? ts.t2_WallRight_Top : ts.t2_WallRight_Bot);
                return;
            }
            if (fW && !fE && !fN && !fS)
            {
                bool isTop = IsWall(grid, x, y + 1) && IsFloor(grid, x, y + 2);
                wall.SetTile(pos, isTop ? ts.t2_WallLeft_Top : ts.t2_WallLeft_Bot);
                return;
            }

            // ══════════════════════════════════════════════════════════════════
            // КОРИДОРЫ — боковые стены (пол слева или справа)
            // ══════════════════════════════════════════════════════════════════
            if (fE && !fW)
            {
                wall.SetTile(pos, DungeonWallTileSet.Pick(ts.sideWall_R, x, y));
                return;
            }
            if (fW && !fE)
            {
                wall.SetTile(pos, DungeonWallTileSet.Pick(ts.sideWall_L, x, y));
                return;
            }
        }

        // ── Хелперы ──────────────────────────────────────────────────────────

        private static bool IsFloor(DungeonGrid grid, int x, int y) =>
            grid.IsInBounds(x, y) && grid[x, y] == TileType.Floor;

        private static bool IsWall(DungeonGrid grid, int x, int y) =>
            grid.IsInBounds(x, y) && grid[x, y] == TileType.Wall;
    }
}
