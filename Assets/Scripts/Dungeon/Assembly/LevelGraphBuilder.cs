using System.Collections.Generic;
using UnityEngine;
using Game.Data;

namespace Game.Dungeon
{
    /// <summary>
    /// Абстрактный «скелет» этажа (ТЗ ЭТАП 2 — случайный граф связей комнат).
    /// Строит дерево узлов с типами (Start→Normal/Elite/Treasure/Shop/Shrine→Boss→Exit)
    /// по FloorConfig. Конкретные префабы НЕ выбираются здесь — это делает ассемблер
    /// на ЭТАПЕ 3. Узел знает только свой тип и индекс родителя.
    /// </summary>
    public static class LevelGraphBuilder
    {
        public struct NodeSpec
        {
            public RoomType type;
            public int parentIndex;   // -1 для Start
            public NodeSpec(RoomType t, int p) { type = t; parentIndex = p; }
        }

        public static List<NodeSpec> Build(FloorConfig cfg, int floor, System.Random rng)
        {
            var specs = new List<NodeSpec> { new NodeSpec(RoomType.Start, -1) };

            int minR = cfg != null ? cfg.minRooms : 6;
            int maxR = cfg != null ? cfg.maxRooms : 10;
            int normalTarget = Mathf.Max(1, rng.Next(minR, maxR + 1));

            // Дерево обычных комнат: каждый ребёнок — у случайного, но с уклоном в глубину
            // (чтобы получались ветки, как в примере ТЗ).
            for (int i = 0; i < normalTarget; i++)
            {
                int parent = PickParent(specs, rng);
                specs.Add(new NodeSpec(RoomType.Normal, parent));
            }

            // Спец-комнаты — как листья у случайных обычных узлов.
            if (floor >= 2 && Chance(rng, 0.6f)) specs.Add(new NodeSpec(RoomType.Elite, RandomNormal(specs, rng)));
            if (cfg != null && Chance(rng, cfg.treasureChance)) specs.Add(new NodeSpec(RoomType.Treasure, RandomNormal(specs, rng)));
            if (cfg != null && Chance(rng, cfg.shopChance))     specs.Add(new NodeSpec(RoomType.Shop,     RandomNormal(specs, rng)));
            if (cfg != null && Chance(rng, cfg.shrineChance))   specs.Add(new NodeSpec(RoomType.Shrine,   RandomNormal(specs, rng)));

            // Босс на самой дальней ветке, затем выход.
            int deepest = DeepestIndex(specs);
            bool boss = cfg != null && (cfg.hasBoss || floor % 5 == 0);
            if (boss)
            {
                specs.Add(new NodeSpec(RoomType.Boss, deepest));
                deepest = specs.Count - 1;
            }
            specs.Add(new NodeSpec(RoomType.Exit, deepest));

            return specs;
        }

        static bool Chance(System.Random rng, float p) => rng.NextDouble() < p;

        static int PickParent(List<NodeSpec> specs, System.Random rng)
        {
            // 60% — последний узел (цепочка), иначе случайный (ветвление).
            if (specs.Count == 1) return 0;
            return rng.NextDouble() < 0.6 ? specs.Count - 1 : rng.Next(0, specs.Count);
        }

        static int RandomNormal(List<NodeSpec> specs, System.Random rng)
        {
            var idx = new List<int>();
            for (int i = 0; i < specs.Count; i++)
                if (specs[i].type == RoomType.Normal) idx.Add(i);
            return idx.Count > 0 ? idx[rng.Next(idx.Count)] : 0;
        }

        static int DeepestIndex(List<NodeSpec> specs)
        {
            var depth = new int[specs.Count];
            int best = 0, bestIdx = 0;
            for (int i = 0; i < specs.Count; i++)
            {
                depth[i] = specs[i].parentIndex < 0 ? 0 : depth[specs[i].parentIndex] + 1;
                if (depth[i] > best) { best = depth[i]; bestIdx = i; }
            }
            return bestIdx;
        }
    }
}
