using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Состояние игры (ТЗ §5 — GameManager FSM: Menu/Run/Pause/Dead).
    /// </summary>
    public enum GameState { Boot, Menu, CharacterSelect, Run, Pause, Dead }

    /// <summary>
    /// Центральный менеджер состояния (ТЗ §5 — GameManager, Singleton).
    /// Держит текущий забег (PlayerRunData), мета-данные, управляет паузой и
    /// переходами состояний. Реагирует на EventBus (смерть → Dead).
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        [Header("Текущий забег (runtime)")]
        [SerializeField] Data.PlayerRunData runData;

        public GameState State { get; private set; } = GameState.Boot;
        public Data.PlayerRunData Run => runData;

        public System.Action<GameState> OnStateChanged;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;
            EventBus.OnPlayerDeath += HandlePlayerDeath;
            EventBus.OnFloorComplete += HandleFloorComplete;
        }

        protected override void OnDestroy()
        {
            EventBus.OnPlayerDeath -= HandlePlayerDeath;
            EventBus.OnFloorComplete -= HandleFloorComplete;
            base.OnDestroy();
        }

        // ── Управление состоянием ───────────────────────────────────────────────
        public void SetState(GameState next)
        {
            if (State == next) return;
            State = next;
            Time.timeScale = next == GameState.Pause ? 0f : 1f;
            OnStateChanged?.Invoke(next);
        }

        public void TogglePause()
        {
            if (State == GameState.Run)        SetState(GameState.Pause);
            else if (State == GameState.Pause) SetState(GameState.Run);
        }

        // ── Жизненный цикл забега ───────────────────────────────────────────────

        /// <summary>Начать новый забег выбранным классом.</summary>
        public void StartNewRun(Data.CharacterData character)
        {
            runData = Data.PlayerRunData.CreateForCharacter(character);
            SetState(GameState.Run);
            SceneLoader.Load(SceneLoader.GameScene);
        }

        void HandleFloorComplete(int floor)
        {
            if (runData != null) runData.currentFloor = floor + 1;
        }

        void HandlePlayerDeath()
        {
            SetState(GameState.Dead);
            // Начисляем заработанные Осколки душ в мета-сейв (ТЗ §4 «Мета-прогрессия»).
            if (runData != null && SaveSystem.Instance != null)
                SaveSystem.Instance.AddSoulShards(runData.SoulShardsEarned());
            SceneLoader.Load(SceneLoader.GameOver);
        }
    }
}
