using UnityEngine;
using Game.Combat;

namespace Enemy
{
    /// <summary>
    /// Враг-таран (ТЗ §4 — «ускоряется к игроку, после промаха — stun»).
    /// State Machine: Idle → Chase → WindUp → Charge → (Hit | Miss→Stun).
    /// </summary>
    public class ChargerEnemy : EnemyBase
    {
        [Header("Charger AI")]
        [SerializeField] float moveSpeed      = 2f;
        [SerializeField] float detectionRange = 7f;
        [SerializeField] float chargeRange    = 4.5f;  // дистанция начала рывка
        [SerializeField] float chargeSpeed    = 11f;
        [SerializeField] float windUpTime     = 0.5f;
        [SerializeField] float chargeDuration = 0.6f;
        [SerializeField] float missStunTime   = 1.5f;

        enum CState { Idle, Chase, WindUp, Charge, Recover }
        CState _state = CState.Idle;

        Transform _player;
        StatusEffectHandler _status;
        Vector2 _chargeDir;
        float _timer;
        bool _hitDuringCharge;

        protected override void Awake()
        {
            base.Awake();
            _status = GetComponent<StatusEffectHandler>();
        }

        void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _player = p.transform;
        }

        protected override void Update()
        {
            base.Update();
            if (IsDead || _player == null) return;
            if (_status != null && _status.IsStunned) { Stop(); return; }

            float dist = Vector2.Distance(transform.position, _player.position);
            float speedMul = _status != null ? _status.SpeedMultiplier : 1f;
            Vector2 toPlayer = ((Vector2)_player.position - (Vector2)transform.position).normalized;

            switch (_state)
            {
                case CState.Idle:
                    Stop();
                    if (dist <= detectionRange) _state = CState.Chase;
                    break;

                case CState.Chase:
                    Rb.linearVelocity = toPlayer * moveSpeed * speedMul;
                    SetMoving(toPlayer);
                    if (dist > detectionRange) _state = CState.Idle;
                    else if (dist <= chargeRange) BeginWindUp(toPlayer);
                    break;

                case CState.WindUp:
                    Stop();
                    _timer -= Time.deltaTime;
                    if (_timer <= 0f)
                    {
                        _state = CState.Charge;
                        _timer = chargeDuration;
                        _hitDuringCharge = false;
                    }
                    break;

                case CState.Charge:
                    Rb.linearVelocity = _chargeDir * chargeSpeed;
                    SetMoving(_chargeDir);
                    if (dist <= attackRange) { HitPlayer(); _hitDuringCharge = true; }
                    _timer -= Time.deltaTime;
                    if (_timer <= 0f)
                    {
                        Stop();
                        // Промах → самостан (ТЗ §4).
                        if (!_hitDuringCharge && _status != null)
                            _status.Apply(StatusEffect.Stun, missStunTime);
                        _state = CState.Recover;
                        _timer = 0.4f;
                    }
                    break;

                case CState.Recover:
                    Stop();
                    _timer -= Time.deltaTime;
                    if (_timer <= 0f) _state = CState.Chase;
                    break;
            }
        }

        void BeginWindUp(Vector2 dir)
        {
            _state = CState.WindUp;
            _timer = windUpTime;
            _chargeDir = dir;
            SetDirection(dir);
        }

        void HitPlayer()
        {
            var ph = _player.GetComponent<PlayerHealth>();
            if (ph != null && ph.IsAlive) ph.TakeDamage(attackDamage);
        }

        void Stop()
        {
            Rb.linearVelocity = Vector2.zero;
            SetMoving(Vector2.zero);
        }
    }
}
