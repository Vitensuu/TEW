using UnityEngine;
using Game.Core;

namespace Enemy
{
    /// <summary>
    /// Энергетическая связь между врагами (дизайн Фаза 3 — Corruption Link).
    /// Якорь (Support) усиливает союзника, пока луч жив: лечение, щит
    /// (снижение входящего урона), перенос части урона на якоря, баффы урона/скорости.
    ///
    /// Разрыв связи: смерть якоря/цели, разрыв дистанции, ИЛИ игрок пересекает луч
    /// (триггер-коллайдер вдоль линии). Это и есть тактическая задача — рвать связи.
    ///
    /// Самостоятельный объект: создаётся через <see cref="Create"/>, рисует себя
    /// LineRenderer'ом, при разрыве откатывает все эффекты на цели и самоуничтожается.
    /// Переиспользуется боссом (Phase 7) для link-gated урона.
    /// </summary>
    public class CorruptionLink : MonoBehaviour
    {
        EnemyBase _anchor, _target;
        StateMachineEnemy _targetSm;
        float _heal, _shield, _share, _buffDmg, _buffSpd, _maxRange;
        LineRenderer _lr;
        BoxCollider2D _breakCol;
        bool _broken;

        public bool Alive => !_broken;

        /// <summary>
        /// Создать связь. healPerSecond — лечение цели; shieldFraction (0..1) —
        /// доля поглощаемого уроном; damageShareToAnchor (0..1) — доля урона цели,
        /// перенаправляемая на якорь; buff* — множители урона/скорости цели.
        /// </summary>
        public static CorruptionLink Create(EnemyBase anchor, EnemyBase target,
            float healPerSecond = 0f, float shieldFraction = 0f, float damageShareToAnchor = 0f,
            float buffDamageMult = 1f, float buffSpeedMult = 1f, float maxRange = 12f, Color? color = null)
        {
            if (anchor == null || target == null || anchor == target) return null;

            var go = new GameObject("CorruptionLink");
            var link = go.AddComponent<CorruptionLink>();
            link.Init(anchor, target, healPerSecond, shieldFraction, damageShareToAnchor,
                buffDamageMult, buffSpeedMult, maxRange, color ?? new Color(0.7f, 0.2f, 0.9f));
            return link;
        }

        void Init(EnemyBase anchor, EnemyBase target, float heal, float shield, float share,
            float buffDmg, float buffSpd, float maxRange, Color color)
        {
            _anchor = anchor; _target = target; _targetSm = target as StateMachineEnemy;
            _heal = heal; _shield = shield; _share = share;
            _buffDmg = buffDmg; _buffSpd = buffSpd; _maxRange = maxRange;

            _lr = gameObject.AddComponent<LineRenderer>();
            _lr.material = new Material(Shader.Find("Sprites/Default"));
            _lr.positionCount = 2;
            _lr.startWidth = _lr.endWidth = 0.12f;
            _lr.numCapVertices = 4;
            _lr.startColor = _lr.endColor = color;
            _lr.useWorldSpace = true;
            _lr.sortingOrder = 10;

            _breakCol = gameObject.AddComponent<BoxCollider2D>();
            _breakCol.isTrigger = true;

            ApplyEffects();
        }

        void ApplyEffects()
        {
            if (_shield > 0f || _share > 0f)
            {
                _target.IncomingDamageFilter = (amt, type) =>
                {
                    if (_share > 0f && _anchor != null && _anchor.IsAlive)
                        _anchor.TakeDamage(amt * _share, type);
                    return amt * (1f - _shield);
                };
            }
            if (_buffDmg != 1f) _target.damageDealtMultiplier *= _buffDmg;
            if (_buffSpd != 1f && _targetSm != null) _targetSm.SpeedBuff *= _buffSpd;
        }

        void Update()
        {
            if (_broken) return;
            if (_anchor == null || _target == null || !_anchor.IsAlive || !_target.IsAlive) { Break(); return; }

            Vector3 a = _anchor.transform.position;
            Vector3 b = _target.transform.position;
            if ((a - b).sqrMagnitude > _maxRange * _maxRange) { Break(); return; }

            // Луч.
            _lr.SetPosition(0, a);
            _lr.SetPosition(1, b);

            // Коллайдер-разрыв вдоль луча.
            Vector3 d = b - a;
            float len = d.magnitude;
            transform.position = (a + b) * 0.5f;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            _breakCol.size = new Vector2(len, 0.4f);

            // Лечение цели.
            if (_heal > 0f && _target.IsAlive) _target.Heal(_heal * Time.deltaTime);
        }

        void OnTriggerEnter2D(Collider2D other) { if (other.CompareTag("Player")) Break(); }
        void OnTriggerStay2D(Collider2D other)  { if (other.CompareTag("Player")) Break(); }

        /// <summary>Разорвать связь и откатить эффекты на цели.</summary>
        public void Break()
        {
            if (_broken) return;
            _broken = true;

            if (_target != null)
            {
                _target.IncomingDamageFilter = null;
                if (_buffDmg != 1f) _target.damageDealtMultiplier /= _buffDmg;
                if (_buffSpd != 1f && _targetSm != null) _targetSm.SpeedBuff /= _buffSpd;
            }
            Destroy(gameObject);
        }
    }
}
