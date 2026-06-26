using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Точка сборки персистентных менеджеров (ТЗ §4-5 — Bootstrap, ServiceLocator).
    /// Положи ОДИН объект с этим компонентом в стартовую сцену (MainMenu).
    /// Создаёт/находит глобальные менеджеры (DontDestroyOnLoad), которые
    /// сами регистрируются в ServiceLocator (см. Singleton).
    ///
    /// Делает только глобальный слой. Привязку к конкретной сцене (Player, Room*,
    /// UI-панели) выполняет SceneBootstrap.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameBootstrap : MonoBehaviour
    {
        [Tooltip("Префаб с GameManager (опц.). Если пусто — создаётся автоматически.")]
        [SerializeField] GameObject gameManagerPrefab;
        [SerializeField] GameObject saveSystemPrefab;
        [SerializeField] GameObject audioManagerPrefab;
        [SerializeField] GameObject inventoryManagerPrefab;

        static bool _booted;

        void Awake()
        {
            if (_booted) { Destroy(gameObject); return; }
            _booted = true;

            EnsureManager<GameManager>(gameManagerPrefab, "GameManager");
            EnsureManager<Game.Save.SaveSystem>(saveSystemPrefab, "SaveSystem");
            EnsureManager<Game.Audio.AudioManager>(audioManagerPrefab, "AudioManager");
            EnsureManager<Game.Items.InventoryManager>(inventoryManagerPrefab, "InventoryManager");
        }

        static void EnsureManager<T>(GameObject prefab, string name) where T : MonoBehaviour
        {
            if (Object.FindFirstObjectByType<T>() != null) return;

            if (prefab != null) Instantiate(prefab);
            else
            {
                var go = new GameObject(name);
                go.AddComponent<T>();
            }
        }
    }
}
