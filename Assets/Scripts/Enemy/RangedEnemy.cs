using UnityEngine;
using Game.Combat;
using Game.Core;

namespace Enemy
{
    /// <summary>
    /// Дальнобойный враг (ТЗ §4 — «держат дистанцию, меняют позицию при контакте»).
    /// State Machine: Idle → Chase(approach) → Kite(retreat) → Attack(shoot).
    /// </summary>
    public class RangedEnemy : EnemyBase
    {
        [Header("Ranged AI")]
        [SerializeField] float moveSpeed      = 2.2f;
        [SerializeField] float detectionRange = 8f;
        [SerializeField] float preferredRange = 5f;   // желаемая дистанция стрельбы
        [SerializeField] float tooCloseRange  = 3f;   // ближе — отступаем
        [SerializeField] float shootCooldown  = 1.5f;

        [Header("Снаряд")]
        [SerializeField] GameObject projectilePrefab;
        [SerializeField] float projectileSpeed = 7f;
        [SerializeField] float projectileDamage = 6f;
        [SerializeField] LayerMask playerMask;

        Transform _player;
        StatusEffectHandler _status;
        float _shootTimer;

        protected override void Awake()
        {
            base.Awake();
            _status = GetComponent<StatusEffectHandler>();
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

            if (_shootTimer > 0f) _shootTimer -= Time.deltaTime;

            float dist = Vector2.Distance(transform.position, _player.position);
            float speedMul = _status != null ? _status.SpeedMultiplier : 1f;
            Vector2 toPlayer = ((Vector2)_player.position - (Vector2)transform.position).normalized;

            if (dist > detectionRange) { Stop(); return; }

            // Kiting: слишком близко → отступаем; далеко → приближаемся; в зоне → стоим и стреляем.
            if (dist < tooCloseRange)
            {
                Rb.linearVelocity = -toPlayer * moveSpeed * speedMul;
                SetMoving(-toPlayer);
            }
            else if (dist > preferredRange)
            {
                Rb.linearVelocity = toPlayer * moveSpeed * speedMul;
                SetMoving(toPlayer);
            }
            else
            {
                Stop();
            }

            SetDirection(toPlayer);
            if (_shootTimer <= 0f && dist <= detectionRange)
                Shoot(toPlayer);
        }

        void Shoot(Vector2 dir)
        {
            _shootTimer = shootCooldown;
            if (CanAttack()) { /* анимация атаки */ }

            if (projectilePrefab == null) return;
            var go = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            var proj = go.GetComponent<Projectile>() ?? go.AddComponent<Projectile>();
            proj.Init(dir, projectileSpeed, projectileDamage, DamageType.Magic, false, playerMask);
        }

        void Stop()
        {
            Rb.linearVelocity = Vector2.zero;
            SetMoving(Vector2.zero);
        }
    }
}
