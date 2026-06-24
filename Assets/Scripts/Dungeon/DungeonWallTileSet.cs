using UnityEngine;
using UnityEngine.Tilemaps;

namespace Dungeon
{
    /// <summary>
    /// ScriptableObject — хранит все тайлы стен подземелья.
    /// Создать: ПКМ в Project → Create → Dungeon → Wall Tile Set
    ///
    /// ═══ СТРУКТУРА ТИП 1 (снаружи) ═══
    ///
    ///  тайл1.1 [CornerTopLeft]     тайл1.2 [WallTop...]       тайл1.3 [CornerTopRight]
    ///  тайл2.1 [WallLeft]          тайл2.2 [пустой]           тайл2.3 [WallRight]
    ///  тайл3.1 [FaceLeft_Top]      тайл3.2 [WallFace_Top...]  тайл3.3 [FaceRight_Top]
    ///  тайл4.1 [FaceLeft_Bot]      тайл4.2 [WallFace_Bot...]  тайл4.3 [FaceRight_Bot]
    ///  (пол рисуется поверх тайлов 1.x и 2.x на другом слое)
    ///
    /// ═══ СТРУКТУРА ТИП 2 (изнутри) ═══
    ///
    ///  тайл1.1 [T2_CornerTopLeft]  тайл1.2 [T2_WallLeft_Top]  тайл1.3 [WallFace_Top...]  тайл1.4 [T2_WallRight_Top]  тайл1.5 [T2_CornerTopRight]
    ///  тайл2.1 [T2_SideR...]       тайл2.2 [T2_WallLeft_Bot]  тайл2.3 [WallFace_Bot...]  тайл2.4 [T2_WallRight_Bot]  тайл2.5 [T2_SideL...]
    ///  тайл3.1 [T2_SideR...]       тайл3.2 [пустой]           тайл3.3 [пустой]           тайл3.4 [пустой]            тайл3.5 [T2_SideL...]
    ///  тайл4.1 [T2_CornerBotLeft]  тайл4.2-4.4 [T2_WallBot...]                                                        тайл4.5 [T2_CornerBotRight]
    ///
    /// Боковые стены: для тип1 левые=44,45,46,8 правые=47,48,49,9
    ///                для тип2 правые=44,45,46,8 левые=47,48,49,9  (НАОБОРОТ!)
    /// </summary>
    [CreateAssetMenu(menuName = "Dungeon/Wall Tile Set", fileName = "DungeonWallTileSet")]
    public class DungeonWallTileSet : ScriptableObject
    {
        // ═══════════════════════════════════════════════════════════════
        // ТИП 1 — строка 1: верх коробки (ledgeTilemap)
        // ═══════════════════════════════════════════════════════════════
        [Header("═══ ТИП 1 — строка 1: верх коробки ═══")]
        public TileBase t1_CornerTopLeft;       // тайл1.1  idx 0
        public TileBase[] t1_WallTop;           // тайл1.2  idx 19,20,21,50,51,52
        public TileBase t1_CornerTopRight;      // тайл1.3  idx 2

        // ═══════════════════════════════════════════════════════════════
        // ТИП 1 — строка 2: боковые стены коробки (ledgeTilemap)
        // ═══════════════════════════════════════════════════════════════
        [Header("═══ ТИП 1 — строка 2: боковые стены коробки ═══")]
        public TileBase[] t1_WallLeft;          // тайл2.1  idx 44,45,46,8
        public TileBase[] t1_WallRight;         // тайл2.3  idx 47,48,49,9

        // ═══════════════════════════════════════════════════════════════
        // ТИП 1 — строка 3: лицевая стена верх (wallTilemap)
        // ═══════════════════════════════════════════════════════════════
        [Header("═══ ТИП 1 — строка 3: лицевая стена верх ═══")]
        public TileBase t1_FaceLeft_Top;        // тайл3.1  idx 15
        public TileBase t1_FaceLeft_Top2;       // тайл3.1  idx 57 (доп. вариант)
        public TileBase[] t1_WallFace_Top;      // тайл3.2  idx 16,5,28,29,30,31,36,38,40,42
        public TileBase t1_FaceRight_Top;       // тайл3.3  idx 17
        public TileBase t1_FaceRight_Top2;      // тайл3.3  idx 58 (доп. вариант)

        // ═══════════════════════════════════════════════════════════════
        // ТИП 1 — строка 4: лицевая стена низ (wallTilemap)
        // ═══════════════════════════════════════════════════════════════
        [Header("═══ ТИП 1 — строка 4: лицевая стена низ ═══")]
        public TileBase t1_FaceLeft_Bot;        // тайл4.1  idx 23
        public TileBase t1_FaceLeft_Bot2;       // тайл4.1  idx 59 (доп. вариант)
        public TileBase[] t1_WallFace_Bot;      // тайл4.2  idx 24,12,32,33,34,35,37,39,41,43
        public TileBase t1_FaceRight_Bot;       // тайл4.3  idx 25
        public TileBase t1_FaceRight_Bot2;      // тайл4.3  idx 60 (доп. вариант)

