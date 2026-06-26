using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Привязка к конкретной сцене (ТЗ §4-5 — SceneBootstrap).
    /// Положи ОДИН объект с этим компонентом в игровую сцену. Он:
    ///   • при необходимости создаёт GameBootstrap (если сцену запустили напрямую из редактора);
    ///   • переводит GameState в нужное состояние (по умолчанию Playing);
    ///   • при выходе из сцены может почистить scene-scoped подписки EventBus.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public class SceneBootstrap : MonoBehaviour
    {
        [Header("Состояние при входе в сцену")]
        [SerializeField] GameState enterState = GameState.Playing;
        [SerializeField] bool setState = true;

        [Header("Fallback для запуска сцены напрямую (Editor)")]
        [Tooltip("Создать GameBootstrap, если глобальные менеджеры ещё не подняты.")]
        [SerializeField] GameObject gameBootstrapPrefab;

        void Awake()
        {
            // Сцену могли запустить напрямую (Play в редакторе) минуя MainMenu —
            // поднимем глобальный слой, чтобы менеджеры были доступны в ServiceLocator.
            if (ServiceLocator.Get<GameManager>() == null && Object.FindFirstObjectByType<GameBootstrap>() == null)
            {
                if (gameBootstrapPrefab != null) Instantiate(gameBootstrapPrefab);
                else new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
            }
        }

        void Start()
        {
            if (setState)
            {
                var gm = ServiceLocator.Get<GameManager>() ?? GameManager.Instance;
                gm?.SetState(enterState);
            }
        }
    }
}
