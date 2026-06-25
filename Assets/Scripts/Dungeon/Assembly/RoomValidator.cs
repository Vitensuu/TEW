using System.Collections.Generic;
using UnityEngine;

namespace Game.Dungeon
{
    /// <summary>
    /// Проверка собранного этажа (ТЗ — RoomValidator). Гарантирует:
    /// нет наложения комнат, есть Start, связность графа, и (если требуется)
    /// присутствуют Boss/Exit. Используется ассемблером: при провале — перегенерация.
    /// </summary>
    public static class RoomValidator
    {
        /// <summary>Наложение двух комнат (AABB с допуском на общие стены).</summary>
        public static bool Overlaps(RoomInstance a, RoomInstance b, float tolerance = 0.6f)
        {
            Bounds ba = a.WorldBounds, bb = b.WorldBounds;
            ba.Expand(-tolerance * 2f);   // сжать, чтобы соседи «стена к стене» не считались наложением
            bb.Expand(-tolerance * 2f);
            return ba.Intersects(bb);
        }

        public static bool AnyOverlap(IReadOnlyList<RoomInstance> placed, RoomInstance candidate)
        {
            foreach (var r in placed)
                if (r != candidate && Overlaps(r, candidate)) return true;
            return false;
        }

        public static bool Validate(RoomGraph graph, bool requireBoss, bool requireExit, out string error)
        {
            error = null;
            if (graph.Start == null) { error = "нет Start-комнаты"; return false; }
            if (requireBoss && graph.Boss == null) { error = "нет Boss-комнаты"; return false; }
            if (requireExit && graph.Exit == null) { error = "нет Exit-комнаты"; return false; }
            if (!graph.IsFullyConnected()) { error = "граф несвязный"; return false; }
            return true;
        }
    }
}
