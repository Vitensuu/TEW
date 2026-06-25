using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// Запись таблицы дропа (ТЗ §3 — LootEntry). weight — вес в рулетке,
    /// dropChance — вероятность что запись вообще выпадет (0..1).
    /// </summary>
    [Serializable]
    public struct LootEntry
    {
        public ItemData item;
        [Range(0f, 1f)] public float dropChance;
        public float weight;
    }

    /// <summary>
    /// Данные врага (ТЗ §3 — EnemyData). behaviorType выбирает AI-компонент,
    /// который должен висеть на префабе (Melee/Ranged/Charger/Summoner/Boss).
    /// </summary>
    [CreateAssetMenu(menuName = "TEW/Enemy", fileName = "Enemy_")]
    public class EnemyData : ScriptableObject
    {
        [Header("Характеристики")]
        public string enemyName = "Enemy";
        public float maxHealth     = 30f;
        public float damage        = 5f;
        public float speed         = 2.5f;
        public float detectionRange = 7f;
        public float attackRange   = 0.9f;
        public float attackCooldown = 1.2f;

        [Header("Поведение")]
        public BehaviorType behaviorType = BehaviorType.Melee;

        [Header("Награды")]
        public int expReward  = 5;
        public int goldReward = 3;
        public List<LootEntry> lootTable = new List<LootEntry>();

        [Header("Визуал")]
        public GameObject spritePrefab;
    }
}
