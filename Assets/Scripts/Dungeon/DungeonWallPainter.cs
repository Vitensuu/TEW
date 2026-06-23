using UnityEngine;
using UnityEngine.Tilemaps;

namespace Dungeon
{
    /// <summary>
    /// Расставляет тайлы стен на основе соседей в DungeonGrid.
    ///
    /// ═══ СТРУКТУРА ТИП 1 (снаружи) ═══
    ///
    ///   Y+3: [CornerTopLeft]   [WallTop...]      [CornerTopRight]    ← строка 1, ledge
    ///   Y+2: [WallLeft...]     [пустой]           [WallRight...]      ← строка 2, ledge
    ///   Y+1: [FaceLeft_Top]    [WallFace_Top...]  [FaceRight_Top]     ← строка 3, wall
    ///   Y+0: [FaceLeft_Bot]    [WallFace_Bot...]  [FaceRight_Bot]     ← строка 4, wall
    ///   Пол рисуется поверх Y+3 и Y+2 (другой Tilemap-слой).
    ///
    /// ═══ СТРУКТУРА ТИП 2 (изнутри) ═══
    ///
    ///   тайл1: [T2_CornerTopLeft] [T2_WallLeft_Top]  [WallFace_Top...] [T2_WallRight_Top] [T2_CornerTopRight]
    ///   тайл2: [T2_SideRight...]  [T2_WallLeft_Bot]  [WallFace_Bot...] [T2_WallRight_Bot] [T2_SideLeft...]
    ///   тайл3: [T2_SideRight...]  [пустой]           [пустой]          [пустой]           [T2_SideLeft...]
    ///   тайл4: [T2_CornerBotLeft] [T2_WallBot...]                                          [T2_CornerBotRight]
    ///
    ///   Боковые для тип2:
    ///     пол слева  (fW) → стена справа → t2_SideRight (idx 44,45,46,8)
    ///     пол справа (fE) → стена слева  → t2_SideLeft  (idx 47,48,49,9)
    ///
    /// ═══ ЛОГИКА ОПРЕДЕЛЕНИЯ ТИПА ═══
    ///   Тип 1 — стена с полом на ЮГЕ (y-1). Игрок смотрит снизу.
    ///   Тип 2 — стена с полом на СЕВЕРЕ (y+1). Игрок внутри.
    ///
    /// ═══ СЛОИ ═══
    ///   wallTilemap  (1) — лицевые стены (строки 3-4 тип1) + все стены тип2
    ///   ledgeTilemap (2) — крыша и боковые коробки (строки 1-2 тип1)
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

            bool fS2 = IsFloor(grid, x, y - 2);  // пол в 2х клетках южнее
            bool fN2 = IsFloor(grid, x, y + 2);  // пол в 2х клетках севернее

            var pos = new Vector3Int(x, y, 0);

            // ══════════════════════════════════════════════════════════════════
            // ТИП 1 — СТРОКА 4: лицевая стена низ (пол на y-1)
            // ══════════════════════════════════════════════════════════════════
            if (fS && !fN)
            {
                if (!wW && wE)
                {
                    wall.SetTile(pos, ts.t1_FaceLeft_Bot);
                    return;
                }
                if (wW && !wE)
                {
                    wall.SetTile(pos, ts.t1_FaceRight_Bot);
                    return;
                }
                wall.SetTile(pos, DungeonWallTileSet.Pick(ts.t1_WallFace_Bot, x, y));
                return;
            }

            // ══════════════════════════════════════════════════════════════════
            // ТИП 1 — СТРОКА 3: лицевая стена верх (пол на y-2)
            // ══════════════════════════════════════════════════════════════════
            if (!fS && !fN && fS2 && wS)
            {
                if (!wW && wE)
                {
                    wall.SetTile(pos, ts.t1_FaceLeft_Top);
                    return;
                }
                if (wW && !wE)
                {
                    wall.SetTile(pos, ts.t1_FaceRight_Top);
                    return;
                }
                wall.SetTile(pos, DungeonWallTileSet.Pick(ts.t1_WallFace_Top, x, y));
                return;
            }

            // ══════════════════════════════════════════════════════════════════
            // ТИП 1 — СТРОКА 2: боковые стены коробки (ledge)
            // ══════════════════════════════════════════════════════════════════
            if (!fS && !fN && !fE && !fW && wS && IsWall(grid, x, y - 1))
            {
                if (!wW && wE)
                {
                    ledge.SetTile(pos, DungeonWallTileSet.Pick(ts.t1_WallLeft, x, y));
                    return;
                }
                if (wW && !wE)
                {
                    ledge.SetTile(pos, DungeonWallTileSet.Pick(ts.t1_WallRight, x, y));
                    return;
                }
            }

            // ══════════════════════════════════════════════════════════════════
            // ТИП 1 — СТРОКА 1: верх коробки / крыша (ledge)
            // ══════════════════════════════════════════════════════════════════
            if (!fS && !fN && !fE && !fW && wS && IsWall(grid, x, y - 1) && IsWall(grid, x, y - 2))
            {
                if (!wW && wE)
                {
                    ledge.SetTile(pos, ts.t1_CornerTopLeft);
                    return;
                }
                if (wW && !wE)
                {
                    ledge.SetTile(pos, ts.t1_CornerTopRight);
                    return;
                }
                ledge.SetTile(pos, DungeonWallTileSet.Pick(ts.t1_WallTop, x, y));
                return;
            }

