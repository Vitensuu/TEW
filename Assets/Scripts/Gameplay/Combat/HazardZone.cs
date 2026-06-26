using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.Data;

namespace Game.Combat
{
    /// <summary>
    /// Опасная зона «живой комнаты» (дизайн Фаза 1): триггер-область, которая
    /// периодически наносит урон и/или накладывает статус-эффект тем, кто внутри.
    /// Базовый инструмент для ролей Controller (лёд/огонь), Tank/Disruptor
    /// (ядовитое облако), модификатора Gravity, ловушек и фаз босса.
    ///
    /// Используется двумя путями:
    ///   • как компонент на префабе-VFX (дизайнер задаёт поля в инспекторе);
    ///   • программно из врага через <see cref="SpawnCircle"/>/<see cref="SpawnBox"/>.
    ///
    /// Урон идёт по IDamageable (работает на игроке и врагах), статус — по
    /// StatusEffectHandler, если он есть на цели. Кого задевать — по тегу
    /// (по умолчанию "Player") или по маске слоёв.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class HazardZone : MonoBehaviour
    {
        [Header("Тайминги")]
        [Tooltip("Время жизни зоны, сек (<=0 — бессрочно, пока не уничтожат)")]
        [SerializeField] float lifetime = 4f;
        [SerializeField] float tickInterval = 0.5f;
        [Tooltip("Задержка активации (телеграф опасности), сек")]
        [SerializeField] float startDelay = 0f;

        [Header("Эффект")]
        [SerializeField] float damagePerTick = 4f;
        [SerializeField] DamageType damageType = DamageType.Magic;
        [SerializeField] bool applyStatus = false;
        [SerializeField] StatusEffect status = StatusEffect.Freeze;
        [SerializeField] float statusDuration = 1f;
        [SerializeField] float statusMagnitude = 0.5f;

        [Header("Цель")]
        [Tooltip("Фильтр по тегу (true) или по маске слоёв (false)")]
        [SerializeField] bool useTag = true;
        [SerializeField] string targetTag = "Player";
        [SerializeField] LayerMask targetMask;

        readonly List<Collider2D> _inside = new List<Collider2D>();
        float _tickTimer, _life, _delay;
        bool  _active;

        void Awake()
        {
            foreach (var c in GetComponents<Collider2D>()) c.isTrigger = true;
        }

        // Инициализация в Start: фабрика выставляет поля сразу после AddComponent,
        // т.е. ДО Start, но ПОСЛЕ Awake — поэтому кэш таймеров берём здесь.
        void Start()
        {
            _life   = lifetime;
            _delay  = startDelay;
            _active = startDelay <= 0f;
        }

        void Update()
        {
            float dt = Time.deltaTime;

            if (!_active)
            {
                _delay -= dt;
                if (_delay <= 0f) _active = true;
                else return;
            }

            _tickTimer -= dt;
            if (_tickTimer <= 0f) { _tickTimer = tickInterval; Tick(); }

            if (lifetime > 0f)
            {
                _life -= dt;
                if (_life <= 0f) Destroy(gameObject);
            }
        }

        void Tick()
        {
            for (int i = _inside.Count - 1; i >= 0; i--)
            {
                var c = _inside[i];
                if (c == null) { _inside.RemoveAt(i); continue; }
                Affect(c);
            }
        }

        void Affect(Collider2D c)
        {
            if (damagePerTick > 0f)
            {
                var dmg = c.GetComponentInParent<IDamageable>();
                if (dmg != null && dmg.IsAlive) dmg.TakeDamage(damagePerTick, damageType);
            }
            if (applyStatus)
            {
                var seh = c.GetComponentInParent<StatusEffectHandler>();
                if (seh != null) seh.Apply(status, statusDuration, statusMagnitude);
            }
        }

        bool Matches(Collider2D c)
            => useTag ? c.CompareTag(targetTag)
                      : ((1 << c.gameObject.layer) & targetMask.value) != 0;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (Matches(other) && !_inside.Contains(other)) _inside.Add(other);
        }

        void OnTriggerExit2D(Collider2D other) => _inside.Remove(other);

        // ── Фабрики ──────────────────────────────────────────────────────────────
        public static HazardZone SpawnCircle(Vector3 pos, float radius, float lifetime,
            float damagePerTick, DamageType type,
            StatusEffect? status = null, float statusDuration = 0f, float statusMagnitude = 0f,
            string targetTag = "Player", float tickInterval = 0.5f, float startDelay = 0f)
        {
            var go = new GameObject("HazardZone");
            go.transform.position = pos;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = radius;
            col.isTrigger = true;
            return Configure(go, lifetime, damagePerTick, type, status,
                statusDuration, statusMagnitude, targetTag, tickInterval, startDelay);
        }

        public static HazardZone SpawnBox(Vector3 pos, Vector2 size, float lifetime,
            float damagePerTick, DamageType type,
            StatusEffect? status = null, float statusDuration = 0f, float statusMagnitude = 0f,
            string targetTag = "Player", float tickInterval = 0.5f, float startDelay = 0f)
        {
            var go = new GameObject("HazardZone");
            go.transform.position = pos;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            col.isTrigger = true;
            return Configure(go, lifetime, damagePerTick, type, status,
                statusDuration, statusMagnitude, targetTag, tickInterval, startDelay);
        }

        static HazardZone Configure(GameObject go, float lifetime, float damagePerTick,
            DamageType type, StatusEffect? status, float statusDuration, float statusMagnitude,
            string targetTag, float tickInterval, float startDelay)
        {
            var hz = go.AddComponent<HazardZone>();
            hz.lifetime        = lifetime;
            hz.tickInterval    = tickInterval;
            hz.startDelay      = startDelay;
            hz.damagePerTick   = damagePerTick;
            hz.damageType      = type;
            hz.useTag          = true;
            hz.targetTag       = targetTag;
            if (status.HasValue)
            {
                hz.applyStatus     = true;
                hz.status          = status.Value;
                hz.statusDuration  = statusDuration;
                hz.statusMagnitude = statusMagnitude;
            }
            return hz;
        }
    }
}
