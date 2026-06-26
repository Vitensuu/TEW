using UnityEngine;
using Game.Combat;
using Game.Core;
using Game.Data;
using Game.Dungeon;
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

        // Фаза 7: связи с миньонами (link-gated), таймеры живой комнаты.
        readonly System.Collections.Generic.List<CorruptionLink> _links
            = new System.Collections.Generic.List<CorruptionLink>();
        float _linkDamageMult = 1f;
        float _wallTimer, _hazardTimer;

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
            var p = Game.Core.PlayerRef.Resolve();
            if (p != null) _player = p.transform;

            // Фаза 7: фильтр входящего урона — зеркало (свёртка) + link-gated защита.
            IncomingDamageFilter = (amt, type) =>
            {
                if (_phase != null && _phase.mirrorDamage && _player != null)
                {
                    var ph = _player.GetComponent<PlayerHealth>();
                    if (ph != null && ph.IsAlive) ph.TakeDamage(amt * _phase.mirrorFraction);
                }
                return amt * _linkDamageMult;
            };

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

            UpdateLinkGate();      // Фаза 7: пока связи с миньонами целы — урон боссу снижен
            UpdateRoomTactics();   // Фаза 7: стены/опасные зоны живой комнаты

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
                    var prefab = _phase.minionToSummon.spritePrefab;
                    if (prefab == null) break;
                    Vector2 off = Random.insideUnitCircle * 2f;
                    var minion = Instantiate(prefab, transform.position + (Vector3)off, Quaternion.identity);

                    // Link-gated (Фаза 7): босс тянет связь к миньону (лечит его), а сам
                    // получает меньше урона, пока связь цела. Игрок рвёт связь/убивает миньона.
                    if (_phase.linkToMinions)
                    {
                        var meb = minion.GetComponent<EnemyBase>();
                        if (meb != null)
                        {
                            var link = CorruptionLink.Create(this, meb, healPerSecond: 3f);
                            if (link != null) _links.Add(link);
                        }
                    }
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

        // ── Фаза 7: живая комната и link-gated защита ───────────────────────────
        void UpdateLinkGate()
        {
            if (_phase == null || !_phase.linkToMinions) { _linkDamageMult = 1f; return; }
            int alive = 0;
            for (int i = _links.Count - 1; i >= 0; i--)
            {
                if (_links[i] == null || !_links[i].Alive) { _links.RemoveAt(i); continue; }
                alive++;
            }
            _linkDamageMult = alive > 0 ? _phase.linkedDamageTaken : 1f;
        }

        void UpdateRoomTactics()
        {
            if (_phase == null || _player == null) return;

            if (_phase.buildWalls)
            {
                _wallTimer -= Time.deltaTime;
                if (_wallTimer <= 0f)
                {
                    _wallTimer = _phase.wallInterval;
                    Vector2 d = Random.insideUnitCircle.normalized;
                    if (d == Vector2.zero) d = Vector2.right;
                    TempWall.Spawn(_player.position + (Vector3)(d * 2.5f), new Vector2(4f, 0.6f), 4f);
                }
            }

            if (_phase.hazardField)
            {
                _hazardTimer -= Time.deltaTime;
                if (_hazardTimer <= 0f)
                {
                    _hazardTimer = _phase.hazardInterval;
                    HazardZone.SpawnCircle(_player.position + (Vector3)(Random.insideUnitCircle * 2f),
                        1.6f, 3f, 4f, _phase.hazardType, startDelay: 0.4f);
                }
            }
        }

        protected override void Die()
        {
            foreach (var l in _links) if (l != null) l.Break();
            _links.Clear();

            OnBossDefeated?.Invoke();
            EventBus.TriggerFloorComplete(GameManager.Instance != null
                ? GameManager.Instance.Run?.currentFloor ?? 0 : 0);

            // Гарантированный дроп (ТЗ §3 — BossData.guaranteedLoot).
            if (_boss != null && _boss.guaranteedLoot != null)
                LootDropper.Spawn(_boss.guaranteedLoot, transform.position);

            // Финальный босс → Victory (ТЗ §6). Обычный босс просто завершает этаж.
            if (_boss != null && _boss.isFinalBoss)
                EventBus.TriggerVictory();

            base.Die();
        }

        void Stop()
        {
            Rb.linearVelocity = Vector2.zero;
            SetMoving(Vector2.zero);
        }
    }
}
