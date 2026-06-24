using UnityEngine;
using UnityEngine.Tilemaps;

namespace Dungeon.Procedural
{
    /// <summary>
    /// Роль клетки стены, вычисляется по соседям-полу (4 ребра + 4 внешних
    /// угла + 4 внутренних угла). Этого набора достаточно, чтобы собрать
    /// «коробочную» комнату из спрайтовых стен без швов.
    /// </summary>
    public enum WallRole
    {
        TopEdge, BottomEdge, LeftEdge, RightEdge,
        TopLeft, TopRight, BottomLeft, BottomRight,         // внешние углы
        InnerTopLeft, InnerTopRight, InnerBottomLeft, InnerBottomRight // внутренние углы
    }

    /// <summary>
    /// Набор тайлов подземелья (ScriptableObject — создаётся через
    /// Assets ▸ Create ▸ Dungeon ▸ Tile Set). Один ассет на тему/этаж.
    ///
    /// ДВА РЕЖИМА (можно комбинировать):
    ///   1) Простой: назначь <see cref="wallRuleTile"/> (Unity RuleTile) — он сам
    ///      выберет нужный спрайт по соседям. Самый надёжный путь.
    ///   2) Явный: заполни массивы ролей ниже своими Wall_* тайлами. Painter
    ///      выберет роль по соседям и возьмёт случайный тайл из массива роли
    ///      (вариативность). Если массив роли пуст — откат на wallRuleTile,
    ///      затем на fallbackWall.
    ///
    /// Соответствие спецификации (ТИП 1 — внешняя коробка с лицевой частью):
    ///   TopLeft        ← Wall_0  Wall_53
    ///   TopEdge        ← Wall_19 Wall_20 Wall_21 Wall_50 Wall_51 Wall_52
    ///   TopRight       ← Wall_2  Wall_54
    ///   LeftEdge       ← Wall_8  Wall_44 Wall_45 Wall_46
    ///   RightEdge      ← Wall_9  Wall_47 Wall_48 Wall_49
    ///   BottomLeft     ← Wall_23 Wall_59 (левая лицевая нижняя)
    ///   BottomRight    ← Wall_25 Wall_60 (правая лицевая нижняя)
    ///   BottomEdge     ← Wall_24 Wall_32 Wall_33 Wall_34 Wall_35 … (нижняя лицевая)
    ///   InnerTopLeft   ← Wall_3   InnerTopRight  ← Wall_7
    ///   InnerBottomLeft← Wall_18  InnerBottomRight← Wall_22 (ТИП 2 — внутренние углы)
    /// Лицевые верхние (Wall_15/57, Wall_16/5…) — это TopEdge, если стена двойной
    /// высоты; при использовании RuleTile он закроет их автоматически.
    /// </summary>
    [CreateAssetMenu(menuName = "Dungeon/Tile Set", fileName = "DungeonTileSet")]
    public class DungeonTileSet : ScriptableObject
    {
        [Header("Пол (случайный выбор для разнообразия)")]
        public TileBase[] floorTiles;

        [Header("Простой режим — один RuleTile на все стены")]
        [Tooltip("Если задан и роль-массив пуст, ставится этот тайл (обычно RuleTile)")]
        public TileBase wallRuleTile;

        [Tooltip("Аварийный одиночный тайл стены, если ничего не назначено")]
        public TileBase fallbackWall;

        [Header("Явный режим — тайлы по ролям (см. комментарий класса)")]
        public TileBase[] topEdge;
        public TileBase[] bottomEdge;
        public TileBase[] leftEdge;
        public TileBase[] rightEdge;
        public TileBase[] topLeft;
        public TileBase[] topRight;
        public TileBase[] bottomLeft;
        public TileBase[] bottomRight;
        public TileBase[] innerTopLeft;
        public TileBase[] innerTopRight;
        public TileBase[] innerBottomLeft;
        public TileBase[] innerBottomRight;

        /// <summary>Случайный тайл пола (или null, если массив пуст).</summary>
        public TileBase PickFloor(System.Random rng) => PickRandom(floorTiles, rng);

        /// <summary>
        /// Тайл стены для роли. Приоритет: явный массив роли → wallRuleTile →
        /// fallbackWall. Возврат null означает «не рисовать» (painter пропустит).
        /// </summary>
        public TileBase PickWall(WallRole role, System.Random rng)
        {
            TileBase[] arr = role switch
            {
                WallRole.TopEdge          => topEdge,
                WallRole.BottomEdge       => bottomEdge,
                WallRole.LeftEdge         => leftEdge,
                WallRole.RightEdge        => rightEdge,
                WallRole.TopLeft          => topLeft,
                WallRole.TopRight         => topRight,
                WallRole.BottomLeft       => bottomLeft,
                WallRole.BottomRight      => bottomRight,
                WallRole.InnerTopLeft     => innerTopLeft,
                WallRole.InnerTopRight    => innerTopRight,
                WallRole.InnerBottomLeft  => innerBottomLeft,
                WallRole.InnerBottomRight => innerBottomRight,
                _                         => null
            };

            TileBase t = PickRandom(arr, rng);
            if (t != null) return t;
            if (wallRuleTile != null) return wallRuleTile;
            return fallbackWall;
        }

        static TileBase PickRandom(TileBase[] arr, System.Random rng)
        {
            if (arr == null || arr.Length == 0) return null;
            return arr[rng.Next(arr.Length)];
        }
    }
}
