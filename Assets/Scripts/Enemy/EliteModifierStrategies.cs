using UnityEngine;
using Game.Core;
using Game.Combat;

namespace Enemy
{
    /// <summary>
    /// Базовая стратегия элитного модификатора (плоский класс, не MonoBehaviour —
    /// поэтому без ограничения «файл = имя класса»; ими владеет <see cref="EliteModifier"/>).
    /// Хуки: OnSpawn (разовая настройка/баффы), Tick (периодика), ModifyIncoming
    /// (входящий урон), OnDealtDamage (исходящий урон), OnDeath. Tint — цвет спрайта.
    /// </summary>
    public abstract class EliteStrategy
    {
        protected EnemyBase host;
        protected StateMachineEnemy sm;

        public virtual Color? Tint => null;

        public virtual void OnSpawn(EnemyBase h) { host = h; sm = h as StateMachineEnemy; }
        public virtual void Tick(float dt) { }
        public virtual float ModifyIncoming(float amount) => amount;
        public virtual void OnDealtDamage(IDamageable target, float amount) { }
        public virtual void OnDeath() { }

        protected Vector3 Pos => host != null ? host.transform.position : Vector3.zero;

        protected Transform Player()
        {
            if (sm != null && sm.Target != null) return sm.Target;
            var p = GameObject.FindGameObjectWithTag("Player");
            return p != null ? p.transform : null;
        }
    }

    public static class EliteStrategyFactory
    {
        public static EliteStrategy Create(EliteModifierType t) => t switch
        {
            EliteModifierType.Berserker   => new BerserkerStrategy(),
            EliteModifierType.VoidTouched => new VoidTouchedStrategy(),
            EliteModifierType.Arcane      => new ArcaneStrategy(),
            EliteModifierType.Toxic       => new ToxicStrategy(),
            EliteModifierType.Frozen      => new FrozenStrategy(),
            EliteModifierType.Mirror      => new MirrorStrategy(),
            EliteModifierType.Temporal    => new TemporalStrategy(),
            EliteModifierType.Gravity     => new GravityStrategy(),
            EliteModifierType.Vampiric    => new VampiricStrategy(),
            EliteModifierType.Explosive   => new ExplosiveStrategy(),
            _                             => new BerserkerStrategy(),
        };
    }

    // ① <50% HP — урон/скорость ×2, ярость (необратимо).
    public class BerserkerStrategy : EliteStrategy
    {
        bool _enraged;
        public override Color? Tint => new Color(0.85f, 0.25f, 0.22f);
        public override void Tick(float dt)
        {
            if (_enraged || host == null) return;
            if (host.CurrentHp / Mathf.Max(1f, host.MaxHp) <= 0.5f)
            {
                _enraged = true;
                host.damageDealtMultiplier *= 2f;
                if (sm != null) sm.SpeedBuff *= 2f;
            }
        }
    }

    // ② Оставляет Void-зоны на пути; смерть → большая провал-зона.
    public class VoidTouchedStrategy : EliteStrategy
    {
        float _t;
        public override Color? Tint => new Color(0.5f, 0.2f, 0.7f);
        public override void Tick(float dt)
        {
            _t -= dt;
            if (_t <= 0f)
            {
                _t = 0.6f;
                HazardZone.SpawnCircle(Pos, 0.7f, 2.5f, 2f, DamageType.Magic);
            }
        }
        public override void OnDeath()
            => HazardZone.SpawnCircle(Pos, 2.2f, 4f, 4f, DamageType.Magic);
    }

    // ③ Периодический блинк за спину игрока.
    public class ArcaneStrategy : EliteStrategy
    {
        float _t = 3f;
        public override Color? Tint => new Color(0.3f, 0.5f, 0.9f);
        public override void Tick(float dt)
        {
            _t -= dt;
            if (_t > 0f || host == null) return;
            _t = 3f;
            var p = Player();
            if (p == null) return;
            Vector3 dir = p.position - host.transform.position;
            if (dir.sqrMagnitude < 0.01f) return;
            host.transform.position = p.position + dir.normalized * 1.5f; // появиться за игроком
        }
    }

