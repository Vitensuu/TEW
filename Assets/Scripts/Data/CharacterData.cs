using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// Класс героя (ТЗ §3 — CharacterData): Warrior, Rogue, Mage, Ranger.
    /// Базовые статы, стартовое оружие, уникальная пассивка, условие разблокировки.
    /// </summary>
    [CreateAssetMenu(menuName = "TEW/Character", fileName = "Character_")]
    public class CharacterData : ScriptableObject
    {
        [Header("Класс")]
        public string characterName = "Warrior";
        [TextArea] public string description;

        [Header("Базовые характеристики")]
        public int baseHealth  = 100;
        public int baseMana    = 50;
        public float baseSpeed = 5f;
        public int baseAttack  = 10;
        public int baseDefense = 5;
        [Range(0f, 1f)] public float baseCritChance = 0.05f;
        public float baseCritMultiplier = 1.5f;

        [Header("Снаряжение / способности")]
        public WeaponData startingWeapon;
        public AbilityData passiveAbility;

        [Header("Визуал")]
        public GameObject spritePrefab;

        [Header("Мета-прогрессия")]
        [Tooltip("ID условия разблокировки; пусто = доступен сразу")]
        public string unlockCondition;
        public bool unlockedByDefault = true;

        /// <summary>Базовый StatBlock класса (до модификаторов предметов/мета).</summary>
        public StatBlock ToBaseStats() => new StatBlock
        {
            maxHealth      = baseHealth,
            maxMana        = baseMana,
            speed          = baseSpeed,
            attack         = baseAttack,
            defense        = baseDefense,
            critChance     = baseCritChance,
            critMultiplier = baseCritMultiplier,
        };
    }
}
