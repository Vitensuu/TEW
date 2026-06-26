using System.Collections.Generic;
using UnityEngine;
using Game.Data;

namespace Game.Save
{
    public enum UpgradeBranch { Strength, Agility, Magic }   // Сила, Ловкость, Магия

    /// <summary>
    /// Узел дерева мета-улучшений (ТЗ §4 — дерево 3 ветки × 10 узлов).
    /// Покупается за Осколки душ, навсегда добавляет StatModifier к базе забега.
    /// </summary>
    [CreateAssetMenu(menuName = "TEW/Meta/UpgradeNode", fileName = "Upgrade_")]
    public class UpgradeNode : ScriptableObject
    {
        public string id;
        public UpgradeBranch branch = UpgradeBranch.Strength;
        [Tooltip("Позиция в ветке 0..9 (определяет порядок открытия)")]
        public int tier;
        public int cost = 50;

        public string title;
        [TextArea] public string description;
        public Sprite icon;

        [Tooltip("Модификаторы, применяемые к базовым статам забега")]
        public List<StatModifier> modifiers = new List<StatModifier>();

        [Tooltip("Узел-предшественник (нужно купить раньше). Пусто = доступен сразу.")]
        public UpgradeNode prerequisite;

        public string Id => string.IsNullOrEmpty(id) ? name : id;
    }
}
