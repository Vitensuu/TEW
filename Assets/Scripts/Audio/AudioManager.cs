using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Game.Core;

namespace Game.Audio
{
    /// <summary>
    /// Менеджер звука (ТЗ §5 — AudioManager, Singleton; §7 — каналы Master/Music/SFX).
    /// Пул источников для SFX (низкая латентность), один источник для музыки с
    /// кроссфейдом. Маршрутизация через AudioMixer groups.
    /// </summary>
    public class AudioManager : Singleton<AudioManager>
    {
        [Header("Mixer (ТЗ §7 — Master/Music/SFX)")]
        [SerializeField] AudioMixer mixer;
        [SerializeField] AudioMixerGroup musicGroup;
        [SerializeField] AudioMixerGroup sfxGroup;

        [Header("SFX-пул")]
        [SerializeField] int sfxPoolSize = 12;

        AudioSource _musicSource;
        readonly List<AudioSource> _sfxPool = new List<AudioSource>();
        int _sfxIndex;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
            _musicSource.outputAudioMixerGroup = musicGroup;

            for (int i = 0; i < sfxPoolSize; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.outputAudioMixerGroup = sfxGroup;
                _sfxPool.Add(s);
            }
        }

        // ── SFX (ТЗ §7 — шаги/атака/попадание/смерть/подбор) ───────────────────
        public void PlaySfx(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;
            var src = _sfxPool[_sfxIndex];
            _sfxIndex = (_sfxIndex + 1) % _sfxPool.Count;
            src.pitch = pitch;
            src.PlayOneShot(clip, volume);
        }

        public void PlaySfxAt(AudioClip clip, Vector3 pos, float volume = 1f)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, pos, volume);
        }

        // ── Музыка с кроссфейдом ────────────────────────────────────────────────
        public void PlayMusic(AudioClip clip, float fade = 1f)
        {
            if (clip == null || _musicSource.clip == clip) return;
            StopAllCoroutines();
            StartCoroutine(CrossfadeTo(clip, fade));
        }

        System.Collections.IEnumerator CrossfadeTo(AudioClip clip, float fade)
        {
            float startVol = _musicSource.volume;
            for (float t = 0; t < fade; t += Time.unscaledDeltaTime)
            {
                _musicSource.volume = Mathf.Lerp(startVol, 0f, t / fade);
                yield return null;
            }
            _musicSource.clip = clip;
            _musicSource.Play();
            for (float t = 0; t < fade; t += Time.unscaledDeltaTime)
            {
                _musicSource.volume = Mathf.Lerp(0f, 1f, t / fade);
                yield return null;
            }
            _musicSource.volume = 1f;
        }

        // ── Громкость каналов (для меню настроек) ───────────────────────────────
        public void SetVolume(string exposedParam, float linear01)
        {
            if (mixer == null) return;
            float dB = linear01 <= 0.0001f ? -80f : Mathf.Log10(linear01) * 20f;
            mixer.SetFloat(exposedParam, dB);
        }
    }
}
