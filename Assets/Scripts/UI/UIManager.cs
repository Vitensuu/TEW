using UnityEngine;
using UnityEngine.InputSystem;
using Game.Core;

namespace Game.UI
{
    /// <summary>
    /// Переключение панелей UI / HUD (ТЗ §5 — UIManager, Singleton).
    /// Держит ссылки на корневые панели (HUD, Pause, Inventory) и показывает/прячет их.
    /// Esc — пауза (через GameManager).
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        [Header("Панели (корневые GameObject)")]
        [SerializeField] GameObject hudPanel;
        [SerializeField] GameObject pausePanel;
        [SerializeField] GameObject inventoryPanel;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged += HandleState;
        }

        protected override void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= HandleState;
            base.OnDestroy();
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                GameManager.Instance?.TogglePause();

            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
                ToggleInventory();
        }

        void HandleState(GameState state)
        {
            if (pausePanel != null) pausePanel.SetActive(state == GameState.Pause);
            if (hudPanel != null)   hudPanel.SetActive(state == GameState.Run || state == GameState.Pause);
        }

        public void ToggleInventory()
        {
            if (inventoryPanel != null) inventoryPanel.SetActive(!inventoryPanel.activeSelf);
        }

        public void ShowHUD(bool show) { if (hudPanel != null) hudPanel.SetActive(show); }
    }
}
