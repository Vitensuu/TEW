using UnityEngine;
using Game.Core;
using Game.Data;
using Game.Combat;

namespace Enemy
{
    /// <summary>
    /// Способность «Гнилостного Носителя» (роль Tank/Disruptor, Фаза 6): оставляет
    /// ядовитые облака (<see cref="HazardZone"/>) на своём пути, а при смерти —
    /// большой ядовитый взрыв. Наказывает мили-туннелинг по толстому танку.
    /// Подписывается на <see cref="EnemyBase.Died"/>.
    /// </summary>
    public class PlagueBearer : MonoBehaviour
    {
        [SerializeField] float trailInterval = 0.8f;

        EnemyBase _eb;
        float _t;

        void Awake() => _eb = GetComponent<EnemyBase>();
        void OnEnable()  { if (_eb != null) _eb.Died += OnDied; }
        void OnDisable() { if (_eb != null) _eb.Died -= OnDied; }

        void Update()
        {
            if (_eb == null || _eb.Dead) return;
            _t -= Time.deltaTime;
            if (_t <= 0f)
            {
                _t = trailInterval;
                HazardZone.SpawnCircle(transform.position, 0.9f, 2.5f, 1.5f, DamageType.Poison,
                    StatusEffect.Poison, 2f, 1.5f);
            }
        }

        void OnDied()
        {
            HazardZone.SpawnCircle(transform.position, 2.5f, 5f, 3f, DamageType.Poison,
                StatusEffect.Poison, 3f, 2f);
        }
    }
}
