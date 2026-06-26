using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// Оружие (ТЗ §3 — WeaponData). projectilePrefab задаётся только для Ranged/Magic.
    /// </summary>
    [CreateAssetMenu(menuName = "TEW/Items/Weapon", fileName = "Weapon_")]
    public class WeaponData : ItemData
    {
        [Header("Оружие")]
        public float damage      = 10f;
        public float attackSpeed = 1f;     // ударов в секунду
        public float range       = 1.2f;   // дистанция (melee) / для дальнего — авто

        public WeaponType    weaponType    = WeaponType.Melee;
        public AttackPattern attackPattern = AttackPattern.Single;
        public Game.Data.DamageType damageType = Game.Data.DamageType.Physical;

        [Tooltip("Только для Ranged/Magic — снаряд")]
        public GameObject projectilePrefab;

        [Tooltip("Для Spread — число снарядов, для Piercing — макс. пробитий")]
        public int patternCount = 3;
        [Tooltip("Для Spread — суммарный угол разброса, градусы")]
        public float spreadAngle = 30f;

        public ItemTier tier = ItemTier.Common;

        public override Rarity Rarity => (Rarity)(int)tier;

        public float Cooldown => attackSpeed <= 0f ? 0.25f : 1f / attackSpeed;
    }
}
