using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.Data;

namespace Game.Combat
{
    public enum StatusEffect { Burn, Freeze, Poison, Stun }

    /// <summary>
    /// Обработчик статус-эффектов (ТЗ §4 — StatusEffectHandler.cs).
    /// Burn/Poison — урон по тику; Freeze — замедление (SpeedMultiplier);
    /// Stun — полная остановка (IsStunned). Враги/игрок читают SpeedMultiplier и
    /// IsStunned. Требует IDamageable на том же объекте для DoT.
    /// </summary>
    public class StatusEffectHandler : MonoBehaviour
    {
        class ActiveEffect
        {
            public StatusEffect type;
            public float timeLeft;
            public float magnitude;   // урон/тик или коэффициент замедления
            public float tickTimer;
        }

        const float TickInterval = 0.5f;

        readonly List<ActiveEffect> _effects = new List<ActiveEffect>();
        IDamageable _damageable;

        /// <summary>Множитель скорости 0..1 (Freeze снижает).</summary>
        public float SpeedMultiplier { get; private set; } = 1f;
        /// <summary>Оглушён ли объект (Stun) — нельзя двигаться/атаковать.</summary>
        public bool IsStunned { get; private set; }

        void Awake() => _damageable = GetComponent<IDamageable>();

        /// <summary>
        /// Наложить эффект. magnitude: Burn/Poison — урон/тик; Freeze — доля
        /// замедления (0.5 = −50% скорости); Stun — игнорируется.
        /// </summary>
        public void Apply(StatusEffect type, float duration, float magnitude = 0f)
        {
            // Рефреш существующего того же типа (берём большее время).
            foreach (var e in _effects)
                if (e.type == type)
                {
                    e.timeLeft = Mathf.Max(e.timeLeft, duration);
                    e.magnitude = Mathf.Max(e.magnitude, magnitude);
                    return;
                }
            _effects.Add(new ActiveEffect { type = type, timeLeft = duration, magnitude = magnitude });
        }

        void Update()
        {
            float dt = Time.deltaTime;
            float slow = 1f;
            bool stun = false;

            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                var e = _effects[i];
                e.timeLeft -= dt;

                switch (e.type)
                {
                    case StatusEffect.Burn:
                    case StatusEffect.Poison:
                        e.tickTimer -= dt;
                        if (e.tickTimer <= 0f)
                        {
                            e.tickTimer = TickInterval;
                            var dmgType = e.type == StatusEffect.Burn
                                ? DamageType.Fire : DamageType.Poison;
                            if (_damageable != null && _damageable.IsAlive)
                                _damageable.TakeDamage(e.magnitude, dmgType);
                        }
                        break;

                    case StatusEffect.Freeze:
                        slow *= (1f - Mathf.Clamp01(e.magnitude));
                        break;

                    case StatusEffect.Stun:
                        stun = true;
                        break;
                }

                if (e.timeLeft <= 0f) _effects.RemoveAt(i);
            }

            SpeedMultiplier = slow;
            IsStunned = stun;
        }

        public bool HasEffect(StatusEffect type)
        {
            foreach (var e in _effects) if (e.type == type) return true;
            return false;
        }

        public void ClearAll() => _effects.Clear();
    }
}
