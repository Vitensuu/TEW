namespace Game.Core
{
    /// <summary>
    /// Интерфейс интерактивных объектов (ТЗ §5 — IInteractable):
    /// предметы на полу, сундуки, магазин, святилище, выход.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Подсказка для UI («Нажми E чтобы …»).</summary>
        string InteractPrompt { get; }

        /// <summary>Сработать. interactor — кто взаимодействует (игрок).</summary>
        void Interact(PlayerController interactor);
    }
}
