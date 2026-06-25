using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Гарантирует наличие персистентных менеджеров (ТЗ §2 — Core).
    /// Положи ОДИН объект с этим компонентом в стартовую сцену (MainMenu).
    /// Если менеджеры не назначены префабами — создаёт пустые GameObject с нужными
    /// компонентами. Все они DontDestroyOnLoad (см. Singleton).
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class Bootstrap : MonoBehaviour
    {
        [Tooltip("Префаб с GameManager (опц.). Если пусто — создаётся автоматически.")]
        [SerializeField] GameObject gameManagerPrefab;
        [SerializeField] GameObject saveSystemPrefab;
        [SerializeField] GameObject audioManagerPrefab;
        [SerializeField] GameObject inventoryManagerPrefab;

        void Awake()
        {
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
