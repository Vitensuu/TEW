using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// Один модификатор характеристики (ТЗ §3 — StatModifier).
    /// Пример: +10 Attack (Flat), +15% Speed (PercentAdd), x1.2 Crit (PercentMult).
    /// </summary>
    [Serializable]
    public struct StatModifier
    {
        public StatType    stat;
        public ModifierMode mode;
        public float       value;

        public StatModifier(StatType stat, ModifierMode mode, float value)
        {
            this.stat = stat; this.mode = mode; this.value = value;
        }
    }

    /// <summary>
    /// Итоговые характеристики персонажа с учётом всех бонусов (ТЗ §3 — StatBlock).
    /// Формула применения: final = (base + Σflat) * (1 + ΣpercentAdd) * ΠpercentMult.
    /// </summary>
    [Serializable]
    public class StatBlock
    {
        public float maxHealth;
        public float maxMana;
        public float speed;
        public float attack;
        public float defense;
        [Range(0f, 1f)] public float critChance;
        public float critMultiplier = 1.5f;

        public StatBlock Clone() => (StatBlock)MemberwiseClone();

        /// <summary>Пересчитать из базы + списка модификаторов (ТЗ §5 — PlayerStats).</summary>
        public static StatBlock Compute(StatBlock baseStats, IEnumerable<StatModifier> mods)
        {
            var flat   = new Dictionary<StatType, float>();
            var pAdd   = new Dictionary<StatType, float>();
            var pMult  = new Dictionary<StatType, float>();

            if (mods != null)
                foreach (var m in mods)
                {
                    switch (m.mode)
                    {
                        case ModifierMode.Flat:        Add(flat,  m.stat, m.value); break;
                        case ModifierMode.PercentAdd:  Add(pAdd,  m.stat, m.value); break;
                        case ModifierMode.PercentMult: Mul(pMult, m.stat, m.value); break;
                    }
                }

            return new StatBlock
            {
                maxHealth      = Final(baseStats.maxHealth,      StatType.MaxHealth,      flat, pAdd, pMult),
                maxMana        = Final(baseStats.maxMana,        StatType.MaxMana,        flat, pAdd, pMult),
                speed          = Final(baseStats.speed,          StatType.Speed,          flat, pAdd, pMult),
                attack         = Final(baseStats.attack,         StatType.Attack,         flat, pAdd, pMult),
                defense        = Final(baseStats.defense,        StatType.Defense,        flat, pAdd, pMult),
                critChance     = Mathf.Clamp01(Final(baseStats.critChance, StatType.CritChance, flat, pAdd, pMult)),
                critMultiplier = Final(baseStats.critMultiplier, StatType.CritMultiplier, flat, pAdd, pMult),
            };
        }

        static void Add(Dictionary<StatType, float> d, StatType s, float v)
            => d[s] = (d.TryGetValue(s, out var c) ? c : 0f) + v;
        static void Mul(Dictionary<StatType, float> d, StatType s, float v)
            => d[s] = (d.TryGetValue(s, out var c) ? c : 1f) * v;

        static float Final(float b, StatType s,
            Dictionary<StatType, float> flat,
            Dictionary<StatType, float> pAdd,
            Dictionary<StatType, float> pMult)
        {
            float f  = flat.TryGetValue(s, out var fv) ? fv : 0f;
            float pa = pAdd.TryGetValue(s, out var av) ? av : 0f;
            float pm = pMult.TryGetValue(s, out var mv) ? mv : 1f;
            return (b + f) * (1f + pa) * pm;
        }
    }
}
