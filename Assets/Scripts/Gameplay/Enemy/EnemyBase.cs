using UnityEngine;
using UnityEngine.Events;
using Game.Core;
using Game.Data;

namespace Enemy
{
    /// <summary>
    /// Базовый класс для всех врагов (ТЗ §5 — EnemyBase, Template Method).
    /// Хранит HP, принимает урон (IDamageable), управляет анимацией и смертью,
    /// при гибели начисляет золото/EXP и дропает лут (LootDropper), шлёт EventBus.
    ///
    /// Animator Controller должен иметь параметры:
    ///   isMoving  (Bool)    — враг движется
    ///   isAttacking (Trigger) — начало атаки
    ///   isDead    (Trigger) — смерть
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class EnemyBase : MonoBehaviour, IDamageable, IEnemy
    {
        [Header("Данные (опционально — переопределяют поля ниже)")]
        [SerializeField] protected EnemyData data;

        [Header("Характеристики")]
        [SerializeField] protected float maxHp      = 30f;
        [SerializeField] protected float attackDamage = 5f;
        [SerializeField] protected float attackRange  = 0.8f;
        [SerializeField] protected float attackCooldown = 1.2f;

        [Tooltip("Длительность замаха перед ударом (сек). Враг подсвечивается красным, " +
                 "урон наносится только в КОНЦЕ замаха — это окно, чтобы игрок успел увернуться. " +
                 "0 = мгновенный удар (старое поведение, увернуться нельзя).")]
        [SerializeField] protected float attackWindup = 0.45f;

        // Animator параметры
        static readonly int AnimIsMoving      = Animator.StringToHash("isMoving");
        static readonly int AnimIsAttacking   = Animator.StringToHash("isAttacking"); // Bool
        static readonly int AnimAttackTrigger = Animator.StringToHash("attackTrigger"); // Trigger
        static readonly int AnimIsDead        = Animator.StringToHash("isDead");
        protected static readonly int AnimDirX = Animator.StringToHash("dirX");
        protected static readonly int AnimDirY = Animator.StringToHash("dirY");

        public UnityEvent<float> OnHpChanged; // 0..1 normalized

        // IDamageable: (current, max)
        public event System.Action<float, float> OnHealthChanged;
        /// <summary>Смерть врага (Фаза 6): для предсмертных эффектов компонентов (PlagueBearer и т.п.).</summary>
        public event System.Action Died;
        public bool IsAlive => !IsDead;

        protected float          Hp;
        protected bool           IsDead;
        protected float          AttackTimer;

        [Tooltip("Sorting Order спрайта врага. Должен быть выше декора комнаты (1–3), " +
                 "иначе враг рендерится ПОД ассетами комнаты и не виден.")]
        [SerializeField] protected int spriteSortingOrder = 6;

        /// <summary>Sorting order спрайта врага — для UI поверх него (HP-бар).</summary>
        public int SpriteSortingOrder => spriteSortingOrder;

        protected Rigidbody2D    Rb;
        protected Animator       Anim;
        protected SpriteRenderer Sr;

        // ── Элитные модификаторы (Фаза 2) ───────────────────────────────────────
        readonly System.Collections.Generic.List<EliteModifier> _modifiers
            = new System.Collections.Generic.List<EliteModifier>();

        /// <summary>Множитель наносимого урона (Berserker и т.п.).</summary>
        [System.NonSerialized] public float damageDealtMultiplier = 1f;

        /// <summary>
        /// Фильтр входящего урона (CorruptionLink: щит/перенос на якорь). Принимает
        /// (урон, тип), возвращает урон, который реально применить. null — нет фильтра.
        /// </summary>
        [System.NonSerialized] public System.Func<float, DamageType, float> IncomingDamageFilter;

        /// <summary>Модификатор саморегистрируется здесь при инициализации.</summary>
        public void RegisterModifier(EliteModifier m)
        {
            if (m != null && !_modifiers.Contains(m)) _modifiers.Add(m);
        }

        /// <summary>Оповестить модификаторы о нанесённом уроне (Toxic/Vampiric).</summary>
        protected void NotifyDealtDamage(Game.Core.IDamageable target, float amount)
        {
            for (int i = 0; i < _modifiers.Count; i++) _modifiers[i].OnDealtDamage(target, amount);
        }

        protected virtual void Awake()
        {
            Rb   = GetComponent<Rigidbody2D>();
            Anim = GetComponent<Animator>();
            Sr   = GetComponent<SpriteRenderer>();

            // Без этого враг рендерится под спрайтами комнаты (декор имеет order 1–3)
            // и его не видно — он бьёт игрока «из-под пола».
            if (Sr != null) Sr.sortingOrder = spriteSortingOrder;

            Rb.gravityScale = 0f;
            Rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

            // Коллайдер НЕ создаём автоматически — он настраивается на префабе вручную.
            // (Для попаданий игрока/HazardZone/связей у врага должен быть Collider2D.)

            ApplyData();
            Hp = maxHp;

            // Плавающая полоска HP над врагом (видно, что урон проходит). Авто-добавляем,
            // чтобы не править каждый префаб вручную.
            if (GetComponent<Game.UI.EnemyHealthBar>() == null)
                gameObject.AddComponent<Game.UI.EnemyHealthBar>();
        }

        /// <summary>Подтянуть характеристики из EnemyData, если назначен.</summary>
        protected virtual void ApplyData()
        {
            if (data == null) return;
            maxHp          = data.maxHealth;
            attackDamage   = data.damage;
            attackRange    = data.attackRange;
            attackCooldown = data.attackCooldown;
        }

        public EnemyData Data => data;

        protected virtual void Update()
        {
            if (IsDead) return;
            if (AttackTimer > 0f) AttackTimer -= Time.deltaTime;
        }

        // ── Урон / смерть ────────────────────────────────────────────────────

        // IDamageable
        public virtual void TakeDamage(float amount, DamageType type = DamageType.Physical)
        {
            if (IsDead) return;
            for (int i = 0; i < _modifiers.Count; i++) amount = _modifiers[i].ModifyIncomingDamage(amount);
            if (IncomingDamageFilter != null) amount = IncomingDamageFilter(amount, type);
            Hp = Mathf.Max(0f, Hp - amount);
            OnHpChanged?.Invoke(Hp / maxHp);
            OnHealthChanged?.Invoke(Hp, maxHp);
            if (Hp <= 0f) Die();
        }

        // Совместимость со старым кодом (одно-аргументный вызов).
        public void TakeDamage(float amount) => TakeDamage(amount, DamageType.Physical);

        public virtual void Heal(float amount)
        {
            if (IsDead) return;
            Hp = Mathf.Min(maxHp, Hp + amount);
            OnHpChanged?.Invoke(Hp / maxHp);
            OnHealthChanged?.Invoke(Hp, maxHp);
        }

        /// <summary>
        /// Изменить максимальное HP при эволюции (Фаза 5): множитель maxHp с
        /// опциональным долечиванием. Урон/скорость эволюция меняет через
        /// damageDealtMultiplier / SpeedBuff. Анимации не трогаются.
        /// </summary>
        public void EvolveStats(float maxHpMult, bool healToFull)
        {
            float ratio = maxHp > 0f ? Hp / maxHp : 1f;
            maxHp = Mathf.Max(1f, maxHp * maxHpMult);
            Hp = healToFull ? maxHp : Mathf.Clamp(maxHp * ratio, 1f, maxHp);
            OnHpChanged?.Invoke(Hp / maxHp);
            OnHealthChanged?.Invoke(Hp, maxHp);
        }

        protected virtual void Die()
        {
            IsDead = true;
            for (int i = 0; i < _modifiers.Count; i++) _modifiers[i].OnHostDeath();
            Died?.Invoke();
            Rb.linearVelocity = Vector2.zero;
            Anim.SetTrigger(AnimIsDead);
            // Физику отключаем чтобы труп не мешал
            var deadCol = GetComponent<Collider2D>();
            if (deadCol != null) deadCol.enabled = false;

            GrantRewardsAndLoot();
            EventBus.TriggerEnemyKilled(this);

            // Уничтожаем объект после анимации смерти (~1.5 сек)
            Destroy(gameObject, 1.5f);
        }

        /// <summary>Награды (золото/EXP) + дроп лута по таблице (ТЗ §4).</summary>
        protected virtual void GrantRewardsAndLoot()
        {
            if (data != null)
            {
                var run = GameManager.Instance != null ? GameManager.Instance.Run : null;
                if (run != null)
                {
                    run.gold += data.goldReward;
                    run.enemiesKilled++;
                    EventBus.TriggerGoldChanged(run.gold);
                }
                Game.Items.LootDropper.DropLoot(data.lootTable, transform.position);
            }
        }

        // ── Анимация движения ─────────────────────────────────────────────────

        protected void SetMoving(Vector2 velocity)
        {
            bool moving = velocity.sqrMagnitude > 0.01f;
            Anim.SetBool(AnimIsMoving, moving);

            if (moving)
                SetDirection(velocity);
        }

        protected void SetDirection(Vector2 dir)
        {
            // Определяем доминирующую ось для 4-направленной анимации
            if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            {
                Anim.SetFloat(AnimDirX, dir.x > 0f ? 1f : -1f);
                Anim.SetFloat(AnimDirY, 0f);
            }
            else
            {
                Anim.SetFloat(AnimDirX, 0f);
                Anim.SetFloat(AnimDirY, dir.y > 0f ? 1f : -1f);
            }
        }

        // ── Атака ─────────────────────────────────────────────────────────────

        protected bool CanAttack() => !IsDead && AttackTimer <= 0f;

        protected virtual void PerformAttack(Transform target)
        {
            if (!CanAttack()) return;
            AttackTimer = attackCooldown;
            StartCoroutine(AttackRoutine(target));
        }

        /// <summary>
        /// Атака с замахом-телеграфом: сначала видимое предупреждение (анимация замаха +
        /// подсветка спрайта), и только в КОНЦЕ замаха проверяется дистанция и наносится
        /// урон. Если игрок за время замаха вышел из attackRange — удар проходит мимо.
        /// Так удар становится виден и от него можно увернуться (attackWindup = 0 —
        /// мгновенный удар, как раньше).
        /// </summary>
        System.Collections.IEnumerator AttackRoutine(Transform target)
        {
            // Включаем Bool isAttacking → анимация атаки/замаха
            Anim.SetBool(AnimIsAttacking, true);
            if (HasAnimParam(AnimAttackTrigger)) Anim.SetTrigger(AnimAttackTrigger);

            Color baseColor = Sr != null ? Sr.color : Color.white;

            // Фаза замаха: пульсирующая красная подсветка — визуальный телеграф удара.
            float t = 0f;
            while (t < attackWindup)
            {
                if (IsDead) { if (Sr != null) Sr.color = baseColor; yield break; }
                if (Sr != null)
                    Sr.color = Color.Lerp(baseColor, Color.red, Mathf.PingPong(t * 6f, 1f));
                t += Time.deltaTime;
                yield return null;
            }
            if (Sr != null) Sr.color = baseColor;

            // Урон в конце замаха. ApplyAttackDamage сам проверит дистанцию —
            // если игрок увернулся (вышел за attackRange), удар пройдёт мимо.
            if (!IsDead) ApplyAttackDamage(target);

            // Сбрасываем isAttacking ближе к концу кулдауна → анимация вернётся в Idle/Walk
            float rest = Mathf.Max(0f, attackCooldown * 0.8f - attackWindup);
            if (rest > 0f) yield return new WaitForSeconds(rest);
            if (!IsDead) Anim.SetBool(AnimIsAttacking, false);
        }

        bool HasAnimParam(int hash)
        {
            foreach (var p in Anim.parameters)
                if (p.nameHash == hash) return true;
            return false;
        }

        // Вызывается как Animation Event из клипа атаки
        protected virtual void OnAttackHit()
        {
            // Переопределяется в наследниках если нужна задержка через Animation Event
        }

        protected void ApplyAttackDamage(Transform target)
        {
            if (target == null) return;
            float dist = Vector2.Distance(transform.position, target.position);
            if (dist > attackRange) return;

            var ph = target.GetComponent<PlayerHealth>();
            if (ph == null)
            {
                Debug.LogWarning("[EnemyBase] PlayerHealth не найден на " + target.name +
                    " — добавь компонент PlayerHealth на объект игрока!");
                return;
            }
            float dmg = attackDamage * damageDealtMultiplier;
            ph.TakeDamage(dmg);
            NotifyDealtDamage(ph, dmg);
        }

        // ── Публичные свойства ─────────────────────────────────────────────────

        public float MaxHp        => maxHp;
        public float CurrentHp    => Hp;
        public bool  Dead         => IsDead;
        public float AttackDmg    => attackDamage;
        public float AttackRangeVal => attackRange;
    }
}
