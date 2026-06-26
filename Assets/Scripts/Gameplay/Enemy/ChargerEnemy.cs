using UnityEngine;
using Game.Combat;

namespace Enemy
{
    /// <summary>
    /// Враг-таран на единой FSM (<see cref="StateMachineEnemy"/>):
    /// Idle → Chase → Special (windup → charge → recover). Промах рывка → самостан.
    /// Рывок — таймированная последовательность, поэтому держит LockState и сам
    /// владеет переходами. Поведение идентично прежней версии.
    /// </summary>
    public class ChargerEnemy : StateMachineEnemy
    {
        [Header("Charger")]
        [SerializeField] float chargeRange    = 4.5f;  // дистанция начала рывка
        [SerializeField] float chargeSpeed    = 11f;
        [SerializeField] float windUpTime     = 0.5f;
        [SerializeField] float chargeDuration = 0.6f;
        [SerializeField] float missStunTime   = 1.5f;

        enum ChargePhase { WindUp, Charge, Recover }
        ChargePhase _phase;
        Vector2 _chargeDir;
        float   _timer;
        bool    _hitDuringCharge;

        protected override EnemyState DecideState(float dist)
        {
            if (dist > detectionRange) return EnemyState.Idle;
            if (dist <= chargeRange)   return EnemyState.Special; // начать рывок
            return EnemyState.Chase;
        }

        protected override void OnEnterState(EnemyState s)
        {
            if (s != EnemyState.Special) return;
            // Старт рывка: фиксируем направление и блокируем переходы.
            _phase     = ChargePhase.WindUp;
            _timer     = windUpTime;
            _chargeDir = DirToPlayer;
            FaceDirection(_chargeDir);
            LockState  = true;
            StopMoving();
        }

        protected override void OnChase()
        {
            MoveInDirection(DirToPlayer, moveSpeed * SpeedMul);
        }

        protected override void OnSpecial()
        {
            float dt = Time.deltaTime;
            switch (_phase)
            {
                case ChargePhase.WindUp:
                    StopMoving();
                    _timer -= dt;
                    if (_timer <= 0f)
                    {
                        _phase = ChargePhase.Charge;
                        _timer = chargeDuration;
                        _hitDuringCharge = false;
                    }
                    break;

                case ChargePhase.Charge:
                    MoveInDirection(_chargeDir, chargeSpeed);
                    if (DistanceToPlayer <= attackRange) { HitPlayer(); _hitDuringCharge = true; }
                    _timer -= dt;
                    if (_timer <= 0f)
                    {
                        StopMoving();
                        if (!_hitDuringCharge && Status != null)
                            Status.Apply(StatusEffect.Stun, missStunTime); // промах → самостан (ТЗ §4)
                        _phase = ChargePhase.Recover;
                        _timer = 0.4f;
                    }
                    break;

                case ChargePhase.Recover:
                    StopMoving();
                    _timer -= dt;
                    if (_timer <= 0f)
                    {
                        LockState = false;
                        SetState(EnemyState.Chase);
                    }
                    break;
            }
        }

        void HitPlayer()
        {
            var ph = Player.GetComponent<PlayerHealth>();
            if (ph != null && ph.IsAlive)
            {
                float dmg = attackDamage * damageDealtMultiplier;
                ph.TakeDamage(dmg);
                NotifyDealtDamage(ph, dmg);
            }
        }
    }
}
