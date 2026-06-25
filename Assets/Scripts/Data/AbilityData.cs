using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// Уникальная способность/пассивка класса (ТЗ §3 — CharacterData.passiveAbility).
    /// effectId разбирается в AbilityHandler (например: "rogue_dodge", "mage_manaburst").
    /// </summary>
    [CreateAssetMenu(menuName = "TEW/Ability", fileName = "Ability_")]
    public class AbilityData : ScriptableObject
    {
        public string abilityName;
        [TextArea] public string description;
        public Sprite icon;

        [Tooltip("ID эффекта, разбирается в AbilityHandler")]
        public string effectId;

        public float cooldown = 0f;
        public float manaCost = 0f;
    }
}
