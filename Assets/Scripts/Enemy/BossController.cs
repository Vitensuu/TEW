using UnityEngine;
using Game.Combat;
using Game.Core;
using Game.Data;
using Game.Items;

namespace Enemy
{
    /// <summary>
    /// Босс (ТЗ §4 — «несколько фаз с разными паттернами, арена»; §5 — BossController).
    /// Переходы фаз по проценту HP (BossData.phases). Каждая фаза задаёт скорость,
    /// кулдаун, снаряд/залп, призыв миньонов. При смерти — гарантированный дроп.
    /// Назначь поле data = BossData в инспекторе префаба.
    /// </summary>
    public class BossController : EnemyBase
    {
        [Header("Boss")]
        [SerializeField] LayerMask playerMask;
        [SerializeField] float projectileSpeed = 6f;

        public System.Action<int> OnPhaseChanged; // индекс фазы (для UI/музыки)
        public System.Action OnBossDefeated;

        BossData _boss;
        Transform _player;
        StatusEffectHandler _status;
        int _phaseIndex = -1;
        BossPhase _phase;
        float _attackTimer;

        protected override void Awake()
        {
            base.Awake();
            _status = GetComponent<StatusEffectHandler>();
            _boss = data as BossData;
        }

        protected override void ApplyData()
        {
            base.ApplyData();
            // maxHp у босса берётся из EnemyData.maxHealth (наследник).
        }

        void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _player = p.transform;

            if (_boss != null && _boss.bossMusic != null && Game.Audio.AudioManager.Instance != null)
                Game.Audio.AudioManager.Instance.PlayMusic(_boss.bossMusic);

            EnterPhaseForHealth();
        }

        protected override void Update()
        {
            base.Update();
            if (IsDead || _player == null || _boss == null) return;
            if (_status != null && _status.IsStunned) { Stop(); return; }

            EnterPhaseForHealth();
            if (_phase == null) return;

            float speedMul = _status != null ? _status.SpeedMultiplier : 1f;
            Vector2 toPlayer = ((Vector2)_player.position - (Vector2)transform.position).normalized;

            // Движение к игроку (арена ограничена — простое преследование).
            Rb.linearVelocity = toPlayer * _phase.moveSpeed * speedMul;
            SetMoving(toPlayer);
            SetDirection(toPlayer);

            // Контактный урон.
            if (Vector2.Distance(transform.position, _player.position) <= attackRange)
                _player.GetComponent<PlayerHealth>()?.TakeDamage(_phase.contactDamage * Time.deltaTime);

            // Атака фазы.
            _attackTimer -= Time.deltaTime;
            if (_attackTimer <= 0f)
            {
                _attackTimer = _phase.attackCooldown;
                DoPhaseAttack(toPlayer);
            }
        }

        // ── Фазы ─────────────────────────────────────────────────────────────────
        void EnterPhaseForHealth()
        {
            if (_boss == null || _boss.phases.Count == 0) return;

            float pct = Hp / Mathf.Max(1f, maxHp);

            // Фазы отсортированы по убыванию healthThreshold; выбираем активную
            // как последнюю, чей порог >= текущего %HP.
            int target = 0;
            for (int i = 0; i < _boss.phases.Count; i++)
                if (pct <= _boss.phases[i].healthThreshold) target = i;

            if (target != _phaseIndex)
            {
                _phaseIndex = target;
                _phase = _boss.phases[target];
                _attackTimer = 0f;
                OnPhaseChanged?.Invoke(_phaseIndex);
            }
        }

        void DoPhaseAttack(Vector2 toPlayer)
        {
            if (_phase.summonsMinions && _phase.minionToSummon != null)
            {
                for (int i = 0; i < _phase.minionsPerSummon; i++)
                {
                    if (_phase.minionToSummon.spritePrefab == null) break;
                    Vector2 off = Random.insideUnitCircle * 2f;
                    Instantiate(_phase.minionToSummon.spritePrefab,
                        transform.position + (Vector3)off, Quaternion.identity);
                }
            }

            if (_phase.projectilePrefab != null)
            {
                int n = Mathf.Max(1, _phase.projectilesPerVolley);
                float step = 360f / n;
                for (int i = 0; i < n; i++)
                {
                    float ang = step * i * Mathf.Deg2Rad;
                    Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                    var go = Instantiate(_phase.projectilePrefab, transform.position, Quaternion.identity);
                    var proj = go.GetComponent<Projectile>() ?? go.AddComponent<Projectile>();
                    proj.Init(dir, projectileSpeed, _phase.contactDamage, DamageType.Magic, false, playerMask);
                }
            }
        }

        protected override void Die()
        {
            OnBossDefeated?.Invoke();
            EventBus.TriggerFloorComplete(GameManager.Instance != null
                ? GameManager.Instance.Run?.currentFloor ?? 0 : 0);

            // Гарантированный дроп (ТЗ §3 — BossData.guaranteedLoot).
            if (_boss != null && _boss.guaranteedLoot != null)
                LootDropper.Spawn(_boss.guaranteedLoot, transform.position);

            base.Die();
        }

        void Stop()
        {
            Rb.linearVelocity = Vector2.zero;
            SetMoving(Vector2.zero);
        }
    }
}
