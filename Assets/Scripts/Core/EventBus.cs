using System;
using Game.Data;

namespace Game.Core
{
    /// <summary>
    /// Глобальная шина событий (ТЗ §5 — EventBus, паттерн Observer).
    /// Системы общаются слабосвязанно: подписался — реагируешь, не зная отправителя.
    ///
    /// ВАЖНО: подписки статические и переживают смену сцены. Каждый подписчик
    /// ОБЯЗАН отписаться в OnDisable/OnDestroy, иначе утечки и «мёртвые» делегаты.
    /// При старте новой сцены вызывай EventBus.Clear() из GameManager, если нужно.
    /// </summary>
    public static class EventBus
    {
        // ── Жизненный цикл забега ──────────────────────────────────────────────
        public static event Action OnPlayerDeath;
        public static event Action<int> OnFloorComplete;   // floorNumber
        public static event Action<int> OnFloorGenerated;  // floorNumber

        // ── Бой / лут ──────────────────────────────────────────────────────────
        public static event Action<ItemData> OnItemPickup;
        public static event Action<IEnemy> OnEnemyKilled;   // ТЗ §11 — IEnemy, не EnemyBase (разрыв цикла)
        public static event Action<int> OnGoldChanged;          // newTotal
        public static event Action<int> OnSoulShardsChanged;    // newTotal (мета-валюта)

        // ── Бой: попадания (для SFX/частиц/числа урона) ────────────────────────
        public static event Action<UnityEngine.Vector3, float, bool> OnDamageDealt; // pos, amount, isCrit

        // ── Триггеры ────────────────────────────────────────────────────────────
        public static void TriggerPlayerDeath()              => OnPlayerDeath?.Invoke();
        public static void TriggerFloorComplete(int f)       => OnFloorComplete?.Invoke(f);
        public static void TriggerFloorGenerated(int f)      => OnFloorGenerated?.Invoke(f);
        public static void TriggerItemPickup(ItemData item)  => OnItemPickup?.Invoke(item);
        public static void TriggerEnemyKilled(IEnemy e)      => OnEnemyKilled?.Invoke(e);
        public static void TriggerGoldChanged(int total)     => OnGoldChanged?.Invoke(total);
        public static void TriggerSoulShardsChanged(int t)   => OnSoulShardsChanged?.Invoke(t);
        public static void TriggerDamageDealt(UnityEngine.Vector3 pos, float amount, bool crit)
            => OnDamageDealt?.Invoke(pos, amount, crit);

        /// <summary>Снять все подписки (вызывать при полном рестарте/выходе в меню).</summary>
        public static void Clear()
        {
            OnPlayerDeath = null;
            OnFloorComplete = null;
            OnFloorGenerated = null;
            OnItemPickup = null;
            OnEnemyKilled = null;
            OnGoldChanged = null;
            OnSoulShardsChanged = null;
            OnDamageDealt = null;
        }
    }
}
