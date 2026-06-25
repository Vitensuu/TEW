using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Базовый персистентный синглтон для менеджеров (ТЗ §5 — Singleton pattern).
    /// Наследники: GameManager, AudioManager, UIManager, InventoryManager, SaveSystem.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        public static T Instance { get; private set; }

        [SerializeField] bool persistAcrossScenes = true;

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = (T)this;
            if (persistAcrossScenes && transform.parent == null)
                DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
