using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Save;

namespace Game.Core
{
    /// <summary>
    /// Единое состояние игры (ТЗ §8 — GameState FSM).
    /// </summary>
    public enum GameState { MainMenu, CharacterSelect, Loading, Playing, Paused, GameOver }

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
            EventBus.OnFloorComplete += HandleFloorComplete;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        protected override void OnDestroy()
        {
            EventBus.OnPlayerDeath -= HandlePlayerDeath;
            EventBus.OnFloorComplete -= HandleFloorComplete;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            base.OnDestroy();
        }

        // Если загрузилась игровая сцена во время Loading — переходим в Playing.
        // Делает переход надёжным даже без SceneBootstrap в сцене (ТЗ §8).
        void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (State == GameState.Loading) SetState(GameState.Playing);
        }

        // ── Управление состоянием ───────────────────────────────────────────────
        public void SetState(GameState next)
        {
            if (State == next) return;
            State = next;
            Time.timeScale = next == GameState.Paused ? 0f : 1f;
            OnStateChanged?.Invoke(next);
        }

        public void TogglePause()
        {
            if (State == GameState.Playing)      SetState(GameState.Paused);
            else if (State == GameState.Paused)  SetState(GameState.Playing);
        }

        /// <summary>Вызывается SceneBootstrap'ом, когда игровая сцена готова.</summary>
        public void MarkPlaying() => SetState(GameState.Playing);

        // ── Жизненный цикл забега ───────────────────────────────────────────────

        /// <summary>Начать новый забег выбранным классом.</summary>
        public void StartNewRun(Data.CharacterData character)
        {
            runData = Data.PlayerRunData.CreateForCharacter(character);
            SetState(GameState.Loading);
            SceneLoader.Load(SceneLoader.GameScene);
            // В Playing переходит SceneBootstrap игровой сцены (через MarkPlaying).
        }

        void HandleFloorComplete(int floor)
        {
            if (runData != null) runData.currentFloor = floor + 1;
        }

        void HandlePlayerDeath()
        {
            SetState(GameState.GameOver);
            // Начисляем заработанные Осколки душ в мета-сейв (ТЗ §4 «Мета-прогрессия»).
            if (runData != null && SaveSystem.Instance != null)
                SaveSystem.Instance.AddSoulShards(runData.SoulShardsEarned());
            SceneLoader.Load(SceneLoader.GameOver);
        }
    }
}