        // ═══════════════════════════════════════════════════════════════
        // ТИП 1 — доп. тайлы для угла (нижняя часть верхних угловых)
        // ═══════════════════════════════════════════════════════════════
        [Header("═══ ТИП 1 — нижние части верхних угловых тайлов ═══")]
        public TileBase t1_CornerTopLeft_Bot;   // idx 53
        public TileBase t1_CornerTopRight_Bot;  // idx 54

        // ═══════════════════════════════════════════════════════════════
        // ТИП 2 — строка 1: верхний ряд изнутри (wallTilemap)
        //
        //   тайл1.1 = T2_CornerTopLeft   (idx 3)
        //   тайл1.2 = T2_WallLeft_Top    (idx 4)  — верх левой боковой, примыкает к углу
        //   тайл1.3 = WallFace_Top       (общие с тип1, idx 16,5,28...)
        //   тайл1.4 = T2_WallRight_Top   (idx 6)  — верх правой боковой, примыкает к углу
        //   тайл1.5 = T2_CornerTopRight  (idx 7)
        // ═══════════════════════════════════════════════════════════════
        [Header("═══ ТИП 2 — строка 1: верхний ряд изнутри ═══")]
        public TileBase t2_CornerTopLeft;       // тайл1.1  idx 3
        public TileBase t2_WallLeft_Top;        // тайл1.2  idx 4
        // тайл1.3 использует t1_WallFace_Top (общий массив)
        public TileBase t2_WallRight_Top;       // тайл1.4  idx 6
        public TileBase t2_CornerTopRight;      // тайл1.5  idx 7

        // ═══════════════════════════════════════════════════════════════
        // ТИП 2 — строка 2: средний ряд с угловыми боковых стен (wallTilemap)
        //
        //   тайл2.1 = T2_SideRight  (idx 44,45,46,8) — боковая правая для тип2
        //   тайл2.2 = T2_WallLeft_Bot  (idx 11) — низ левой боковой, примыкает к боковой стене
        //   тайл2.3 = WallFace_Bot  (общие с тип1, idx 24,12,32...)
        //   тайл2.4 = T2_WallRight_Bot (idx 13) — низ правой боковой, примыкает к боковой стене
        //   тайл2.5 = T2_SideLeft   (idx 47,48,49,9) — боковая левая для тип2
        // ═══════════════════════════════════════════════════════════════
        [Header("═══ ТИП 2 — строка 2: переходный ряд боковых стен ═══")]
        // тайл2.1 использует t2_SideRight
        public TileBase t2_WallLeft_Bot;        // тайл2.2  idx 11
        // тайл2.3 использует t1_WallFace_Bot (общий массив)
        public TileBase t2_WallRight_Bot;       // тайл2.4  idx 13
        // тайл2.5 использует t2_SideLeft

        // ═══════════════════════════════════════════════════════════════
        // ТИП 2 — строки 2-3: боковые стены (wallTilemap)
        //   ВНИМАНИЕ: для тип2 это НАОБОРОТ относительно тип1:
        //   т2_SideRight (idx 44,45,46,8) — стена СПРАВА от пола (пол слева)
        //   т2_SideLeft  (idx 47,48,49,9) — стена СЛЕВА от пола  (пол справа)
        // ═══════════════════════════════════════════════════════════════
        [Header("═══ ТИП 2 — боковые стены (строки 2-3) ═══")]
        public TileBase[] t2_SideRight;         // тайл2.1/3.1  idx 44,45,46,8
        public TileBase[] t2_SideLeft;          // тайл2.5/3.5  idx 47,48,49,9

        // ═══════════════════════════════════════════════════════════════
        // ТИП 2 — строка 4: нижний ряд изнутри (wallTilemap)
        //
        //   тайл4.1 = T2_CornerBotLeft   (idx 18)
        //   тайл4.2-4.4 = T2_WallBot     (idx 1,19,20,21,50,51,52)
        //   тайл4.5 = T2_CornerBotRight  (idx 22)
        // ═══════════════════════════════════════════════════════════════
        [Header("═══ ТИП 2 — строка 4: нижний ряд изнутри ═══")]
        public TileBase t2_CornerBotLeft;       // тайл4.1  idx 18
        public TileBase[] t2_WallBot;           // тайл4.2-4.4  idx 1,19,20,21,50,51,52
        public TileBase t2_CornerBotRight;      // тайл4.5  idx 22

        // ═══════════════════════════════════════════════════════════════
        // КОРИДОРЫ — боковые стены общие
        //   sideWall_L (idx 44,45,46,8) — стена слева от пола  (для тип1 левые)
        //   sideWall_R (idx 47,48,49,9) — стена справа от пола (для тип1 правые)
        // ═══════════════════════════════════════════════════════════════
        [Header("═══ КОРИДОРЫ — боковые стены ═══")]
        public TileBase[] sideWall_L;           // idx 44,45,46,8
        public TileBase[] sideWall_R;           // idx 47,48,49,9

        // ── Утилиты ──────────────────────────────────────────────────────────

        /// <summary>Детерминированный выбор тайла из массива по позиции.</summary>
        public static TileBase Pick(TileBase[] arr, int x, int y)
        {
            if (arr == null || arr.Length == 0) return null;
            if (arr.Length == 1) return arr[0];
            int hash = x * 73856093 ^ y * 19349663;
            return arr[Mathf.Abs(hash) % arr.Length];
        }
    }
}