using System;

namespace Game.Data
{
    /// <summary>
    /// Runtime-экземпляр предмета в инвентаре (ТЗ §3 — inventory: List&lt;ItemInstance&gt;).
    /// Ссылается на SO-данные + хранит изменяемое состояние (стек, прокачка).
    /// </summary>
    [Serializable]
    public class ItemInstance
    {
        public ItemData data;
        public int stackCount = 1;

        public ItemInstance(ItemData data, int count = 1)
        {
            this.data = data;
            stackCount = count;
        }

        public bool IsWeapon     => data is WeaponData;
        public bool IsRelic      => data is RelicData;
        public bool IsConsumable => data is ConsumableData;

        public WeaponData     AsWeapon     => data as WeaponData;
        public RelicData      AsRelic      => data as RelicData;
        public ConsumableData AsConsumable => data as ConsumableData;
    }
}
