using UnityEngine;
using UnityEngine.UI;
using Game.Core;

namespace Game.UI
{
    /// <summary>
    /// Меню паузы (ТЗ §6 — PauseMenu): продолжить, настройки, выход в меню.
    /// Активируется/прячется UIManager по GameState.Paused.
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        [SerializeField] Button resumeButton;
        [SerializeField] Button menuButton;
        [SerializeField] Slider masterVolume;
        [SerializeField] Slider musicVolume;
        [SerializeField] Slider sfxVolume;

        void Start()
        {
            resumeButton?.onClick.AddListener(() => GameManager.Instance?.SetState(GameState.Playing));
            menuButton?.onClick.AddListener(() =>
            {
                GameManager.Instance?.SetState(GameState.MainMenu);
                SceneLoader.Load(SceneLoader.MainMenu);
            });

            masterVolume?.onValueChanged.AddListener(v => SetVol("MasterVolume", v));
            musicVolume?.onValueChanged.AddListener(v => SetVol("MusicVolume", v));
            sfxVolume?.onValueChanged.AddListener(v => SetVol("SFXVolume", v));
        }

        void SetVol(string param, float v)
            => Audio.AudioManager.Instance?.SetVolume(param, v);
    }
}
