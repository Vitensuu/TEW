using System.Collections.Generic;
using UnityEngine;
using Game.Data;

namespace Game.Items
{
    /// <summary>
    /// Дроп лута по таблице весов (ТЗ §5 — LootDropper).
    /// Для каждой записи кидает dropChance; среди выпавших — рулетка по weight.
    /// Создаёт ItemPickup в мире (использует префаб из LootDropperConfig, иначе авто-объект).
    /// </summary>
    public static class LootDropper
    {
        public static GameObject PickupPrefab; // назначается LootDropperConfig в сцене

        public static void DropLoot(List<LootEntry> table, Vector3 position)
        {
            if (table == null || table.Count == 0) return;

            // Сначала отбираем по dropChance.
            var candidates = new List<LootEntry>();
            float totalWeight = 0f;
            foreach (var e in table)
            {
                if (e.item == null) continue;
                if (Random.value <= e.dropChance)
                {
                    candidates.Add(e);
                    totalWeight += Mathf.Max(0.0001f, e.weight);
                }
            }
            if (candidates.Count == 0) return;

            // Рулетка по весу — один предмет за смерть (MVP).
            float roll = Random.value * totalWeight;
            float acc = 0f;
            ItemData chosen = candidates[candidates.Count - 1].item;
            foreach (var e in candidates)
            {
                acc += Mathf.Max(0.0001f, e.weight);
                if (roll <= acc) { chosen = e.item; break; }
            }

            Spawn(chosen, position);
        }

        public static void Spawn(ItemData item, Vector3 position)
        {
            if (item == null) return;

            GameObject go;
            if (PickupPrefab != null)
            {
                go = Object.Instantiate(PickupPrefab, position, Quaternion.identity);
            }
            else
            {
                go = new GameObject($"Pickup_{item.itemName}");
                go.transform.position = position;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = item.sprite;
                sr.sortingOrder = 5;
                if (item.sprite == null) sr.color = Color.yellow;
                go.AddComponent<CircleCollider2D>().radius = 0.4f;
            }

            var pickup = go.GetComponent<ItemPickup>() ?? go.AddComponent<ItemPickup>();
            pickup.Setup(item);
        }
    }

    /// <summary>Опциональный конфиг сцены: задаёт префаб подбора для LootDropper.</summary>
    public class LootDropperConfig : MonoBehaviour
    {
        [SerializeField] GameObject pickupPrefab;
        void Awake() => LootDropper.PickupPrefab = pickupPrefab;
    }
}
