using UnityEngine;
using Game.Core;
using Game.Data;

namespace Game.Combat
{
    /// <summary>
    /// Снаряд дальнего боя (ТЗ §4 — «Дальний бой: Instantiate, Physics2D, OnTriggerEnter2D»).
    /// Повесь на префаб снаряда: Rigidbody2D (Kinematic), Collider2D (IsTrigger).
    /// Конфигурируется через Init(). Бьёт только цели слоя targetMask.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] float lifeTime = 4f;
        [SerializeField] GameObject hitVfx;

        float _damage;
        DamageType _type;
        bool _crit;
        LayerMask _targetMask;
        int _pierceLeft;
        bool _boomerang;
        Vector2 _origin;
        float _maxRange;
        Rigidbody2D _rb;

        public void Init(Vector2 direction, float speed, float damage, DamageType type,
            bool crit, LayerMask targetMask, int pierce = 0, bool boomerang = false,
            float maxRange = 12f)
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _damage = damage;
            _type = type;
            _crit = crit;
            _targetMask = targetMask;
            _pierceLeft = pierce;
            _boomerang = boomerang;
            _origin = transform.position;
            _maxRange = maxRange;

            _rb.linearVelocity = direction.normalized * speed;

            float ang = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, ang);

            Destroy(gameObject, lifeTime);
        }

        void FixedUpdate()
        {
            if (!_boomerang) return;

            Vector2 fromOrigin = (Vector2)transform.position - _origin;
            if (fromOrigin.sqrMagnitude > _maxRange * _maxRange)
            {
                // Разворот к точке запуска (упрощённый бумеранг).
                Vector2 back = (_origin - (Vector2)transform.position).normalized;
                _rb.linearVelocity = back * _rb.linearVelocity.magnitude;
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (((1 << other.gameObject.layer) & _targetMask) == 0) return;

            var dmg = other.GetComponentInParent<IDamageable>();
            if (dmg != null && dmg.IsAlive)
            {
                dmg.TakeDamage(_damage, _type);
                EventBus.TriggerDamageDealt(transform.position, _damage, _crit);
                if (hitVfx != null) Instantiate(hitVfx, transform.position, Quaternion.identity);

                if (_pierceLeft > 0) { _pierceLeft--; return; }
                if (!_boomerang) Destroy(gameObject);
            }
            else if (other.CompareTag("Wall") || other.gameObject.layer == LayerMask.NameToLayer("Wall"))
            {
                if (!_boomerang) Destroy(gameObject);
            }
        }
    }
}
