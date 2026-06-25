using System.Collections.Generic;
using UnityEngine;

namespace Game.Dungeon
{
    /// <summary>
    /// A* по <see cref="NavGrid"/> (4-направленный). Сохранён алгоритм из старой
    /// системы, но источник проходимости теперь — реальная геометрия комнат
    /// (стены/препятствия/двери), а не вырезанные процедурные клетки.
    /// Возвращает список клеток от from (не включая) до to (включая) или пустой.
    /// </summary>
    public static class GridPathfinder
    {
        public static List<Vector2Int> FindPath(NavGrid grid, Vector2Int from, Vector2Int to)
        {
            var result = new List<Vector2Int>();
            if (grid == null || !grid.IsWalkable(to)) return result;

            var openSet  = new SortedList<float, Node>(new DuplicateKeyComparer());
            var allNodes = new Dictionary<Vector2Int, Node>();

            var startNode = new Node(from, null, 0f, Heuristic(from, to));
            openSet.Add(startNode.F, startNode);
            allNodes[from] = startNode;

            while (openSet.Count > 0)
            {
                var current = openSet.Values[0];
                openSet.RemoveAt(0);

                if (current.Pos == to) return ReconstructPath(current);

                foreach (var dir in Directions)
                {
                    var next = current.Pos + dir;
                    if (!grid.IsWalkable(next)) continue;

                    float g = current.G + 1f;
                    if (allNodes.TryGetValue(next, out var existing))
                    {
                        if (g >= existing.G) continue;
                        existing.G = g;
                        existing.Parent = current;
                    }
                    else
                    {
                        var node = new Node(next, current, g, Heuristic(next, to));
                        openSet.Add(node.F, node);
                        allNodes[next] = node;
                    }
                }
            }
            return result;
        }

        static float Heuristic(Vector2Int a, Vector2Int b)
            => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        static List<Vector2Int> ReconstructPath(Node end)
        {
            var path = new List<Vector2Int>();
            var node = end;
            while (node.Parent != null) { path.Add(node.Pos); node = node.Parent; }
            path.Reverse();
            return path;
        }

        static readonly Vector2Int[] Directions =
            { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        class Node
        {
            public Vector2Int Pos;
            public Node Parent;
            public float G, H;
            public float F => G + H;
            public Node(Vector2Int pos, Node parent, float g, float h)
            { Pos = pos; Parent = parent; G = g; H = h; }
        }

        class DuplicateKeyComparer : IComparer<float>
        {
            public int Compare(float x, float y)
            { int r = x.CompareTo(y); return r == 0 ? 1 : r; }
        }
    }
}
