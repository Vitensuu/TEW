using System.Collections.Generic;

namespace Game.Dungeon
{
    /// <summary>
    /// Граф собранного этажа (ТЗ ЭТАП 6). Хранит узлы и связи между комнатами,
    /// отдаёт стартовую/боссовую/выходную комнаты, соседей и обход BFS
    /// (для глубины, миникарты, открытия дверей).
    /// </summary>
    public class RoomGraph
    {
        public readonly List<RoomNode> Nodes = new List<RoomNode>();

        public RoomNode Start { get; private set; }
        public RoomNode Boss  { get; private set; }
        public RoomNode Exit  { get; private set; }

        public RoomNode AddNode(RoomType type)
        {
            var node = new RoomNode(Nodes.Count, type);
            Nodes.Add(node);
            if (type == RoomType.Start) Start = node;
            if (type == RoomType.Boss)  Boss  = node;
            if (type == RoomType.Exit)  Exit  = node;
            return node;
        }

        /// <summary>Двусторонняя связь a→b по направлению dir (от a к b).</summary>
        public void Connect(RoomNode a, RoomNode b, Direction dir)
        {
            a.Neighbors[dir] = b;
            b.Neighbors[dir.Opposite()] = a;
        }

        /// <summary>Пересчитать глубину BFS от Start.</summary>
        public void RecomputeDepth()
        {
            if (Start == null) return;
            foreach (var n in Nodes) n.Depth = int.MaxValue;
            var q = new Queue<RoomNode>();
            Start.Depth = 0;
            q.Enqueue(Start);
            while (q.Count > 0)
            {
                var n = q.Dequeue();
                foreach (var nb in n.AllNeighbors)
                    if (nb.Depth > n.Depth + 1)
                    {
                        nb.Depth = n.Depth + 1;
                        q.Enqueue(nb);
                    }
            }
        }

        /// <summary>Все комнаты достижимы из Start?</summary>
        public bool IsFullyConnected()
        {
            if (Start == null) return false;
            RecomputeDepth();
            foreach (var n in Nodes) if (n.Depth == int.MaxValue) return false;
            return true;
        }
    }
}
