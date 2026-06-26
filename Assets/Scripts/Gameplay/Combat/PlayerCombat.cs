using UnityEngine;
using UnityEngine.InputSystem;
using Game.Core;
using Game.Data;
using Game.Items;
using Game.Player;

namespace Game.Combat
{
    /// <summary>
    /// Боевой контроллер игрока (ТЗ §4 «Система боя», §5 — CombatHandler/AttackHandler).
    /// Ближний бой: OverlapCircle в зоне удара → IDamageable.
    /// Дальний бой: Instantiate(projectile) с паттерном Single/Spread/Piercing/Boomerang.
    /// Урон считается через DamageCalculator с учётом PlayerStats и крита.
    ///
    /// Назначь activeWeapon (или он подтянется из InventoryManager), enemyMask = слой Enemy.
    /// Прицеливание — по курсору мыши. ЛКМ / E — атака.
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Оружие")]
        [SerializeField] WeaponData activeWeapon;     // если пусто — берём из инвентаря
        [SerializeField] Transform  firePoint;        // откуда вылетают снаряды (нос игрока)

        [Header("Цели")]
        [SerializeField] LayerMask enemyMask;
        [SerializeField] float projectileSpeed = 9f;

        [Header("Анимация")]
        [SerializeField] PlayerAnimator animatorBridge;

        PlayerStats _stats;
        float _cooldownTimer;
        Camera _cam;
        StatusEffectHandler _status;
        readonly System.Random _rng = new System.Random();

        public WeaponData ActiveWeapon => activeWeapon;
        public void SetWeapon(WeaponData w) => activeWeapon = w;

        void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            if (animatorBridge == null) animatorBridge = GetComponent<PlayerAnimator>();
            _cam = Camera.main;
            if (firePoint == null) firePoint = transform;
        }

        void Update()
        {
            if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;

            // Stun (ТЗ §4): оглушённый игрок не атакует.
            if (_status == null) _status = GetComponent<StatusEffectHandler>();
            if (_status != null && _status.IsStunned) return;

            // Подтянуть активное оружие из инвентаря, если не задано.
            if (activeWeapon == null && InventoryManager.Instance != null)
                activeWeapon = InventoryManager.Instance.ActiveWeapon;

            bool firePressed =
                (Mouse.current != null && Mouse.current.leftButton.isPressed) ||
                (Keyboard.current != null && Keyboard.current.eKey.isPressed);

            if (firePressed && _cooldownTimer <= 0f)
                Attack();
        }

        void Attack()
        {
            if (activeWeapon == null) return;
            _cooldownTimer = activeWeapon.Cooldown;

            animatorBridge?.TriggerAttack();
            Vector2 aim = AimDirection();

            if (activeWeapon.weaponType == WeaponType.Melee)
                MeleeAttack(aim);
            else
                RangedAttack(aim);
        }

        // ── Ближний бой: OverlapCircle перед игроком ────────────────────────────
        void MeleeAttack(Vector2 aim)
        {
            Vector2 center = (Vector2)transform.position + aim * (activeWeapon.range * 0.5f);
            var hits = Physics2D.OverlapCircleAll(center, activeWeapon.range * 0.6f, enemyMask);

            foreach (var h in hits)
            {
                var dmg = h.GetComponentInParent<IDamageable>();
                if (dmg == null || !dmg.IsAlive) continue;
                ApplyHit(dmg, h.transform.position);

                if (activeWeapon.attackPattern != AttackPattern.Piercing)
                    if (System.Array.IndexOf(hits, h) >= activeWeapon.patternCount) break;
            }
        }

        // ── Дальний бой: снаряды по паттерну ────────────────────────────────────
        void RangedAttack(Vector2 aim)
        {
            if (activeWeapon.projectilePrefab == null)
            {
                Debug.LogWarning($"[PlayerCombat] У оружия {activeWeapon.itemName} нет projectilePrefab.");
                return;
            }

            switch (activeWeapon.attackPattern)
            {
                case AttackPattern.Spread:
                    int n = Mathf.Max(1, activeWeapon.patternCount);
                    float step = n > 1 ? activeWeapon.spreadAngle / (n - 1) : 0f;
                    float start = -activeWeapon.spreadAngle * 0.5f;
                    for (int i = 0; i < n; i++)
                        SpawnProjectile(Rotate(aim, start + step * i), 0, false);
                    break;

                case AttackPattern.Piercing:
                    SpawnProjectile(aim, activeWeapon.patternCount, false);
                    break;

                case AttackPattern.Boomerang:
                    SpawnProjectile(aim, 999, true);
                    break;

                default: // Single
                    SpawnProjectile(aim, 0, false);
                    break;
            }
        }

        void SpawnProjectile(Vector2 dir, int pierce, bool boomerang)
        {
            var go = Instantiate(activeWeapon.projectilePrefab, firePoint.position, Quaternion.identity);
            var proj = go.GetComponent<Projectile>() ?? go.AddComponent<Projectile>();

            var result = DamageCalculator.Compute(
                _stats != null ? _stats.Stats : null,
                activeWeapon.damage, 0f, activeWeapon.damageType, 1f, _rng);

            proj.Init(dir, projectileSpeed, result.amount, activeWeapon.damageType,
                result.isCrit, enemyMask, pierce, boomerang, activeWeapon.range > 1f ? 12f : 12f);
        }

        void ApplyHit(IDamageable target, Vector3 pos)
        {
            var result = DamageCalculator.Compute(
                _stats != null ? _stats.Stats : null,
                activeWeapon.damage, GetTargetDefense(target),
                activeWeapon.damageType, 1f, _rng);

            target.TakeDamage(result.amount, result.type);
            EventBus.TriggerDamageDealt(pos, result.amount, result.isCrit);
        }

        static float GetTargetDefense(IDamageable _) => 0f; // враги MVP без защиты; расширяемо

        // ── Прицеливание ────────────────────────────────────────────────────────
        Vector2 AimDirection()
        {
            if (_cam != null && Mouse.current != null)
            {
                Vector3 m = _cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                Vector2 d = (Vector2)(m - transform.position);
                if (d.sqrMagnitude > 0.001f) return d.normalized;
            }
            return Vector2.right;
        }

        static Vector2 Rotate(Vector2 v, float deg)
        {
            float r = deg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(r), sin = Mathf.Sin(r);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        void OnDrawGizmosSelected()
        {
            if (activeWeapon != null && activeWeapon.weaponType == WeaponType.Melee)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, activeWeapon.range);
            }
        }
    }
}