            // ══════════════════════════════════════════════════════════════════
            // ТИП 2 — СТРОКА 1: верхний ряд изнутри (пол на y+1)
            //
            //   тайл1.1 = T2_CornerTopLeft   — нет стены слева  (!wW && wE)
            //   тайл1.2 = T2_WallLeft_Top    — стена слева, но пол слева (fW)
            //   тайл1.3 = WallFace_Top       — середина верхней стены
            //   тайл1.4 = T2_WallRight_Top   — стена справа, но пол справа (fE)
            //   тайл1.5 = T2_CornerTopRight  — нет стены справа (wW && !wE)
            // ══════════════════════════════════════════════════════════════════
            if (fN && !fS)
            {
                // тайл1.1 — верхний левый угол изнутри
                if (!wW && wE)
                {
                    wall.SetTile(pos, ts.t2_CornerTopLeft);
                    return;
                }
                // тайл1.5 — верхний правый угол изнутри (idx 7)
                if (wW && !wE)
                {
                    wall.SetTile(pos, ts.t2_CornerTopRight);
                    return;
                }
                // тайл1.2 — верх левой боковой (слева боковая стена, примыкает к углу)
                if (wW && IsFloor(grid, x - 1, y + 1))
                {
                    wall.SetTile(pos, ts.t2_WallLeft_Top);
                    return;
                }
                // тайл1.4 — верх правой боковой (справа боковая стена, примыкает к углу)
                if (wE && IsFloor(grid, x + 1, y + 1))
                {
                    wall.SetTile(pos, ts.t2_WallRight_Top);
                    return;
                }
                // тайл1.3 — верхняя стена середина (те же тайлы что и тип1 лицевая верх)
                wall.SetTile(pos, DungeonWallTileSet.Pick(ts.t1_WallFace_Top, x, y));
                return;
            }

            // ══════════════════════════════════════════════════════════════════
            // ТИП 2 — СТРОКА 4: нижний ряд изнутри (стена на y+1, пол на y+2)
            //
            //   тайл4.1 = T2_CornerBotLeft   — нет стены слева  (!wW && wE)
            //   тайл4.2-4.4 = T2_WallBot     — середина нижней стены
            //   тайл4.5 = T2_CornerBotRight  — нет стены справа (wW && !wE)
            // ══════════════════════════════════════════════════════════════════
            if (!fN && fN2 && wN)
            {
                // тайл4.1 — нижний левый угол изнутри
                if (!wW && wE)
                {
                    wall.SetTile(pos, ts.t2_CornerBotLeft);
                    return;
                }
                // тайл4.5 — нижний правый угол изнутри
                if (wW && !wE)
                {
                    wall.SetTile(pos, ts.t2_CornerBotRight);
                    return;
                }
                // тайл4.2-4.4 — нижняя стена середина (те же тайлы что и тип1 лицевая низ)
                wall.SetTile(pos, DungeonWallTileSet.Pick(ts.t1_WallFace_Bot, x, y));
                return;
            }

            // ══════════════════════════════════════════════════════════════════
            // ТИП 2 — СТРОКИ 2-3: боковые стены изнутри
            //
            //   ВНИМАНИЕ: для тип2 направление ОБРАТНОЕ относительно тип1!
            //   пол слева  (fW) → игрок видит правую стену → t2_SideRight (44,45,46,8)
            //   пол справа (fE) → игрок видит левую стену  → t2_SideLeft  (47,48,49,9)
            //
            //   Строка 2 отличается от строки 3 тайлами тайл2.2 и тайл2.4:
            //   тайл2.2 = T2_WallLeft_Bot  (idx 11) — если снизу стена (wS) типа 4
            //   тайл2.4 = T2_WallRight_Bot (idx 13) — если снизу стена (wS) типа 4
            // ══════════════════════════════════════════════════════════════════

            // Боковая стена тип2 — пол слева (fW)
            if (fW && !fE && !fN && !fS)
            {
                // тайл2.2 — низ левой боковой (примыкает к нижней стене снизу)
                // Условие: снизу стена, и за два шага вниз пол (это строка 4 нижней стены)
                if (wS && IsFloor(grid, x, y - 2))
                {
                    wall.SetTile(pos, ts.t2_WallLeft_Bot);
                    return;
                }
                // тайл3.1 / тайл2.1 — обычная боковая правая стена
                wall.SetTile(pos, DungeonWallTileSet.Pick(ts.t2_SideRight, x, y));
                return;
            }

            // Боковая стена тип2 — пол справа (fE)
            if (fE && !fW && !fN && !fS)
            {
                // тайл2.4 — низ правой боковой (примыкает к нижней стене снизу)
                if (wS && IsFloor(grid, x, y - 2))
                {
                    wall.SetTile(pos, ts.t2_WallRight_Bot);
                    return;
                }
                // тайл3.5 / тайл2.5 — обычная боковая левая стена
                wall.SetTile(pos, DungeonWallTileSet.Pick(ts.t2_SideLeft, x, y));
                return;
            }

            // ══════════════════════════════════════════════════════════════════
            // КОРИДОРЫ — боковые стены (пол с одной стороны, есть пол сверху или снизу)
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
