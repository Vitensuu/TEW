using System.Collections.Generic;
using UnityEngine;
using Game.Data;

namespace Game.Items
{
    /// <summary>
    /// Реестр всех предметов (ТЗ §5 — ItemDatabase, Registry).
    /// SO-ассет: назначь все WeaponData/RelicData/ConsumableData в инспекторе.
    /// Поиск по ID для save/загрузки и случайного лута по редкости.
    /// </summary>
    [CreateAssetMenu(menuName = "TEW/ItemDatabase", fileName = "ItemDatabase")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField] List<ItemData> items = new List<ItemData>();

        Dictionary<string, ItemData> _byId;

        public IReadOnlyList<ItemData> All => items;

        void OnEnable() => Rebuild();

        public void Rebuild()
        {
            _byId = new Dictionary<string, ItemData>();
            foreach (var it in items)
                if (it != null && !_byId.ContainsKey(it.Id))
                    _byId[it.Id] = it;
        }

        public ItemData GetById(string id)
        {
            if (_byId == null) Rebuild();
            return id != null && _byId.TryGetValue(id, out var it) ? it : null;
        }

        public ItemData RandomByRarity(Rarity rarity)
        {
            var pool = new List<ItemData>();
            foreach (var it in items)
                if (it != null && it.Rarity == rarity) pool.Add(it);
            return pool.Count > 0 ? pool[Random.Range(0, pool.Count)] : null;
        }
    }
}
