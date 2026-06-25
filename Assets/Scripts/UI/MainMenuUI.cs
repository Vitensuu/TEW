using UnityEngine;
using UnityEngine.UI;
using Game.Core;

namespace Game.UI
{
    /// <summary>
    /// Главное меню (ТЗ §6 — MainMenu): Новая игра, Продолжить, Настройки, Выход.
    /// Привяжи кнопки к методам в инспекторе или используй авто-привязку ниже.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] Button newGameButton;
        [SerializeField] Button metaButton;
        [SerializeField] Button quitButton;

        void Start()
        {
            newGameButton?.onClick.AddListener(OnNewGame);
            metaButton?.onClick.AddListener(OnMetaUpgrades);
            quitButton?.onClick.AddListener(OnQuit);
        }

        public void OnNewGame()      => SceneLoader.Load(SceneLoader.CharacterSelect);
        public void OnMetaUpgrades() => SceneLoader.Load(SceneLoader.MetaUpgrades);

        public void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
