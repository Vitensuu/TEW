using UnityEngine;

namespace Enemy
{
    /// <summary>
    /// Призыватель на единой FSM (<see cref="StateMachineEnemy"/>):
    /// Idle (вне детекта) → Chase (держит дистанцию) → Special (призыв миньонов).
    /// Во время призыва уязвим (vulnerabilityMultiplier) и стоит на месте.
    /// Призыв — таймированная корутина, держит LockState. Поведение идентично прежней версии.
    /// </summary>
    public class SummonerEnemy : StateMachineEnemy
    {
        [Header("Summoner")]
        [SerializeField] float keepDistance   = 6f;
        [SerializeField] float summonCooldown = 6f;
        [SerializeField] float summonCastTime = 1.5f;

        [Header("Призыв")]
        [SerializeField] GameObject minionPrefab;
        [SerializeField] int   minionsPerSummon = 2;
        [SerializeField] float summonRadius = 1.5f;
        [Tooltip("Множитель получаемого урона во время призыва (уязвимость)")]
        [SerializeField] float vulnerabilityMultiplier = 2f;

        float _summonTimer;
        bool  _isSummoning;

        protected override void Awake()
        {
            base.Awake();
            _summonTimer = summonCooldown;
        }

        protected override EnemyState DecideState(float dist)
            => dist > detectionRange ? EnemyState.Idle : EnemyState.Chase;

        protected override void OnChase()
        {
            _summonTimer -= Time.deltaTime;
            if (_summonTimer <= 0f && DistanceToPlayer <= detectionRange)
            {
                StartCoroutine(SummonRoutine());
                return;
            }

            // Держим дистанцию: слишком близко — отступаем, иначе стоим.
            float dist  = DistanceToPlayer;
            Vector2 toP = DirToPlayer;
            if (dist < keepDistance) MoveInDirection(-toP, moveSpeed * SpeedMul);
            else                     StopMoving();
            FaceDirection(toP);
        }

        // Призыв ведёт корутина; обработчик Special — пустой (стоим на месте).
        protected override void OnSpecial() { }

        System.Collections.IEnumerator SummonRoutine()
        {
            _isSummoning = true;
            _summonTimer = summonCooldown;
            LockState    = true;
            SetState(EnemyState.Special);
            StopMoving();

            // Уязвим во время каста.
            yield return new WaitForSeconds(summonCastTime);

            if (!IsDead && minionPrefab != null)
                for (int i = 0; i < minionsPerSummon; i++)
                {
                    Vector2 offset = Random.insideUnitCircle * summonRadius;
                    Instantiate(minionPrefab, transform.position + (Vector3)offset, Quaternion.identity);
                }

            _isSummoning = false;
            LockState    = false;
            SetState(EnemyState.Chase);
        }

        // Уязвимость в момент призыва (ТЗ §4): умножаем входящий урон.
        public override void TakeDamage(float amount, Game.Data.DamageType type = Game.Data.DamageType.Physical)
        {
            if (_isSummoning) amount *= vulnerabilityMultiplier;
            base.TakeDamage(amount, type);
        }
    }
}
