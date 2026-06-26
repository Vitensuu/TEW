using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Save;

namespace Game.Core
{
    /// <summary>
    /// Единое состояние игры (ТЗ §8 — GameState FSM).
    /// Playing = Gameplay, Paused = Pause (исторические имена проекта).
    /// Состояния после Victory зарезервированы под расширение — добавляются без
    /// правки логики переходов (ChangeState универсален).
    /// </summary>
    public enum GameState
    {
        MainMenu,
        CharacterSelect,
        Loading,
        Playing,        // Gameplay
        Paused,         // Pause
        GameOver,
        Victory,
        // ── Резерв (ТЗ — будущие состояния) ──
        Shop,
        Dialogue,
        Cutscene,
        BossIntro,
    }

    /// <summary>
    /// Центральный менеджер состояния (ТЗ §5 — GameManager, Singleton).
    /// Держит текущий забег (PlayerRunData), мета-данные, управляет паузой и
    /// переходами состояний. Реагирует на EventBus (смерть → Dead).
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        [Header("Текущий забег (runtime)")]
        [SerializeField] Data.PlayerRunData runData;

        public GameState State { get; private set; } = GameState.MainMenu;
        public Data.PlayerRunData Run => runData;

        public System.Action<GameState> OnStateChanged;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;
            EventBus.OnPlayerDeath += HandlePlayerDeath;
            EventBus.OnVictory += HandleVictory;
            EventBus.OnFloorComplete += HandleFloorComplete;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        protected override void OnDestroy()
        {
            EventBus.OnPlayerDeath -= HandlePlayerDeath;
            EventBus.OnVictory -= HandleVictory;
            EventBus.OnFloorComplete -= HandleFloorComplete;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            base.OnDestroy();
        }

        // Тикаем время забега только в Gameplay (для статистики победы/смерти).
        void Update()
        {
            if (State == GameState.Playing && runData != null)
                runData.runTimeSeconds += Time.deltaTime;
        }

        // Если загрузилась игровая сцена во время Loading — переходим в Playing.
        // Делает переход надёжным даже без SceneBootstrap в сцене (ТЗ §8).
        void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (State == GameState.Loading) SetState(GameState.Playing);
        }

        // ── Управление состоянием ───────────────────────────────────────────────
        /// <summary>
        /// ЕДИНСТВЕННАЯ точка перехода между состояниями (ТЗ). Замораживает время
        /// для Pause/Victory, шлёт OnStateChanged — UI и системы реагируют сами.
        /// </summary>
        public void ChangeState(GameState next)
        {
            if (State == next) return;
            State = next;
            Time.timeScale = (next == GameState.Paused || next == GameState.Victory) ? 0f : 1f;
            OnStateChanged?.Invoke(next);
        }

        /// <summary>Алиас для обратной совместимости со старым кодом.</summary>
        public void SetState(GameState next) => ChangeState(next);

        public void TogglePause()
        {
            if (State == GameState.Playing)      ChangeState(GameState.Paused);
            else if (State == GameState.Paused)  ChangeState(GameState.Playing);
        }

        /// <summary>Вызывается SceneBootstrap'ом, когда игровая сцена готова.</summary>
        public void MarkPlaying() => ChangeState(GameState.Playing);

        // ── Навигация для кнопок UI (без FindObjectOfType) ──────────────────────
        public void ToMainMenu()
        {
            ChangeState(GameState.MainMenu);
            SceneLoader.Load(SceneLoader.MainMenu);
        }

        public void ToCharacterSelect()
        {
            ChangeState(GameState.CharacterSelect);
            SceneLoader.Load(SceneLoader.CharacterSelect);
        }

        /// <summary>Перезапуск забега тем же классом (кнопки Restart / New Run).</summary>
        public void RestartRun()
        {
            var character = runData != null ? runData.character : null;
            if (character != null) StartNewRun(character);
            else ToCharacterSelect();
        }

        // ── Жизненный цикл забега ───────────────────────────────────────────────

        /// <summary>Начать новый забег выбранным классом.</summary>
        public void StartNewRun(Data.CharacterData character)
        {
            runData = Data.PlayerRunData.CreateForCharacter(character);
            ChangeState(GameState.Loading);
            SceneLoader.Load(SceneLoader.GameScene);
            // В Playing переходит SceneBootstrap игровой сцены (через MarkPlaying).
        }

        void HandleFloorComplete(int floor)
        {
            if (runData != null) runData.currentFloor = floor + 1;
        }

        void HandlePlayerDeath()
        {
            if (State == GameState.GameOver) return;
            // Начисляем заработанные Осколки душ в мета-сейв (ТЗ §4 «Мета-прогрессия»).
            if (runData != null && SaveSystem.Instance != null)
                SaveSystem.Instance.AddSoulShards(runData.SoulShardsEarned());
            ChangeState(GameState.GameOver);
            SceneLoader.Load(SceneLoader.GameOver);
        }

        /// <summary>Победа: убит финальный босс. Вызывается EventBus.OnVictory или WinRun().</summary>
        void HandleVictory()
        {
            if (State == GameState.Victory) return;
            if (runData != null && SaveSystem.Instance != null)
                SaveSystem.Instance.AddSoulShards(runData.SoulShardsEarned());
            // Экран победы — оверлей в игровой сцене (UIManager.victoryPanel).
            ChangeState(GameState.Victory);
        }

        /// <summary>Программный триггер победы (если не через EventBus).</summary>
        public void WinRun() => EventBus.TriggerVictory();
    }
}
