using UnityEngine;
using UnityEngine.Audio;

namespace Game.Audio
{
    /// <summary>
    /// Слоистая музыка (ТЗ §7 — «процедурно микшируемый ambient + боевая музыка»).
    /// Два синхронных лупа (ambient + combat), играют одновременно, громкость боевого
    /// слоя поднимается при бою (SetCombatIntensity 0..1). Слои стартуют синхронно,
    /// поэтому переходы бесшовные.
    /// </summary>
    public class LayeredMusicSystem : MonoBehaviour
    {
        [SerializeField] AudioMixerGroup musicGroup;
        [SerializeField] AudioClip ambientLayer;
        [SerializeField] AudioClip combatLayer;
        [SerializeField] float blendSpeed = 2f;

        AudioSource _ambient;
        AudioSource _combat;
        float _targetCombat;

        void Awake()
        {
            _ambient = NewSource(ambientLayer);
            _combat  = NewSource(combatLayer);
            _combat.volume = 0f;
        }

        void Start()
        {
            // Синхронный старт обоих слоёв.
            double t = AudioSettings.dspTime + 0.1;
            if (_ambient.clip != null) _ambient.PlayScheduled(t);
            if (_combat.clip  != null) _combat.PlayScheduled(t);
        }

        AudioSource NewSource(AudioClip clip)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.clip = clip;
            s.loop = true;
            s.playOnAwake = false;
            s.outputAudioMixerGroup = musicGroup;
            return s;
        }

        void Update()
        {
            _combat.volume = Mathf.MoveTowards(_combat.volume, _targetCombat, blendSpeed * Time.deltaTime);
            _ambient.volume = Mathf.MoveTowards(_ambient.volume, 1f - _targetCombat * 0.5f, blendSpeed * Time.deltaTime);
        }

        /// <summary>0 = тишина боя (только ambient), 1 = полный боевой слой.</summary>
        public void SetCombatIntensity(float intensity01)
            => _targetCombat = Mathf.Clamp01(intensity01);
    }
}
