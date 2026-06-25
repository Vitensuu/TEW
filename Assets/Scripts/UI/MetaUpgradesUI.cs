using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Game.Core;
using Game.Save;

namespace Game.UI
{
    /// <summary>
    /// Экран дерева мета-улучшений (ТЗ §6 — MetaUpgrades): 3 ветки × 10 узлов,
    /// баланс Осколков. Узлы покупаются за Осколки душ через MetaProgression.
    /// </summary>
    public class MetaUpgradesUI : MonoBehaviour
    {
        [System.Serializable]
        public class NodeButton
        {
            public UpgradeNode node;
            public Button button;
            public Image background;
            public Text label;
        }

        [SerializeField] NodeButton[] nodeButtons;
        [SerializeField] Text shardsText;
        [SerializeField] Button backButton;

        static readonly Color BoughtColor    = new Color(0.2f, 0.8f, 0.3f);
        static readonly Color AvailableColor = new Color(0.9f, 0.8f, 0.2f);
        static readonly Color LockedColor    = new Color(0.4f, 0.4f, 0.4f);

        void Start()
        {
            foreach (var nb in nodeButtons)
            {
                if (nb.button == null || nb.node == null) continue;
                var local = nb;
                nb.button.onClick.AddListener(() => Buy(local));
            }
            backButton?.onClick.AddListener(() => SceneLoader.Load(SceneLoader.MainMenu));
            Refresh();
        }

        void Buy(NodeButton nb)
        {
            if (MetaProgression.Instance == null) return;
            if (MetaProgression.Instance.Purchase(nb.node)) Refresh();
        }

        void Refresh()
        {
            if (shardsText != null && SaveSystem.Instance != null)
                shardsText.text = $"Осколки душ: {SaveSystem.Instance.SoulShards}";

            var meta = MetaProgression.Instance;
            foreach (var nb in nodeButtons)
            {
                if (nb.node == null) continue;
                if (nb.label != null)
                    nb.label.text = $"{nb.node.title}\n{nb.node.cost}";

                bool bought    = meta != null && meta.IsPurchased(nb.node);
                bool available = meta != null && meta.CanPurchase(nb.node);

                if (nb.background != null)
                    nb.background.color = bought ? BoughtColor
                                        : available ? AvailableColor
                                        : LockedColor;
                if (nb.button != null)
                    nb.button.interactable = available;
            }
        }
    }
}
