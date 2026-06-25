using UnityEngine;
using Game.Combat;

namespace Enemy
{
    /// <summary>
    /// Призыватель (ТЗ §4 — «периодически призывает миньонов, уязвим в момент призыва»).
    /// Держит дистанцию, периодически входит в Summon: останавливается, открывается
    /// для урона (vulnerabilityMultiplier), спавнит миньонов.
    /// </summary>
    public class SummonerEnemy : EnemyBase
    {
        [Header("Summoner AI")]
        [SerializeField] float moveSpeed      = 1.8f;
        [SerializeField] float detectionRange = 9f;
        [SerializeField] float keepDistance   = 6f;
        [SerializeField] float summonCooldown = 6f;
        [SerializeField] float summonCastTime = 1.5f;

        [Header("Призыв")]
        [SerializeField] GameObject minionPrefab;
        [SerializeField] int minionsPerSummon = 2;
        [SerializeField] float summonRadius = 1.5f;
        [Tooltip("Множитель получаемого урона во время призыва (уязвимость)")]
        [SerializeField] float vulnerabilityMultiplier = 2f;

        Transform _player;
        StatusEffectHandler _status;
        float _summonTimer;
        bool _isSummoning;

        protected override void Awake()
        {
            base.Awake();
            _status = GetComponent<StatusEffectHandler>();
            _summonTimer = summonCooldown;
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

            if (_isSummoning) return; // во время каста стоит на месте (корутина)

            float dist = Vector2.Distance(transform.position, _player.position);
            float speedMul = _status != null ? _status.SpeedMultiplier : 1f;
            Vector2 toPlayer = ((Vector2)_player.position - (Vector2)transform.position).normalized;

            _summonTimer -= Time.deltaTime;
            if (_summonTimer <= 0f && dist <= detectionRange)
            {
                StartCoroutine(SummonRoutine());
                return;
            }

            if (dist > detectionRange) { Stop(); return; }

            // Держим дистанцию.
            if (dist < keepDistance)
            {
                Rb.linearVelocity = -toPlayer * moveSpeed * speedMul;
                SetMoving(-toPlayer);
            }
            else { Stop(); }
            SetDirection(toPlayer);
        }

        System.Collections.IEnumerator SummonRoutine()
        {
            _isSummoning = true;
            _summonTimer = summonCooldown;
            Stop();

            // Уязвим во время призыва.
            yield return new WaitForSeconds(summonCastTime);

            if (!IsDead && minionPrefab != null)
                for (int i = 0; i < minionsPerSummon; i++)
                {
                    Vector2 offset = Random.insideUnitCircle * summonRadius;
                    Instantiate(minionPrefab, transform.position + (Vector3)offset, Quaternion.identity);
                }

            _isSummoning = false;
        }

        // Уязвимость в момент призыва (ТЗ §4): умножаем входящий урон.
        public override void TakeDamage(float amount, Game.Core.DamageType type = Game.Core.DamageType.Physical)
        {
            if (_isSummoning) amount *= vulnerabilityMultiplier;
            base.TakeDamage(amount, type);
        }

        void Stop()
        {
            Rb.linearVelocity = Vector2.zero;
            SetMoving(Vector2.zero);
        }
    }
}
