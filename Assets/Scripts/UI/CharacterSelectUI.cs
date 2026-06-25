using UnityEngine;
using UnityEngine.UI;
using Game.Core;
using Game.Data;
using Game.Save;

namespace Game.UI
{
    /// <summary>
    /// Выбор класса (ТЗ §6 — CharacterSelect): карточки классов + «Начать забег».
    /// Заблокированные классы (по unlockCondition) показываются неактивными.
    /// </summary>
    public class CharacterSelectUI : MonoBehaviour
    {
        [System.Serializable]
        public class Card
        {
            public CharacterData character;
            public Button button;
            public Image highlight;
        }

        [SerializeField] Card[] cards;
        [SerializeField] Button startButton;
        [SerializeField] Text descriptionText;

        CharacterData _selected;

        void Start()
        {
            foreach (var card in cards)
            {
                if (card.button == null || card.character == null) continue;
                bool unlocked = IsUnlocked(card.character);
                card.button.interactable = unlocked;
                var c = card; // замыкание
                card.button.onClick.AddListener(() => Select(c));
            }
            startButton?.onClick.AddListener(StartRun);
            if (startButton != null) startButton.interactable = false;
        }

        bool IsUnlocked(CharacterData c)
        {
            if (c.unlockedByDefault || string.IsNullOrEmpty(c.unlockCondition)) return true;
            return SaveSystem.Instance != null &&
                   SaveSystem.Instance.Meta.HasCharacter(c.characterName);
        }

        void Select(Card card)
        {
            _selected = card.character;
            if (descriptionText != null) descriptionText.text = card.character.description;
            foreach (var c in cards)
                if (c.highlight != null) c.highlight.enabled = (c == card);
            if (startButton != null) startButton.interactable = true;
        }

        void StartRun()
        {
            if (_selected == null) return;
            if (GameManager.Instance != null) GameManager.Instance.StartNewRun(_selected);
            else SceneLoader.Load(SceneLoader.GameScene);
        }
    }
}
