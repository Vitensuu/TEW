using System.Collections.Generic;

namespace Game.Dungeon
{
    /// <summary>
    /// Узел графа этажа (ТЗ ЭТАП 6 — Room Graph для миникарты/навигации/соседей).
    /// Один узел = одна собранная комната на сцене.
    /// </summary>
    public class RoomNode
    {
        public int Index;
        public RoomType Type;
        public RoomInstance Instance;       // null до инстанса (на этапе абстрактного графа)
        public int Depth;                   // расстояние от Start (для размещения босса/выхода)

        // Соседи по направлениям (двусторонние связи).
        public readonly Dictionary<Direction, RoomNode> Neighbors = new Dictionary<Direction, RoomNode>();

        // Состояние для геймплея/миникарты.
        public bool Visited;     // игрок заходил
        public bool Cleared;     // комната зачищена

        public RoomNode(int index, RoomType type) { Index = index; Type = type; }

        public IEnumerable<RoomNode> AllNeighbors => Neighbors.Values;
    }
}
