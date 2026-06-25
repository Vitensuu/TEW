using UnityEngine;
using Game.Core;
using Game.Data;

namespace Game.Items
{
    /// <summary>
    /// Предмет на полу (ТЗ §4 — «Подбор: OnTriggerEnter2D → InventoryManager.AddItem()»).
    /// Реализует IInteractable: можно подобрать автоматически (касание) или по кнопке E.
    /// Нужен Collider2D (IsTrigger). Игрок должен иметь тег "Player".
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ItemPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] ItemData item;
        [SerializeField] bool autoPickupOnTouch = true;

        SpriteRenderer _sr;

        public string InteractPrompt => item != null ? $"Взять {item.itemName}" : "Взять";

        public void Setup(ItemData data)
        {
            item = data;
            ApplyVisual();
        }

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
            ApplyVisual();
        }

        void ApplyVisual()
        {
            if (_sr != null && item != null && item.sprite != null)
                _sr.sprite = item.sprite;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!autoPickupOnTouch) return;
            if (!other.CompareTag("Player")) return;
            TryPickup(other.GetComponent<PlayerController>());
        }

        public void Interact(PlayerController interactor) => TryPickup(interactor);

        void TryPickup(PlayerController interactor)
        {
            if (item == null || InventoryManager.Instance == null) return;
            bool added = InventoryManager.Instance.AddItem(item);
            if (added) Destroy(gameObject);
            // Если не влез — InventoryManager поднимет OnInventoryFull, предмет остаётся.
        }
    }
}
