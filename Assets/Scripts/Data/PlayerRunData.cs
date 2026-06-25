using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// Состояние текущего забега (ТЗ §3 — PlayerRunData, Runtime).
    /// Создаётся при старте забега, живёт до смерти. Сериализуем для RunSave.
    /// </summary>
    [Serializable]
    public class PlayerRunData
    {
        public CharacterData character;

        public float currentHealth;
        public float currentMana;
        public int   currentFloor = 1;
        public int   gold;

        public int   enemiesKilled;
        public float runTimeSeconds;

        public List<ItemInstance> inventory    = new List<ItemInstance>();
        public List<RelicData>    activeRelics  = new List<RelicData>();

        /// <summary>Итоговые статы с учётом всех модификаторов (пересчитывает PlayerStats).</summary>
        [NonSerialized] public StatBlock stats;

        public static PlayerRunData CreateForCharacter(CharacterData c)
        {
            var run = new PlayerRunData
            {
                character     = c,
                currentFloor  = 1,
                gold          = 0,
                stats         = c != null ? c.ToBaseStats() : new StatBlock(),
            };
            run.currentHealth = run.stats.maxHealth;
            run.currentMana   = run.stats.maxMana;

            if (c != null && c.startingWeapon != null)
                run.inventory.Add(new ItemInstance(c.startingWeapon));

            return run;
        }

        /// <summary>
        /// Сколько Осколков душ заработано (ТЗ §4 — мета-валюта).
        /// Простая формула MVP: этажи + киллы + золото/10.
        /// </summary>
        public int SoulShardsEarned()
            => Mathf.RoundToInt(currentFloor * 10 + enemiesKilled + gold * 0.1f);
    }
}
