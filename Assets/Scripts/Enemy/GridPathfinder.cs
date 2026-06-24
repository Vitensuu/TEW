using System.Collections.Generic;
using UnityEngine;
using Dungeon;

namespace Enemy
{
    /// <summary>
    /// A* pathfinding по DungeonGrid (4-направленный, без диагоналей).
    /// Использование:
    ///   var path = GridPathfinder.FindPath(grid, from, to);
    /// Возвращает список клеток от from (не включая) до to (включая),
    /// или пустой список если путь не найден.
    /// </summary>
    public static class GridPathfinder
    {
        public static List<Vector2Int> FindPath(DungeonGrid grid, Vector2Int from, Vector2Int to)
        {
            if (!IsWalkable(grid, to)) return new List<Vector2Int>();

            var openSet  = new SortedList<float, Node>(new DuplicateKeyComparer());
            var allNodes = new Dictionary<Vector2Int, Node>();

            var startNode = new Node(from, null, 0f, Heuristic(from, to));
            openSet.Add(startNode.F, startNode);
            allNodes[from] = startNode;

            while (openSet.Count > 0)
            {
                var current = openSet.Values[0];
                openSet.RemoveAt(0);

                if (current.Pos == to)
                    return ReconstructPath(current);

                foreach (var dir in Directions)
                {
                    var next = current.Pos + dir;
                    if (!IsWalkable(grid, next)) continue;

                    float g = current.G + 1f;

                    if (allNodes.TryGetValue(next, out var existing))
                    {
                        if (g >= existing.G) continue;
                        // Обновляем существующий узел
                        existing.G      = g;
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

            return new List<Vector2Int>();
        }

        static bool IsWalkable(DungeonGrid grid, Vector2Int pos)
            => grid[pos.x, pos.y] == TileType.Floor;

        static float Heuristic(Vector2Int a, Vector2Int b)
            => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        static List<Vector2Int> ReconstructPath(Node end)
        {
            var path = new List<Vector2Int>();
            var node = end;
            while (node.Parent != null)
            {
                path.Add(node.Pos);
                node = node.Parent;
            }
            path.Reverse();
            return path;
        }

        static readonly Vector2Int[] Directions =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        class Node
        {
            public Vector2Int Pos;
            public Node       Parent;
            public float      G;
            public float      H;
            public float      F => G + H;

            public Node(Vector2Int pos, Node parent, float g, float h)
            {
                Pos    = pos;
                Parent = parent;
                G      = g;
                H      = h;
            }
        }

        // SortedList не допускает дублирующих ключей — компаратор решает это
        class DuplicateKeyComparer : IComparer<float>
        {
            public int Compare(float x, float y)
            {
                int result = x.CompareTo(y);
                return result == 0 ? 1 : result;
            }
        }
    }
}
