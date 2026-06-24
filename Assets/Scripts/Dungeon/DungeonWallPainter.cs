using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Dungeon
{
    /// <summary>
    /// Тип 2 (top-down, вид изнутри).
    ///
    /// Для комнаты floor x=fx..fx+fw-1, y=fy..fy+fh-1:
    ///
    ///  y=fy+fh  (WALL): [CornerTL][WL_Top][Face_Top...][WR_Top][CornerTR]
    ///                    fx-1       fx      fx+1..fw-2   fx+fw-1  fx+fw
    ///
    ///  y=fy+fh-1 (FLOOR строка, тайлы 2.x рисуются поверх пола на wallTilemap):
    ///            (WALL): [SideL ]                                [SideR ]
    ///            (FLOOR): [WL_Bot][Face_Bot...              ][WR_Bot]
    ///
    ///  y=fy..fy+fh-2 (WALL): [SideL]                          [SideR]
    ///
    ///  y=fy-1   (WALL): [CornerBL][WallBot...            ][CornerBR]
    ///                    fx-1       fx..fx+fw-1              fx+fw
    /// </summary>
    public static class DungeonWallPainter
    {
        public static void Paint(
            DungeonGrid                   grid,
            DungeonWallTileSet            ts,
            Tilemap                       wallTilemap,
            Tilemap                       ledgeTilemap,
            IReadOnlyList<RoomInfo>       rooms        = null,
            Tilemap                       floorTilemap = null,
            System.Func<int,int,TileBase> pickFloor    = null)
        {
            var painted = new HashSet<Vector2Int>();

            if (rooms != null)
                foreach (var room in rooms)
                    PaintRoom(ts, wallTilemap, room.Area, painted);

            // Коридоры — оставшиеся wall-клетки
            for (int x = 0; x < grid.Width; x++)
            for (int y = 0; y < grid.Height; y++)
            {
                if (grid[x, y] != TileType.Wall) continue;
                if (painted.Contains(new Vector2Int(x, y))) continue;
                PaintCorridorWall(grid, ts, wallTilemap, x, y);
            }
        }

        // ── Рамка одной комнаты ────────────────────────────────────────────────

        static void PaintRoom(DungeonWallTileSet ts, Tilemap wall,
            RectInt area, HashSet<Vector2Int> painted)
        {
            int fx = area.x;
            int fy = area.y;
            int fw = area.width;
            int fh = area.height;

            // ── Row 1 (y = fy+fh): верхний ряд стены — WALL клетки ────────────
            //
            //  тайл1.1  тайл1.2   тайл1.3 ...   тайл1.4  тайл1.5
            //  CornerTL WL_Top    Face_Top        WR_Top   CornerTR
            //  x=fx-1   x=fx      x=fx+1..fw-2   x=fw-1   x=fw

            int ty = fy + fh;
            Claim(wall, painted, fx - 1,      ty, ts.t2_CornerTopLeft);
            Claim(wall, painted, fx,           ty, ts.t2_WallLeft_Top);
            for (int x = fx + 1; x <= fx + fw - 2; x++)
                Claim(wall, painted, x,        ty, Pick(ts.t1_WallFace_Top, x, ty));
            Claim(wall, painted, fx + fw - 1,  ty, ts.t2_WallRight_Top);
            Claim(wall, painted, fx + fw,      ty, ts.t2_CornerTopRight);

            // ── Row 2 (y = fy+fh-1): верхняя строка ПОЛА ─────────────────────
            //
            //  WALL-клетки по бокам:   тайл2.1 (SideL)      тайл2.5 (SideR)
            //  FLOOR-клетки в центре:  тайл2.2  тайл2.3...  тайл2.4
            //                          WL_Bot   Face_Bot     WR_Bot
            //
            // Тайлы 2.2-2.4 рисуются поверх пола на wallTilemap — это нормально.

            int r2y = fy + fh - 1;
            Claim(wall, painted, fx - 1,     r2y, Pick(ts.t2_SideLeft,  fx - 1, r2y));
            Set  (wall,          fx,          r2y, ts.t2_WallLeft_Bot);        // floor
            for (int x = fx + 1; x <= fx + fw - 2; x++)
                Set(wall,        x,           r2y, Pick(ts.t1_WallFace_Bot, x, r2y)); // floor
            Set  (wall,          fx + fw - 1, r2y, ts.t2_WallRight_Bot);      // floor
            Claim(wall, painted, fx + fw,    r2y, Pick(ts.t2_SideRight, fx + fw, r2y));

            // ── Rows 3+ (y = fy .. fy+fh-2): боковые стены — WALL клетки ─────
            //
            //  тайл3.1 SideL     [пол пол пол]     тайл3.5 SideR

            for (int y = fy; y <= fy + fh - 2; y++)
            {
                Claim(wall, painted, fx - 1,  y, Pick(ts.t2_SideLeft,  fx - 1, y));
                Claim(wall, painted, fx + fw,  y, Pick(ts.t2_SideRight, fx + fw, y));
            }

            // ── Row 4 (y = fy-1): нижний ряд стены — WALL клетки ─────────────
            //
            //  тайл4.1     тайл4.2-4.4           тайл4.5
            //  CornerBL    WallBot...             CornerBR
            //  x=fx-1      x=fx..fx+fw-1          x=fx+fw

            int by = fy - 1;
            Claim(wall, painted, fx - 1,     by, ts.t2_CornerBotLeft);
            for (int x = fx; x <= fx + fw - 1; x++)
                Claim(wall, painted, x,      by, Pick(ts.t2_WallBot, x, by));
            Claim(wall, painted, fx + fw,    by, ts.t2_CornerBotRight);
        }

        // ── Коридорные wall-клетки ─────────────────────────────────────────────

        static void PaintCorridorWall(DungeonGrid grid, DungeonWallTileSet ts,
            Tilemap wall, int x, int y)
        {
            bool fN = IsFloor(grid, x, y + 1);
            bool fS = IsFloor(grid, x, y - 1);
            bool fE = IsFloor(grid, x + 1, y);
            bool fW = IsFloor(grid, x - 1, y);

            TileBase tile = null;

            if      (fS && !fN) tile = Pick(ts.t1_WallFace_Top, x, y);
            else if (fN && !fS) tile = Pick(ts.t2_WallBot,      x, y);
            else if (fE && !fW) tile = Pick(ts.t2_SideLeft,     x, y);
            else if (fW && !fE) tile = Pick(ts.t2_SideRight,    x, y);
            else if (fS || fN)  tile = Pick(ts.t1_WallFace_Top, x, y);
            else if (fE || fW)  tile = Pick(ts.t2_SideLeft,     x, y);

            if (tile != null)
                wall.SetTile(new Vector3Int(x, y, 0), tile);
        }

        // ── Утилиты ────────────────────────────────────────────────────────────

        /// Рисует тайл и помечает клетку как нарисованную (для WALL-клеток).
        static void Claim(Tilemap map, HashSet<Vector2Int> painted, int x, int y, TileBase tile)
        {
            Set(map, x, y, tile);
            painted.Add(new Vector2Int(x, y));
        }

        static void Set(Tilemap map, int x, int y, TileBase tile)
        {
            if (tile != null)
                map.SetTile(new Vector3Int(x, y, 0), tile);
        }

        static TileBase Pick(TileBase[] arr, int x, int y)
            => DungeonWallTileSet.Pick(arr, x, y);

        static bool IsFloor(DungeonGrid grid, int x, int y)
            => grid.IsInBounds(x, y) && grid[x, y] == TileType.Floor;
    }
}
