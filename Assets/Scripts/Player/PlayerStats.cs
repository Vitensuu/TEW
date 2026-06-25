using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.Data;
using Game.Save;

namespace Game.Player
{
    /// <summary>
    /// Характеристики игрока и их пересчёт (ТЗ §5 — PlayerStats, Component).
    /// Итоговый StatBlock = база класса + мета-улучшения + модификаторы реликвий.
    /// Дёргай Recompute() при изменении инвентаря/реликвий.
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        [Tooltip("Класс по умолчанию, если нет активного забега (для теста сцены)")]
        [SerializeField] CharacterData fallbackCharacter;

        public StatBlock Stats { get; private set; } = new StatBlock();

        public System.Action<StatBlock> OnStatsChanged;

        PlayerRunData Run =>
            GameManager.Instance != null ? GameManager.Instance.Run : null;

        void Awake() => Recompute();

        /// <summary>Пересчитать итоговые характеристики из всех источников.</summary>
        public void Recompute()
        {
            var run = Run;
            CharacterData character = run?.character ?? fallbackCharacter;
            StatBlock baseStats = character != null ? character.ToBaseStats() : new StatBlock
            {
                maxHealth = 100, maxMana = 50, speed = 5, attack = 10, defense = 5,
                critChance = 0.05f, critMultiplier = 1.5f
            };

            var mods = new List<StatModifier>();

            // 1. Перманентные мета-улучшения (ТЗ §4 — дерево).
            if (MetaProgression.Instance != null)
                mods.AddRange(MetaProgression.Instance.CollectPurchasedModifiers());

            // 2. Активные реликвии забега (ТЗ §3 — RelicData.statModifiers).
            if (run != null)
                foreach (var relic in run.activeRelics)
                    if (relic != null) mods.AddRange(relic.statModifiers);

            Stats = StatBlock.Compute(baseStats, mods);

            if (run != null) run.stats = Stats;

            OnStatsChanged?.Invoke(Stats);
        }

        public float Attack         => Stats.attack;
        public float Defense        => Stats.defense;
        public float Speed          => Stats.speed;
        public float MaxHealth      => Stats.maxHealth;
        public float MaxMana        => Stats.maxMana;
    }
}
