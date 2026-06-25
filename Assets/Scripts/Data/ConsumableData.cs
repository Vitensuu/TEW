using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// Расходник (ТЗ §3 — ConsumableData): Health/Mana Potion, Bomb, Scroll.
    /// </summary>
    [CreateAssetMenu(menuName = "TEW/Items/Consumable", fileName = "Consumable_")]
    public class ConsumableData : ItemData
    {
        [Header("Расходник")]
        public EffectType effectType = EffectType.Heal;
        public float effectValue = 25f;
        public bool  stackable = true;
        public int   maxStack = 9;

        public Rarity rarity = Rarity.Common;
        public override Rarity Rarity => rarity;
    }
}
