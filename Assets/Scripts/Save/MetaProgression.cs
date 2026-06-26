using System.Collections.Generic;
using UnityEngine;
using Game.Data;

namespace Game.Save
{
    /// <summary>
    /// Реестр дерева улучшений (ТЗ §4). Держит все UpgradeNode, проверяет
    /// доступность/покупку, и собирает модификаторы купленных узлов для применения
    /// к базовому StatBlock в начале забега.
    /// Повесь на тот же объект, что SaveSystem, и назначь все ноды в инспекторе.
    /// </summary>
    public class MetaProgression : MonoBehaviour
    {
        public static MetaProgression Instance { get; private set; }

        [Tooltip("Все узлы дерева (3 ветки × 10 = 30)")]
        [SerializeField] List<UpgradeNode> allNodes = new List<UpgradeNode>();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        public IReadOnlyList<UpgradeNode> AllNodes => allNodes;

        public IEnumerable<UpgradeNode> NodesOfBranch(UpgradeBranch b)
        {
            foreach (var n in allNodes) if (n != null && n.branch == b) yield return n;
        }

        bool Purchased(UpgradeNode n)
            => SaveSystem.Instance != null && SaveSystem.Instance.Meta.HasUpgrade(n.Id);

        /// <summary>Можно ли купить: не куплен, пререквизит куплен, хватает осколков.</summary>
        public bool CanPurchase(UpgradeNode n)
        {
            if (n == null || SaveSystem.Instance == null) return false;
            if (Purchased(n)) return false;
            if (n.prerequisite != null && !Purchased(n.prerequisite)) return false;
            return SaveSystem.Instance.SoulShards >= n.cost;
        }

        public bool Purchase(UpgradeNode n)
        {
            if (!CanPurchase(n)) return false;
            if (!SaveSystem.Instance.SpendSoulShards(n.cost)) return false;
            SaveSystem.Instance.Meta.purchasedUpgrades.Add(n.Id);
            SaveSystem.Instance.Save();
            return true;
        }

        public bool IsPurchased(UpgradeNode n) => Purchased(n);

        /// <summary>Все модификаторы купленных узлов (применяются к базе забега).</summary>
        public List<StatModifier> CollectPurchasedModifiers()
        {
            var list = new List<StatModifier>();
            foreach (var n in allNodes)
                if (n != null && Purchased(n))
                    list.AddRange(n.modifiers);
            return list;
        }
    }
}
