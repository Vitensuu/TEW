using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Маркер-абстракция врага для слабосвязанных событий (ТЗ §11 — разрыв
    /// циклической зависимости Core ↔ Gameplay).
    /// EventBus оперирует IEnemy, а не конкретным EnemyBase, поэтому слой Core
    /// больше не знает о слое геймплея. Конкретику получают подписчики через as-cast.
    /// </summary>
    public interface IEnemy
    {
        Transform transform { get; }
        bool Dead { get; }
    }
}
