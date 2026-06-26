using UnityEditor;
using UnityEngine;
using Game.Core;
using Game.Data;
using Game.Dungeon;
using Enemy;

namespace Game.DungeonEditor
{
    /// <summary>
    /// Сборщик босса «Нихиль» (Фаза 7): создаёт BossData с 3 фазами (Угасание →
    /// Свёртка → Бездна), клонирует базовый префаб в boss-префаб с BossController,
    /// назначает одну комнату библиотеки типом Boss и включает босса во FloorConfig.
    /// Призываемые миньоны берутся из бестиария (Фаза 6) — сначала запусти Build Bestiary.
    ///
    /// Меню: TEW ▸ Enemies ▸ Build Boss (Nihil)
    /// </summary>
    public static class BossBuilder
    {
        const string BasePrefab  = "Assets/Enemy1/Enemy1.prefab";
        const string OutFolder   = "Assets/Bestiary";
        const string FloorConfig = "Assets/maps/RoomData/Floor_Default.asset";
        const string BossRoom    = "Assets/maps/RoomData/Room_7_room.asset";

        [MenuItem("TEW/Enemies/Build Boss (Nihil)")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefab) == null)
            {
                Debug.LogError($"[BossBuilder] Не найден базовый префаб {BasePrefab}."); return;
            }
            if (!AssetDatabase.IsValidFolder(OutFolder))
                AssetDatabase.CreateFolder("Assets", "Bestiary");

            var hound    = AssetDatabase.LoadAssetAtPath<EnemyData>($"{OutFolder}/VoidHound.asset");
            var hexbinder = AssetDatabase.LoadAssetAtPath<EnemyData>($"{OutFolder}/Hexbinder.asset");
            if (hound == null || hexbinder == null)
                Debug.LogWarning("[BossBuilder] Миньоны бестиария не найдены — сначала TEW ▸ Enemies ▸ Build Bestiary. Босс соберётся, но призыв будет пустым.");

            // 1. BossData + фазы.
            string dataPath = $"{OutFolder}/Nihil.asset";
            var boss = AssetDatabase.LoadAssetAtPath<BossData>(dataPath);
            if (boss == null)
            {
                boss = ScriptableObject.CreateInstance<BossData>();
                AssetDatabase.CreateAsset(boss, dataPath);
            }
            boss.enemyName      = "Нихиль, Тот-Кто-Сворачивает-Свет";
            boss.maxHealth      = 600;
            boss.damage         = 14;
            boss.speed          = 2.2f;
            boss.detectionRange = 100;
            boss.attackRange    = 1.3f;
            boss.attackCooldown = 2f;
            boss.behaviorType   = BehaviorType.Boss;
            boss.roles          = EnemyRole.Elite;
            boss.isFinalBoss    = true;   // смерть Нихиля → Victory (ТЗ §6)

            boss.phases = new System.Collections.Generic.List<BossPhase>
            {
                new BossPhase   // Фаза 1 «Угасание» (>66% HP)
                {
                    phaseName = "Угасание", healthThreshold = 1f,
                    moveSpeed = 2.2f, attackCooldown = 2.5f, contactDamage = 12,
                    summonsMinions = hound != null, minionToSummon = hound, minionsPerSummon = 2,
                    buildWalls = true, wallInterval = 7f,           // обрушение колонн → реформа арены
                },
                new BossPhase   // Фаза 2 «Свёртка» (33–66%)
                {
                    phaseName = "Свёртка", healthThreshold = 0.66f,
                    moveSpeed = 2.4f, attackCooldown = 2f, contactDamage = 14,
                    summonsMinions = hexbinder != null, minionToSummon = hexbinder, minionsPerSummon = 2,
                    linkToMinions = true, linkedDamageTaken = 0.3f, // пока связи целы — урон ×0.3
                    hazardField = true, hazardInterval = 3.5f, hazardType = DamageType.Magic,
                },
                new BossPhase   // Фаза 3 «Бездна» (<33%)
                {
                    phaseName = "Бездна", healthThreshold = 0.33f,
                    moveSpeed = 2.7f, attackCooldown = 1.5f, contactDamage = 16,
                    summonsMinions = hound != null, minionToSummon = hound, minionsPerSummon = 1,
                    mirrorDamage = true, mirrorFraction = 0.3f,     // зеркало
                    buildWalls = true, wallInterval = 4f,           // перестройка в лабиринт
                    hazardField = true, hazardInterval = 2.5f, hazardType = DamageType.Magic,
                },
            };

            // 2. Boss-префаб (клон базы → BossController).
            var root = PrefabUtility.LoadPrefabContents(BasePrefab);
            var old = root.GetComponent<StateMachineEnemy>();
            if (old != null) Object.DestroyImmediate(old);

            var bc = root.AddComponent<BossController>();
            root.transform.localScale = Vector3.one * 1.6f;          // крупнее обычных

            int playerLayer = 0;
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerLayer = p.layer;

            var so = new SerializedObject(bc);
            var dp = so.FindProperty("data");
            if (dp != null) dp.objectReferenceValue = boss;
            var pm = so.FindProperty("playerMask");
            if (pm != null) pm.intValue = 1 << playerLayer;
            so.ApplyModifiedPropertiesWithoutUndo();

            string prefabPath = $"{OutFolder}/Nihil.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);

            boss.spritePrefab = prefab;
            EditorUtility.SetDirty(boss);

            // 3. Назначить комнату типом Boss (иначе боссу негде заспавниться).
            var bossRoom = AssetDatabase.LoadAssetAtPath<RoomData>(BossRoom);
            if (bossRoom != null)
            {
                bossRoom.roomType = RoomType.Boss;
                EditorUtility.SetDirty(bossRoom);
            }
            else Debug.LogWarning($"[BossBuilder] {BossRoom} не найден — пометь любую RoomData типом Boss вручную.");

            // 4. Включить босса во FloorConfig.
            var fc = AssetDatabase.LoadAssetAtPath<FloorConfig>(FloorConfig);
            if (fc != null)
            {
                fc.hasBoss  = true;
                fc.bossData = boss;
                EditorUtility.SetDirty(fc);
            }
            else Debug.LogWarning($"[BossBuilder] {FloorConfig} не найден — включи hasBoss и назначь bossData вручную.");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BossBuilder] Нихиль собран: BossData+префаб, комната Room_7 → Boss, Floor_Default.hasBoss=true. " +
                      "Нажми Play — на самой дальней ветке этажа будет арена босса.");
            Selection.activeObject = boss;
        }
    }
}
