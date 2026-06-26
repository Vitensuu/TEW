using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.Data;

namespace Game.Items
{
    /// <summary>
    /// Управление предметами и экипировкой (ТЗ §5 — InventoryManager, Singleton).
    /// Слоты (ТЗ §4): 6 оружия (активно 1), 3 брони, 6 расходников, ∞ реликвий.
    /// Полный слот → событие OnInventoryFull для UI-выбора «Взять/Выбросить/Заменить».
    /// </summary>
    public class InventoryManager : Singleton<InventoryManager>
    {
        public const int WeaponSlots     = 6;
        public const int ArmorSlots      = 3;
        public const int ConsumableSlots = 6;

        readonly List<WeaponData>     _weapons     = new List<WeaponData>();
        readonly List<ItemData>       _armor       = new List<ItemData>();
        readonly List<ItemInstance>   _consumables = new List<ItemInstance>();
        readonly List<RelicData>      _relics      = new List<RelicData>();

        public int ActiveWeaponIndex { get; private set; }

        public IReadOnlyList<WeaponData>   Weapons     => _weapons;
        public IReadOnlyList<ItemInstance> Consumables => _consumables;
        public IReadOnlyList<RelicData>    Relics      => _relics;

        public WeaponData ActiveWeapon =>
            _weapons.Count > 0 ? _weapons[Mathf.Clamp(ActiveWeaponIndex, 0, _weapons.Count - 1)] : null;

        // Событие для UI: предмет не влез — нужен выбор. (slotType, item)
        public System.Action<ItemData> OnInventoryFull;
        public System.Action OnChanged;

        protected override void Awake()
        {
            base.Awake();
            if (Instance == this) LoadFromRun();
        }

        /// <summary>Заполнить инвентарь из стартовых предметов забега.</summary>
        public void LoadFromRun()
        {
            var run = GameManager.Instance != null ? GameManager.Instance.Run : null;
            if (run == null) return;
            foreach (var inst in run.inventory)
                if (inst?.data != null) AddItem(inst.data, silent: true);
            OnChanged?.Invoke();
        }

        // ── Добавление ──────────────────────────────────────────────────────────
        public bool AddItem(ItemData item, bool silent = false)
        {
            if (item == null) return false;
            bool added = item switch
            {
                WeaponData w     => AddWeapon(w),
                ConsumableData c => AddConsumable(c),
                RelicData r      => AddRelic(r),
                _                => AddArmor(item),
            };

            if (added && !silent)
            {
                EventBus.TriggerItemPickup(item);
                OnChanged?.Invoke();
            }
            else if (!added && !silent)
            {
                OnInventoryFull?.Invoke(item);   // UI решает: заменить/выбросить
            }
            return added;
        }

        bool AddWeapon(WeaponData w)
        {
            if (_weapons.Count >= WeaponSlots) return false;
            _weapons.Add(w);
            return true;
        }

        bool AddArmor(ItemData a)
        {
            if (_armor.Count >= ArmorSlots) return false;
            _armor.Add(a);
            return true;
        }

        bool AddConsumable(ConsumableData c)
        {
            if (c.stackable)
            {
                foreach (var inst in _consumables)
                    if (inst.data == c && inst.stackCount < c.maxStack)
                    {
                        inst.stackCount++;
                        return true;
                    }
            }
            if (_consumables.Count >= ConsumableSlots) return false;
            _consumables.Add(new ItemInstance(c));
            return true;
        }

        bool AddRelic(RelicData r)
        {
            _relics.Add(r);                          // ∞ реликвий
            var run = GameManager.Instance != null ? GameManager.Instance.Run : null;
            if (run != null && !run.activeRelics.Contains(r))
            {
                run.activeRelics.Add(r);
                // Спец-эффект при подборе (ТЗ §3 — onPickupEffect).
                RelicEffectHandler.Trigger(r.onPickupEffect, r);
                // Реликвия меняет статы → пересчёт.
                var stats = ServiceLocator.Get<Player.PlayerStats>(); // ТЗ §6 — развязка
                stats?.Recompute();
            }
            return true;
        }

        // ── Активное оружие ─────────────────────────────────────────────────────
        public void SetActiveWeapon(int index)
        {
            if (index < 0 || index >= _weapons.Count) return;
            ActiveWeaponIndex = index;
            OnChanged?.Invoke();
        }

        public void CycleWeapon()
        {
            if (_weapons.Count == 0) return;
            ActiveWeaponIndex = (ActiveWeaponIndex + 1) % _weapons.Count;
            OnChanged?.Invoke();
        }

        // ── Расходники ──────────────────────────────────────────────────────────
        public bool UseConsumable(int index, GameObject user)
        {
            if (index < 0 || index >= _consumables.Count) return false;
            var inst = _consumables[index];
            ConsumableEffects.Apply(inst.AsConsumable, user);

            // Учёт использованных предметов для статистики (ТЗ §6 — Victory).
            var run = Game.Core.GameManager.Instance != null ? Game.Core.GameManager.Instance.Run : null;
            if (run != null) run.itemsUsed++;

            if (--inst.stackCount <= 0) _consumables.RemoveAt(index);
            OnChanged?.Invoke();
            return true;
        }

        // ── Замена/выброс (для UI «полный инвентарь») ───────────────────────────
        public void ReplaceWeapon(int index, WeaponData newWeapon)
        {
            if (index < 0 || index >= _weapons.Count) return;
            _weapons[index] = newWeapon;
            OnChanged?.Invoke();
        }
    }
}
