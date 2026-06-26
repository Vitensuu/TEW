using System.Collections.Generic;
using UnityEngine;
using Game.Data;
using Enemy;

namespace Game.Dungeon
{
    /// <summary>
    /// Заселение готовой комнаты врагами (ТЗ ЭТАП 5). Работает по точкам спавна
    /// внутри префаба (SpawnPoint kind=Enemy), используя FloorConfig.enemyPool /
    /// BossData. Лут при смерти врагов остаётся за LootDropper (как раньше).
    /// Полная замена старого RoomPopulator, который работал по RectInt-комнатам.
    /// </summary>
    public class RoomPopulator : MonoBehaviour
    {
        [Header("Фолбэк-префабы (если у EnemyData нет spritePrefab)")]
        [SerializeField] GameObject[] enemyPrefabs;
        [Tooltip("Префаб босса (BossController + BossData). Если пусто — берётся cfg.bossData.spritePrefab")]
        [SerializeField] GameObject bossPrefab;

        [Header("Fallback-спавн (если в комнате нет точек SpawnKind.Enemy)")]
        [SerializeField] int fallbackMin = 2;
        [SerializeField] int fallbackMax = 4;

        [Header("Элиты (Фаза 2)")]
        [Tooltip("Шанс, что заспавненный враг станет элитным (получит EliteModifier)")]
        [Range(0f, 1f)] [SerializeField] float eliteChance = 0.12f;

        [Header("Связи (Фаза 3)")]
        [Tooltip("Шанс, что в комнате (>=2 врага) появится носитель CorruptionLink")]
        [Range(0f, 1f)] [SerializeField] float hexbinderChance = 0.15f;

        [Header("Эволюция (Фаза 5)")]
        [Tooltip("Шанс, что враг получит эволюцию-ярость на <50% HP")]
        [Range(0f, 1f)] [SerializeField] float evolveChance = 0.1f;

        /// <summary>Заселить одну комнату. Возвращает список заспавненных врагов.</summary>
        public List<GameObject> PopulateRoom(RoomInstance room, FloorConfig cfg, int floor)
        {
            var spawned = new List<GameObject>();
            if (room == null) return spawned;

            if (room.Type == RoomType.Boss)
            {
                var boss = ResolveBoss(cfg);
                if (boss != null)
                {
                    Vector3 pos = room.EnemyPoints.Count > 0
                        ? room.EnemyPoints[0].transform.position
                        : room.transform.position;
                    spawned.Add(Instantiate(boss, pos, Quaternion.identity));
                }
                return spawned;
            }

            // Normal / Elite — по точкам спавна.
            if (room.EnemyPoints.Count > 0)
            {
                foreach (var pt in room.EnemyPoints)
                {
                    if (Random.value > pt.chance) continue;
                    var prefab = ResolveEnemy(cfg);
                    if (prefab != null)
                        spawned.Add(SpawnEnemy(prefab, pt.transform.position));
                }
            }
            else
            {
                // Fallback: точек нет (голый визуальный префаб) — спавним по площади пола.
                int count = Random.Range(fallbackMin, fallbackMax + 1);
                for (int i = 0; i < count; i++)
                {
                    var prefab = ResolveEnemy(cfg);
                    if (prefab != null)
                        spawned.Add(SpawnEnemy(prefab, room.RandomFloorPoint()));
                }
            }

            TryAddHexbinder(spawned);
            return spawned;
        }

        /// <summary>Заспавнить врага и с шансом eliteChance сделать его элитным.</summary>
        GameObject SpawnEnemy(GameObject prefab, Vector3 pos)
        {
            var go = Instantiate(prefab, pos, Quaternion.identity);
            if (go.GetComponent<EnemyBase>() == null) return go;

            if (Random.value <= eliteChance)
            {
                var types = (EliteModifierType[])System.Enum.GetValues(typeof(EliteModifierType));
                EliteModifier.Attach(go, types[Random.Range(0, types.Length)]);
            }
            if (Random.value <= evolveChance)
                EvolutionController.AttachEnrage(go);

            return go;
        }

        /// <summary>С шансом hexbinderChance сделать одного врага носителем CorruptionLink.</summary>
        void TryAddHexbinder(List<GameObject> spawned)
        {
            if (spawned.Count < 2 || Random.value > hexbinderChance) return;
            foreach (var go in spawned)
            {
                if (go != null && go.GetComponent<EnemyBase>() != null)
                {
                    go.AddComponent<HexbinderCaster>();
                    return;
                }
            }
        }

        GameObject ResolveEnemy(FloorConfig cfg)
        {
            // Приоритет: пул EnemyData из FloorConfig (их spritePrefab).
            if (cfg != null && cfg.enemyPool != null && cfg.enemyPool.Count > 0)
            {
                var data = cfg.enemyPool[Random.Range(0, cfg.enemyPool.Count)];
                if (data != null && data.spritePrefab != null) return data.spritePrefab;
            }
            if (enemyPrefabs != null && enemyPrefabs.Length > 0)
                return enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            return null;
        }

        GameObject ResolveBoss(FloorConfig cfg)
        {
            if (bossPrefab != null) return bossPrefab;
            if (cfg != null && cfg.bossData != null && cfg.bossData.spritePrefab != null)
                return cfg.bossData.spritePrefab;
            return null;
        }
    }
}
