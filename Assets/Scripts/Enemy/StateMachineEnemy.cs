using UnityEngine;
using Game.Combat;
using Game.Data;

namespace Enemy
{
    /// <summary>
    /// Канонический 6-состоянийный конечный автомат поверх <see cref="EnemyBase"/>
    /// (дизайн «Перестройка»: Idle / Patrol / Chase / Attack / Special / Dead).
    /// EnemyBase НЕ переписывается — этот класс лишь стандартизирует словарь
    /// состояний и общие хелперы (поиск игрока, дистанция, движение, стан,
    /// замедление), на которые опираются все AI-наследники. Анимации не трогаются:
    /// движение/направление по-прежнему идут через SetMoving/SetDirection базы.
    ///
    /// <see cref="EnemyState.Special"/> — единая точка подключения будущих слоёв
    /// (роли, элитные модификаторы, corruption link, эволюция, манипуляция комнатой).
    ///
    /// Наследник реализует <see cref="DecideState"/> (переходы по дистанции) и
    /// нужные обработчики OnIdle/OnChase/OnAttack/OnSpecial. Для таймированных
    /// последовательностей (рывок, призыв), которые сами владеют переходами,
    /// поднимается <see cref="LockState"/> — тогда база не вызывает DecideState и
    /// просто крутит обработчик текущего состояния.
    /// </summary>
    public abstract class StateMachineEnemy : EnemyBase
    {
        public enum EnemyState { Idle, Patrol, Chase, Attack, Special, Dead }

        [Header("Движение / детект")]
        [SerializeField] protected float moveSpeed     = 2.5f;
        [SerializeField] protected float detectionRange = 12f;

        public EnemyState State { get; private set; } = EnemyState.Idle;
        public System.Action<EnemyState> OnStateChanged;

        /// <summary>Роли врага (из EnemyData) — для синергий/слоёв.</summary>
        public EnemyRole Roles => data != null ? data.roles : EnemyRole.None;

        protected Transform Player;
        protected StatusEffectHandler Status;

        /// <summary>Цель (игрок) для слоёв-модификаторов. Может быть null до Start.</summary>
        public Transform Target => Player;

        /// <summary>Множитель скорости от слоёв (Berserker ×2 и т.п.).</summary>
        public float SpeedBuff { get; set; } = 1f;

        /// <summary>
        /// Пока true — база не пересчитывает состояние (DecideState пропускается),
        /// а лишь вызывает обработчик текущего. Таймированная последовательность
        /// сама сбрасывает флаг и переключает состояние по завершении.
        /// </summary>
        protected bool LockState;

        protected override void Awake()
        {
            base.Awake();
            Status = GetComponent<StatusEffectHandler>();
        }

        protected override void ApplyData()
        {
            base.ApplyData();
            if (data != null)
            {
                moveSpeed      = data.speed;
                detectionRange = data.detectionRange;
            }
        }

        protected virtual void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) Player = p.transform;
        }

        protected override void Update()
        {
            base.Update();
            if (IsDead) { SetState(EnemyState.Dead); return; }

            // Страховка: если игрок не был найден в Start (порядок спавна/инициализации) —
            // ищем лениво, пока не появится.
            if (Player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p == null) return;
                Player = p.transform;
            }

            // Stun (ТЗ §4): полная остановка, переходы заморожены.
            if (Stunned) { OnStunned(); return; }

            if (!LockState)
            {
                var next = DecideState(DistanceToPlayer);
                if (next != State) SetState(next);
            }

            switch (State)
            {
                case EnemyState.Idle:    OnIdle();    break;
                case EnemyState.Patrol:  OnPatrol();  break;
                case EnemyState.Chase:   OnChase();   break;
                case EnemyState.Attack:  OnAttack();  break;
                case EnemyState.Special: OnSpecial(); break;
            }
        }

        protected void SetState(EnemyState s)
        {
            if (State == s) return;
            State = s;
            OnEnterState(s);
            OnStateChanged?.Invoke(s);
        }

        // ── Хуки для наследников ────────────────────────────────────────────────
        /// <summary>Выбор состояния по дистанции до игрока (вызывается каждый кадр,
        /// если не поднят LockState). Верни текущее State, чтобы остаться.</summary>
        protected abstract EnemyState DecideState(float distanceToPlayer);

        protected virtual void OnEnterState(EnemyState s) { }
        protected virtual void OnIdle()    { StopMoving(); }
        protected virtual void OnPatrol()  { OnIdle(); }
        protected virtual void OnChase()   { }
        protected virtual void OnAttack()  { }
        protected virtual void OnSpecial() { }
        protected virtual void OnStunned() { StopMoving(); }

        // ── Хелперы ─────────────────────────────────────────────────────────────
        protected float DistanceToPlayer =>
            Player != null ? Vector2.Distance(transform.position, Player.position) : float.MaxValue;

        protected Vector2 DirToPlayer =>
            Player != null
                ? ((Vector2)Player.position - (Vector2)transform.position).normalized
                : Vector2.zero;

        /// <summary>Множитель скорости: Freeze (ТЗ §4) × бафф модификаторов.</summary>
        protected float SpeedMul => (Status != null ? Status.SpeedMultiplier : 1f) * SpeedBuff;
        protected bool  Stunned  => Status != null && Status.IsStunned;

        protected void MoveInDirection(Vector2 dir, float speed)
        {
            Rb.linearVelocity = dir * speed;
            SetMoving(dir);
        }

        protected void StopMoving()
        {
            Rb.linearVelocity = Vector2.zero;
            SetMoving(Vector2.zero);
        }

        protected void FaceDirection(Vector2 dir) => SetDirection(dir);
    }
}