    // ④ Атаки травят; труп — ядовитое облако.
    public class ToxicStrategy : EliteStrategy
    {
        public override Color? Tint => new Color(0.4f, 0.7f, 0.2f);
        public override void OnDealtDamage(IDamageable target, float amount)
        {
            var c = target as Component;
            if (c == null) return;
            var seh = c.GetComponentInParent<StatusEffectHandler>();
            if (seh != null) seh.Apply(StatusEffect.Poison, 3f, 2f);
        }
        public override void OnDeath()
            => HazardZone.SpawnCircle(Pos, 2f, 4f, 2f, DamageType.Poison, StatusEffect.Poison, 2f, 2f);
    }

    // ⑤ Аура Freeze в радиусе (на цели со StatusEffectHandler).
    public class FrozenStrategy : EliteStrategy
    {
        float _t;
        public override Color? Tint => new Color(0.5f, 0.8f, 0.95f);
        public override void Tick(float dt)
        {
            _t -= dt;
            if (_t > 0f) return;
            _t = 0.5f;
            var p = Player();
            if (p == null) return;
            if (Vector2.Distance(Pos, p.position) <= 3.5f)
            {
                var seh = p.GetComponent<StatusEffectHandler>();
                if (seh != null) seh.Apply(StatusEffect.Freeze, 0.8f, 0.5f);
            }
        }
    }

    // ⑥ Отражает часть получаемого урона обратно игроку (thorns/зеркало).
    public class MirrorStrategy : EliteStrategy
    {
        public override Color? Tint => new Color(0.85f, 0.85f, 0.9f);
        public override float ModifyIncoming(float amount)
        {
            var p = Player();
            if (p != null)
            {
                var ph = p.GetComponent<PlayerHealth>();
                if (ph != null && ph.IsAlive) ph.TakeDamage(amount * 0.4f);
            }
            return amount;
        }
    }

    // ⑦ Периодический откат HP/позиции на ~2.5 сек назад.
    public class TemporalStrategy : EliteStrategy
    {
        float _t = 5f, _snapHp;
        Vector3 _snapPos;
        bool _has;
        public override Color? Tint => new Color(0.2f, 0.7f, 0.7f);
        public override void Tick(float dt)
        {
            if (host == null) return;
            _t -= dt;
            if (_t <= 2.5f && !_has) { _snapPos = Pos; _snapHp = host.CurrentHp; _has = true; }
            if (_t <= 0f)
            {
                _t = 5f;
                if (_has)
                {
                    host.transform.position = _snapPos;
                    if (host.CurrentHp < _snapHp) host.Heal(_snapHp - host.CurrentHp);
                }
                _has = false;
            }
        }
    }

    // ⑧ Тянет игрока к себе (гравитация).
    public class GravityStrategy : EliteStrategy
    {
        public override Color? Tint => new Color(0.35f, 0.2f, 0.5f);
        public override void Tick(float dt)
        {
            var p = Player();
            if (p == null) return;
            Vector2 toHost = (Vector2)Pos - (Vector2)p.position;
            float d = toHost.magnitude;
            if (d > 0.3f && d <= 5f)
            {
                var rb = p.GetComponent<Rigidbody2D>();
                if (rb != null) rb.position += toHost.normalized * 2.5f * dt;
            }
        }
    }

    // ⑨ Лечится от нанесённого урона.
    public class VampiricStrategy : EliteStrategy
    {
        public override Color? Tint => new Color(0.6f, 0.1f, 0.15f);
        public override void OnDealtDamage(IDamageable target, float amount)
        {
            if (host != null && host.IsAlive) host.Heal(amount * 0.5f);
        }
    }

    // ⑩ Смерть → взрыв по площади с отбросом.
    public class ExplosiveStrategy : EliteStrategy
    {
        public override Color? Tint => new Color(0.95f, 0.5f, 0.1f);
        public override void OnDeath()
        {
            const float radius = 3f, dmg = 15f;
            var hits = Physics2D.OverlapCircleAll(Pos, radius);
            foreach (var h in hits)
            {
                if (!h.CompareTag("Player")) continue;
                var ph = h.GetComponentInParent<PlayerHealth>();
                if (ph != null && ph.IsAlive) ph.TakeDamage(dmg);
                var rb = h.attachedRigidbody;
                if (rb != null)
                {
                    Vector2 kb = ((Vector2)h.transform.position - (Vector2)Pos).normalized;
                    rb.linearVelocity = kb * 10f;
                }
            }
        }
    }
}
