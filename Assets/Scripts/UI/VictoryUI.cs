using UnityEngine;
using UnityEngine.UI;
using Game.Core;

namespace Game.UI
{
    /// <summary>
    /// Экран победы (ТЗ §6 — Victory): статистика забега (время, убийства, золото,
    /// использованные предметы) и кнопки New Run / Main Menu. Оверлей-панель в
    /// игровой сцене; включается UIManager'ом по GameState.Victory. Данные берёт
    /// из GameManager.Run, переходы — через GameManager (без FindObjectOfType).
    /// </summary>
    public class VictoryUI : MonoBehaviour
    {
        [SerializeField] Text timeText;
        [SerializeField] Text killsText;
        [SerializeField] Text goldText;
        [SerializeField] Text itemsText;

        [SerializeField] Button newRunButton;
        [SerializeField] Button menuButton;

        void Start()
        {
            newRunButton?.onClick.AddListener(() => GameManager.Instance?.RestartRun());
            menuButton?.onClick.AddListener(() => GameManager.Instance?.ToMainMenu());
            Refresh();
        }

        // Панель включается при переходе в Victory — обновляем статистику.
        void OnEnable() => Refresh();

        void Refresh()
        {
            var run = GameManager.Instance != null ? GameManager.Instance.Run : null;
            if (run == null) return;

            int m = Mathf.FloorToInt(run.runTimeSeconds / 60f);
            int s = Mathf.FloorToInt(run.runTimeSeconds % 60f);
            if (timeText  != null) timeText.text  = $"Время: {m:00}:{s:00}";
            if (killsText != null) killsText.text = $"Убийств: {run.enemiesKilled}";
            if (goldText  != null) goldText.text  = $"Золото: {run.gold}";
            if (itemsText != null) itemsText.text = $"Предметов использовано: {run.itemsUsed}";
        }
    }
}
