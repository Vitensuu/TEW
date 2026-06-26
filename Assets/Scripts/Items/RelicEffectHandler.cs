using UnityEngine;
using Game.Core;
using Game.Data;

namespace Game.Items
{
    /// <summary>
    /// Спец-эффекты реликвий при подборе (ТЗ §3 — RelicData.onPickupEffect).
    /// statModifiers применяются автоматически в PlayerStats; здесь — нестандартные
    /// эффекты (исцеление при подборе, доп. снаряд и т.п.) по строковому ID.
    /// Расширяй switch новыми кейсами.
    /// </summary>
    public static class RelicEffectHandler
    {
        public static void Trigger(string effectId, RelicData relic)
        {
            if (string.IsNullOrEmpty(effectId)) return;

            switch (effectId)
            {
                case "heal_on_pickup":
                    ServiceLocator.Get<PlayerHealth>()?.Heal(50f); // ТЗ §6 — развязка
                    break;

                case "full_heal":
                    var hp = ServiceLocator.Get<PlayerHealth>();
                    if (hp != null) hp.Heal(hp.MaxHp);
                    break;

                // Добавляй свои эффекты здесь.
                default:
                    Debug.Log($"[RelicEffectHandler] Неизвестный эффект '{effectId}' " +
                              $"для реликвии {relic?.itemName}. Только статы применены.");
                    break;
            }
        }
    }
}
