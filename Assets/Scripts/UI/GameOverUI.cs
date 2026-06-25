using UnityEngine;
using UnityEngine.UI;
using Game.Core;
using Game.Save;

namespace Game.UI
{
    /// <summary>
    /// Экран смерти (ТЗ §6 — GameOver): статистика забега, заработанные Осколки,
    /// кнопки. Осколки уже начислены GameManager при смерти; здесь — показ итогов.
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField] Text floorReachedText;
        [SerializeField] Text enemiesKilledText;
        [SerializeField] Text shardsEarnedText;
        [SerializeField] Text totalShardsText;

        [SerializeField] Button retryButton;
        [SerializeField] Button metaButton;
        [SerializeField] Button menuButton;

        void Start()
        {
            var run = GameManager.Instance != null ? GameManager.Instance.Run : null;
            if (run != null)
            {
                if (floorReachedText  != null) floorReachedText.text  = $"Этаж: {run.currentFloor}";
                if (enemiesKilledText != null) enemiesKilledText.text = $"Убито врагов: {run.enemiesKilled}";
                if (shardsEarnedText  != null) shardsEarnedText.text  = $"Осколков заработано: {run.SoulShardsEarned()}";
            }
            if (totalShardsText != null && SaveSystem.Instance != null)
                totalShardsText.text = $"Всего осколков: {SaveSystem.Instance.SoulShards}";

            retryButton?.onClick.AddListener(() => SceneLoader.Load(SceneLoader.CharacterSelect));
            metaButton?.onClick.AddListener(() => SceneLoader.Load(SceneLoader.MetaUpgrades));
            menuButton?.onClick.AddListener(() => SceneLoader.Load(SceneLoader.MainMenu));
        }
    }
}
