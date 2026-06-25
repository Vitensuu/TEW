using UnityEngine;
using Game.Core;
using Game.Data;

namespace Game.Items
{
    /// <summary>
    /// Применение эффектов расходников (ТЗ §3 — ConsumableData.effectType).
    /// Heal / ManaRestore / Damage(бомба) / Buff.
    /// </summary>
    public static class ConsumableEffects
    {
        public static void Apply(ConsumableData c, GameObject user)
        {
            if (c == null || user == null) return;

            switch (c.effectType)
            {
                case EffectType.Heal:
                    user.GetComponent<PlayerHealth>()?.Heal(c.effectValue);
                    break;

                case EffectType.ManaRestore:
                    user.GetComponent<PlayerMana>()?.RestoreMana(c.effectValue);
                    break;

                case EffectType.Damage: // бомба: урон по площади
                    var hits = Physics2D.OverlapCircleAll(user.transform.position, 3f);
                    foreach (var h in hits)
                    {
                        var dmg = h.GetComponentInParent<IDamageable>();
                        if (dmg != null && dmg != user.GetComponent<IDamageable>() && dmg.IsAlive)
                            dmg.TakeDamage(c.effectValue, DamageType.Fire);
                    }
                    break;

                case EffectType.Buff:
                    // Временный бафф можно навесить через StatusEffectHandler.
                    break;
            }
        }
    }
}
