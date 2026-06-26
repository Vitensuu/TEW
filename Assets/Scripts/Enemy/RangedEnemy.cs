using UnityEngine;
using Game.Combat;
using Game.Core;

namespace Enemy
{
    /// <summary>
    /// Дальнобойный враг на единой FSM (<see cref="StateMachineEnemy"/>):
    /// Idle (вне детекта) → Attack (кайтинг + стрельба). Держит дистанцию:
    /// слишком близко — отступает, далеко — подходит, в зоне — стоит и стреляет.
    /// Поведение идентично прежней версии.
    /// </summary>
    public class RangedEnemy : StateMachineEnemy
    {
        [Header("Ranged")]
        [SerializeField] float preferredRange = 5f;   // желаемая дистанция стрельбы
        [SerializeField] float tooCloseRange  = 3f;   // ближе — отступаем
        [SerializeField] float shootCooldown  = 1.5f;

        [Header("Снаряд")]
        [SerializeField] GameObject projectilePrefab;
        [SerializeField] float projectileSpeed  = 7f;
        [SerializeField] float projectileDamage  = 6f;
        [SerializeField] LayerMask playerMask;

        float _shootTimer;

        protected override EnemyState DecideState(float dist)
            => dist > detectionRange ? EnemyState.Idle : EnemyState.Attack;

        protected override void OnAttack()
        {
            if (_shootTimer > 0f) _shootTimer -= Time.deltaTime;

            float dist   = DistanceToPlayer;
            Vector2 toP  = DirToPlayer;

            // Кайтинг: слишком близко → отступаем; далеко → приближаемся; в зоне → стоим.
            if (dist < tooCloseRange)      MoveInDirection(-toP, moveSpeed * SpeedMul);
            else if (dist > preferredRange) MoveInDirection(toP, moveSpeed * SpeedMul);
            else                            StopMoving();

            FaceDirection(toP);
            if (_shootTimer <= 0f) Shoot(toP);
        }

        void Shoot(Vector2 dir)
        {
            _shootTimer = shootCooldown;
            if (projectilePrefab == null) return;

            var go = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            var proj = go.GetComponent<Projectile>() ?? go.AddComponent<Projectile>();
            proj.Init(dir, projectileSpeed, projectileDamage, DamageType.Magic, false, playerMask);
        }
    }
}
