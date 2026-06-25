using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// Одна фаза боя босса (ТЗ §3 — BossPhase). Переход на следующую фазу
    /// когда HP падает ниже healthThreshold (доля 0..1).
    /// </summary>
    [Serializable]
    public class BossPhase
    {
        public string phaseName = "Phase";
        [Range(0f, 1f)] public float healthThreshold = 0.5f; // включается ниже этого %HP
        public float moveSpeed       = 2.5f;
        public float attackCooldown  = 1.5f;
        public float contactDamage   = 12f;

        [Tooltip("Снаряд фазы (если стреляет)")]
        public GameObject projectilePrefab;
        [Tooltip("Снарядов за залп (Spread)")]
        public int projectilesPerVolley = 8;
        [Tooltip("Призывает миньонов в этой фазе")]
        public bool summonsMinions;
        public EnemyData minionToSummon;
        public int minionsPerSummon = 2;
    }

    /// <summary>
    /// Босс (ТЗ §3 — BossData : EnemyData). Несколько фаз, своя музыка,
    /// гарантированный дроп.
    /// </summary>
    [CreateAssetMenu(menuName = "TEW/Boss", fileName = "Boss_")]
    public class BossData : EnemyData
    {
        [Header("Босс")]
        public List<BossPhase> phases = new List<BossPhase>();
        public AudioClip bossMusic;
        public ItemData guaranteedLoot;
    }
}
