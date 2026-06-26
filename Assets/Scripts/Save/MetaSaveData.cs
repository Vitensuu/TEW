using System;
using System.Collections.Generic;

namespace Game.Save
{
    /// <summary>
    /// Перманентные данные между забегами (ТЗ §4 — Мета-прогрессия).
    /// Сохраняется в MetaSaveData.json через JsonUtility.
    /// </summary>
    [Serializable]
    public class MetaSaveData
    {
        public int soulShards;                                  // «Осколки душ»

        public List<string> unlockedCharacters = new List<string>(); // characterName / id
        public List<string> unlockedItems      = new List<string>(); // item id

        // Дерево улучшений: 3 ветки × 10 узлов (ТЗ §4). Храним купленные узлы по id.
        public List<string> purchasedUpgrades  = new List<string>();

        // Статистика
        public int totalRuns;
        public int bestFloor;
        public int totalEnemiesKilled;

        public bool HasUpgrade(string nodeId) => purchasedUpgrades.Contains(nodeId);
        public bool HasCharacter(string id)   => unlockedCharacters.Contains(id);
        public bool HasItem(string id)        => unlockedItems.Contains(id);
    }
}
