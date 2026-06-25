using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Core
{
    /// <summary>
    /// Загрузка сцен (ТЗ §2 — Core/SceneLoader). Имена сцен из §6:
    /// MainMenu, CharacterSelect, GameScene, BossRoom, GameOver, MetaUpgrades.
    /// Сцены должны быть добавлены в Build Settings (File ▸ Build Profiles).
    /// </summary>
    public static class SceneLoader
    {
        public const string MainMenu        = "MainMenu";
        public const string CharacterSelect = "CharacterSelect";
        public const string GameScene       = "GameScene";
        public const string BossRoom        = "BossRoom";
        public const string GameOver        = "GameOver";
        public const string MetaUpgrades    = "MetaUpgrades";

        public static void Load(string sceneName) => SceneManager.LoadScene(sceneName);

        /// <summary>Асинхронная загрузка (для экрана загрузки/фейдов).</summary>
        public static IEnumerator LoadAsync(string sceneName, System.Action<float> onProgress = null)
        {
            var op = SceneManager.LoadSceneAsync(sceneName);
            while (op != null && !op.isDone)
            {
                onProgress?.Invoke(op.progress);
                yield return null;
            }
        }

        public static void Reload()
            => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
