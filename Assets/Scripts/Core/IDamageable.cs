using System;
using Game.Data;

namespace Game.Core
{
    /// <summary>
    /// Интерфейс всего, что можно ударить (ТЗ §5 — IDamageable).
    /// Реализуется игроком, врагами, боссами, разрушаемыми объектами.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>Нанести урон. type влияет на elementalMod и статус-эффекты.</summary>
        void TakeDamage(float amount, DamageType type = DamageType.Physical);

        /// <summary>Восстановить здоровье.</summary>
        void Heal(float amount);

        /// <summary>Жив ли объект.</summary>
        bool IsAlive { get; }

        /// <summary>Событие изменения HP: (current, max).</summary>
        event Action<float, float> OnHealthChanged;
    }
}
