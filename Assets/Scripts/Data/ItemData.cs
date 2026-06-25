using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// Базовый класс всех предметов-ассетов (ТЗ §3 — модуль Items).
    /// Общая поверхность для инвентаря, лута и UI. Наследники:
    /// WeaponData, RelicData, ConsumableData.
    /// </summary>
    public abstract class ItemData : ScriptableObject
    {
        [Header("База предмета")]
        [SerializeField] string id;                 // стабильный ID для save/ItemDatabase
        public string itemName;
        [TextArea] public string description;
        public Sprite sprite;

        /// <summary>Стабильный ID. Если не задан в инспекторе — имя ассета.</summary>
        public string Id => string.IsNullOrEmpty(id) ? name : id;

        public abstract Rarity Rarity { get; }
    }
}
