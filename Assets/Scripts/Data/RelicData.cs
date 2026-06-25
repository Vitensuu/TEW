using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// Пассивная реликвия (ТЗ §3 — RelicData). statModifiers применяются к StatBlock,
    /// onPickupEffect — имя метода в RelicEffectHandler для спец-эффектов
    /// (рефлексия/switch при подборе).
    /// </summary>
    [CreateAssetMenu(menuName = "TEW/Items/Relic", fileName = "Relic_")]
    public class RelicData : ItemData
    {
        [Header("Реликвия")]
        [TextArea] public string flavorText;
        public List<StatModifier> statModifiers = new List<StatModifier>();

        [Tooltip("Имя метода в RelicEffectHandler (срабатывает при подборе)")]
        public string onPickupEffect;

        public Rarity rarity = Rarity.Common;
        public override Rarity Rarity => rarity;
    }
}
