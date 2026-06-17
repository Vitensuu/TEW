using UnityEngine;

namespace Dungeon
{
    /// <summary>
    /// Узел дерева BSP (Binary Space Partitioning).
    /// Хранит прямоугольную область в тайлах и ссылки на двух потомков.
    /// Если потомков нет — это лист, в котором позже будет создана комната.
    /// </summary>
    public class BSPNode
    {
        public RectInt Bounds;
        public BSPNode Left;
        public BSPNode Right;

        public BSPNode(RectInt bounds)
        {
            Bounds = bounds;
        }

        public bool IsLeaf => Left == null && Right == null;

        /// <summary>
        /// Рекурсивно делит узел пополам (по горизонтали или по вертикали),
        /// пока получившиеся области не станут меньше minLeafSize.
        /// </summary>
        public void Split(int minLeafSize, System.Random rng)
        {
            if (!TrySplit(Bounds, minLeafSize, rng, out RectInt a, out RectInt b))
                return; // дальше не делится — это лист дерева

            Left = new BSPNode(a);
            Right = new BSPNode(b);

            Left.Split(minLeafSize, rng);
            Right.Split(minLeafSize, rng);
        }

        private static bool TrySplit(RectInt bounds, int minLeafSize, System.Random rng, out RectInt a, out RectInt b)
        {
            bool canSplitHorizontally = bounds.height >= minLeafSize * 2; // делим по Y (верх/низ)
            bool canSplitVertically = bounds.width >= minLeafSize * 2;    // делим по X (лево/право)

            if (!canSplitHorizontally && !canSplitVertically)
            {
                a = b = default;
                return false;
            }

            bool splitHorizontally;
            if (canSplitHorizontally && canSplitVertically)
            {
                // Режем длинную сторону, чтобы листья не получались сильно вытянутыми
                if (bounds.width > bounds.height * 1.25f) splitHorizontally = false;
                else if (bounds.height > bounds.width * 1.25f) splitHorizontally = true;
                else splitHorizontally = rng.NextDouble() < 0.5;
            }
            else
            {
                splitHorizontally = canSplitHorizontally;
            }

            if (splitHorizontally)
            {
                int splitY = rng.Next(minLeafSize, bounds.height - minLeafSize + 1);
                a = new RectInt(bounds.x, bounds.y, bounds.width, splitY);
                b = new RectInt(bounds.x, bounds.y + splitY, bounds.width, bounds.height - splitY);
            }
            else
            {
                int splitX = rng.Next(minLeafSize, bounds.width - minLeafSize + 1);
                a = new RectInt(bounds.x, bounds.y, splitX, bounds.height);
                b = new RectInt(bounds.x + splitX, bounds.y, bounds.width - splitX, bounds.height);
            }
            return true;
        }
    }
}
