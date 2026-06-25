using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.Data;

namespace Game.Items
{
    /// <summary>
    /// Описание одной синергии (комбо предметов → бонус).
    /// </summary>
    [Serializable]
    public class ItemSynergy
    {
        public string synergyName;
        [TextArea] public string description;
        [Tooltip("ID предметов/реликвий, которые должны быть собраны все вместе")]
        public List<string> requiredItemIds = new List<string>();
        public List<StatModifier> bonusModifiers = new List<StatModifier>();
    }

    /// <summary>
    /// Детектор синергий (ТЗ §4 — ItemSynergyDetector).
    /// При изменении инвентаря проверяет комбинации и выдаёт бонусные эффекты.
    /// Повесь на игрока; подписывается на InventoryManager.OnChanged.
    /// </summary>
    public class ItemSynergyDetector : MonoBehaviour
    {
        [SerializeField] List<ItemSynergy> synergies = new List<ItemSynergy>();

        readonly HashSet<string> _active = new HashSet<string>();

        public System.Action<ItemSynergy> OnSynergyActivated;

        void OnEnable()
        {
            if (InventoryManager.Instance != null)
                InventoryManager.Instance.OnChanged += Check;
            Check();
        }

        void OnDisable()
        {
            if (InventoryManager.Instance != null)
                InventoryManager.Instance.OnChanged -= Check;
        }

        void Check()
        {
            var owned = CollectOwnedIds();
            foreach (var s in synergies)
            {
                if (_active.Contains(s.synergyName)) continue;
                if (HasAll(owned, s.requiredItemIds))
                {
                    _active.Add(s.synergyName);
                    ActivateSynergy(s);
                }
            }
        }

        HashSet<string> CollectOwnedIds()
        {
            var ids = new HashSet<string>();
            var inv = InventoryManager.Instance;
            if (inv == null) return ids;
            foreach (var w in inv.Weapons) if (w != null) ids.Add(w.Id);
            foreach (var r in inv.Relics)  if (r != null) ids.Add(r.Id);
            foreach (var c in inv.Consumables) if (c?.data != null) ids.Add(c.data.Id);
            return ids;
        }

        static bool HasAll(HashSet<string> owned, List<string> required)
        {
            foreach (var id in required) if (!owned.Contains(id)) return false;
            return required.Count > 0;
        }

        void ActivateSynergy(ItemSynergy s)
        {
            Debug.Log($"[Synergy] Активирована: {s.synergyName}");
            // Применяем бонусные модификаторы через активную реликвию-«виртуалку»:
            // проще всего пересчитать статы, добавив их в run как реликвию-эффект.
            var run = GameManager.Instance != null ? GameManager.Instance.Run : null;
            if (run != null && s.bonusModifiers.Count > 0)
            {
                var virtualRelic = ScriptableObject.CreateInstance<RelicData>();
                virtualRelic.itemName = s.synergyName;
                virtualRelic.statModifiers = s.bonusModifiers;
                run.activeRelics.Add(virtualRelic);
                GetComponent<Player.PlayerStats>()?.Recompute();
            }
            OnSynergyActivated?.Invoke(s);
        }
    }
}
