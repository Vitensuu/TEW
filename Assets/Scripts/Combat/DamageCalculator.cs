using UnityEngine;
using Game.Core;
using Game.Data;

namespace Game.Combat
{
    /// <summary>Результат расчёта урона.</summary>
    public struct DamageResult
    {
        public float amount;
        public bool  isCrit;
        public DamageType type;
    }

    /// <summary>
    /// Расчёт урона (ТЗ §4 «Система боя», §5 — DamageCalculator).
    /// finalDamage = (attackPower - defense) * critMultiplier * elementalMod.
    /// Криты: шанс = baseCritChance + relicBonus, множитель x1.5 — x3.0.
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>
        /// attacker — статы атакующего (attack, critChance, critMultiplier).
        /// weaponDamage — урон оружия (прибавляется к attack).
        /// targetDefense — защита цели. type — стихия (для elementalMod / резистов).
        /// </summary>
        public static DamageResult Compute(StatBlock attacker, float weaponDamage,
            float targetDefense, DamageType type, float elementalMod = 1f,
            System.Random rng = null)
        {
            float attackPower = (attacker?.attack ?? 0f) + weaponDamage;

            // True игнорирует защиту.
            float mitigated = type == DamageType.True
                ? attackPower
                : Mathf.Max(1f, attackPower - targetDefense);

            float critChance = attacker?.critChance ?? 0f;
            float critMult    = Mathf.Clamp(attacker?.critMultiplier ?? 1.5f, 1.5f, 3.0f);

            double roll = rng != null ? rng.NextDouble() : Random.value;
            bool isCrit = roll < critChance;

            float final = mitigated * (isCrit ? critMult : 1f) * elementalMod;

            return new DamageResult { amount = final, isCrit = isCrit, type = type };
        }
    }
}
