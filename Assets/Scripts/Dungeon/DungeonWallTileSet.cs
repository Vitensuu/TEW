using UnityEngine;
using UnityEngine.Tilemaps;

namespace Dungeon
{
    /// <summary>
    /// ScriptableObject — хранит все тайлы стен подземелья.
    /// Создать: ПКМ в Project → Create → Dungeon → Wall Tile Set
    ///
    /// СТРУКТУРА КОРОБКИ (тип 1, снаружи), 4 строки тайлов:
    ///
    ///  Строка 1: [CornerTopLeft] [WallTop...] [CornerTopRight]   ← крыша / верх
    ///  Строка 2: [WallLeft]      [пусто]      [WallRight]        ← боковые стены
    ///  Строка 3: [FaceLeft_Top]  [WallUpper]  [FaceRight_Top]    ← лицевая верх
    ///  Строка 4: [FaceLeft_Bot]  [WallLower]  [FaceRight_Bot]    ← лицевая низ
    ///
    ///  Пол рисуется НА ТОМ ЖЕ МЕСТЕ что строки 1 и 2 (другой Tilemap-слой).
    /// </summary>
    [CreateAssetMenu(menuName = "Dungeon/Wall Tile Set", fileName = "DungeonWallTileSet")]
    public class DungeonWallTileSet : ScriptableObject
    {
        [Header("═══ ТИП 1 — строка 1: верх коробки ═══")]
        public TileBase t1_CornerTopLeft;       // idx 0  — верхний левый угол
        public TileBase[] t1_WallTop;           // idx 19,20,21,50,51,52 — верхняя стена
        public TileBase t1_CornerTopRight;      // idx 2  — верхний правый угол

        [Header("═══ ТИП 1 — строка 2: боковые стены коробки ═══")]
        public TileBase[] t1_WallLeft;          // idx 8,44,45,46
        public TileBase[] t1_WallRight;         // idx 9,47,48,49

        [Header("═══ ТИП 1 — строка 3: лицевая стена верх ═══")]
        public TileBase t1_FaceLeft_Top;        // idx 15 — левый угол лицевой верх
        public TileBase[] t1_WallFace_Top;      // idx 16,5,28,29,30,31,36,38,40,42
        public TileBase t1_FaceRight_Top;       // idx 17 — правый угол лицевой верх

        [Header("═══ ТИП 1 — строка 4: лицевая стена низ ═══")]
        public TileBase t1_FaceLeft_Bot;        // idx 23 — левый угол лицевой низ
        public TileBase[] t1_WallFace_Bot;      // idx 24,12,32,33,34,35,37,39,41,43
        public TileBase t1_FaceRight_Bot;       // idx 25 — правый угол лицевой низ

        [Header("═══ ТИП 1 — доп. угловые тайлы (53,54,57,58,59,60) ═══")]
        public TileBase t1_CornerTopLeft_Bot;   // idx 53 — нижняя часть верхнего левого угла
        public TileBase t1_CornerTopRight_Bot;  // idx 54 — нижняя часть верхнего правого угла
        public TileBase t1_FaceLeft_Top2;       // idx 57 — доп. левый угол верх
        public TileBase t1_FaceRight_Top2;      // idx 58 — доп. правый угол верх
        public TileBase t1_FaceLeft_Bot2;       // idx 59 — доп. левый угол низ
        public TileBase t1_FaceRight_Bot2;      // idx 60 — доп. правый угол низ

        [Header("═══ ТИП 2 — изнутри ═══")]
        public TileBase t2_CornerTopLeft;       // idx 3
        public TileBase t2_WallLeft_Top;        // idx 4
        public TileBase t2_WallLeft_Bot;        // idx 11
        public TileBase t2_WallRight_Top;       // idx 6
        public TileBase t2_WallRight_Bot;       // idx 13
        public TileBase t2_CornerBotLeft;       // idx 18
        public TileBase t2_CornerBotRight;      // idx 22
        public TileBase[] t2_WallBot;           // idx 1,19,20,21,50,51,52

        [Header("═══ БОКОВЫЕ общие ═══")]
        public TileBase[] sideWall_L;           // idx 44,45,46
        public TileBase[] sideWall_R;           // idx 47,48,49

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