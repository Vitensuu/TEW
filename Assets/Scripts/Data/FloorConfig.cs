using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// Конфигурация этажа (ТЗ §3 — FloorConfig). Управляет генерацией, пулом
    /// врагов, шансами спец-комнат и боссом. Босс — на этажах кратных 5 (ТЗ §1).
    /// </summary>
    [CreateAssetMenu(menuName = "TEW/FloorConfig", fileName = "Floor_")]
    public class FloorConfig : ScriptableObject
    {
        [Header("Этаж")]
        public int floorNumber = 1;
        public int minRooms = 8;
        public int maxRooms = 14;

        [Header("Пул врагов")]
        public List<EnemyData> enemyPool = new List<EnemyData>();
        public int minEnemiesPerRoom = 1;
        public int maxEnemiesPerRoom = 3;

        [Header("Босс")]
        public bool hasBoss;
        public BossData bossData;

        [Header("Шансы спец-комнат (0..1)")]
        [Range(0f, 1f)] public float shopChance     = 0.10f;
        [Range(0f, 1f)] public float shrineChance   = 0.10f;
        [Range(0f, 1f)] public float treasureChance = 0.12f;
    }
}
