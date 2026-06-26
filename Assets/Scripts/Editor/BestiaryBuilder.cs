using System;
using UnityEditor;
using UnityEngine;
using Game.Data;
using Enemy;

namespace Game.DungeonEditor
{
    /// <summary>
    /// Сборщик бестиария (Фаза 6): из базового рабочего префаба врага (с Animator/
    /// Rigidbody/SpriteRenderer) клонирует 6 врагов дизайна — 4 обычных + 2 элиты —
    /// навешивая нужный AI и поведенческие компоненты, создаёт EnemyData-ассеты
    /// с ролями и прописывает их в FloorConfig.enemyPool. Анимации берутся из базы
    /// и НЕ трогаются. Повторный запуск перезаписывает ассеты/префабы.
    ///
    /// Меню: TEW ▸ Enemies ▸ Build Bestiary
    /// </summary>
    public static class BestiaryBuilder
    {
        const string BasePrefab  = "Assets/Enemy1/Enemy1.prefab";
        const string OutFolder   = "Assets/Bestiary";
        const string FloorConfig = "Assets/maps/RoomData/Floor_Default.asset";

        [MenuItem("TEW/Enemies/Build Bestiary")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefab) == null)
            {
                Debug.LogError($"[BestiaryBuilder] Не найден базовый префаб {BasePrefab}.");
                return;
            }
            if (!AssetDatabase.IsValidFolder(OutFolder))
                AssetDatabase.CreateFolder("Assets", "Bestiary");

            var datas = new System.Collections.Generic.List<EnemyData>();

            // ① Хладный Глашатай — Controller (лёд + стены).
            datas.Add(Make("Хладный Глашатай", "FrostHerald", typeof(RangedEnemy),
                EnemyRole.Controller, BehaviorType.Ranged,
                hp: 35, dmg: 4, speed: 2.2f, detection: 100,
                root => root.AddComponent<FrostHerald>()));

            // ② Костяной Пёс Бездны — Hunter/Assassin (рывок).
            datas.Add(Make("Костяной Пёс Бездны", "VoidHound", typeof(ChargerEnemy),
                EnemyRole.Hunter | EnemyRole.Assassin, BehaviorType.Charger,
                hp: 25, dmg: 8, speed: 3.2f, detection: 100, null));

            // ③ Гнилостный Носитель — Tank/Disruptor (яд + предсмертный взрыв).
            datas.Add(Make("Гнилостный Носитель", "PlagueBearer", typeof(EnemyAI),
                EnemyRole.Tank | EnemyRole.Disruptor, BehaviorType.Melee,
                hp: 80, dmg: 6, speed: 1.4f, detection: 100,
                root => root.AddComponent<PlagueBearer>()));

            // ④ Проклятый Чародей-Звено — Support (corruption link).
            datas.Add(Make("Чародей-Звено", "Hexbinder", typeof(SummonerEnemy),
                EnemyRole.Support, BehaviorType.Summoner,
                hp: 30, dmg: 3, speed: 1.8f, detection: 100,
                root => root.AddComponent<HexbinderCaster>()));

            // ⑤ Пожиратель Эха — Elite Assassin (Mirror+Temporal + деление).
            datas.Add(Make("Пожиратель Эха", "EchoDevourer", typeof(ChargerEnemy),
                EnemyRole.Assassin | EnemyRole.Elite, BehaviorType.Charger,
                hp: 50, dmg: 9, speed: 3.2f, detection: 100, root =>
                {
                    root.AddComponent<EliteModifier>().type = EliteModifierType.Mirror;
                    root.AddComponent<EliteModifier>().type = EliteModifierType.Temporal;
                    EvolutionController.AttachSplit(root, 0.5f, 2);
                }));

            // ⑥ Башня Скорби — Elite Tank/Siege (Gravity+Arcane + стены).
            datas.Add(Make("Башня Скорби", "TowerOfSorrow", typeof(EnemyAI),
                EnemyRole.Tank | EnemyRole.Siege | EnemyRole.Elite, BehaviorType.Melee,
                hp: 140, dmg: 10, speed: 0.8f, detection: 100, root =>
                {
                    root.AddComponent<EliteModifier>().type = EliteModifierType.Gravity;
                    root.AddComponent<EliteModifier>().type = EliteModifierType.Arcane;
                    root.AddComponent<SiegeBuilder>();
                }));

            WirePool(datas);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BestiaryBuilder] Готово: {datas.Count} врагов в {OutFolder}. " +
                      "EnemyData прописаны в Floor_Default.enemyPool — нажми Play.");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(OutFolder);
        }

        static EnemyData Make(string displayName, string fileName, Type aiType,
            EnemyRole roles, BehaviorType behavior,
            float hp, float dmg, float speed, float detection,
            Action<GameObject> addComponents)
        {
            string dataPath   = $"{OutFolder}/{fileName}.asset";
            string prefabPath = $"{OutFolder}/{fileName}.prefab";

            // 1. EnemyData (создаём/обновляем).
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(dataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<EnemyData>();
                AssetDatabase.CreateAsset(data, dataPath);
            }
            data.enemyName      = displayName;
            data.maxHealth      = hp;
            data.damage         = dmg;
            data.speed          = speed;
            data.detectionRange = detection;
            data.attackRange    = 0.9f;
            data.attackCooldown = 1.2f;
            data.behaviorType   = behavior;
            data.roles          = roles;
            data.goldReward     = Mathf.RoundToInt(hp * 0.1f);
            data.expReward      = Mathf.RoundToInt(hp * 0.15f);

            // 2. Префаб: клон базы → меняем AI → добавляем поведение → ставим data.
            var root = PrefabUtility.LoadPrefabContents(BasePrefab);
            var old = root.GetComponent<StateMachineEnemy>();
            if (old != null) UnityEngine.Object.DestroyImmediate(old);

            var ai = root.AddComponent(aiType);
            var so = new SerializedObject(ai);
            var dp = so.FindProperty("data");
            if (dp != null) dp.objectReferenceValue = data;
            so.ApplyModifiedPropertiesWithoutUndo();

            addComponents?.Invoke(root);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);

            // 3. Связать data → префаб.
            data.spritePrefab = prefab;
            EditorUtility.SetDirty(data);
            return data;
        }

        static void WirePool(System.Collections.Generic.List<EnemyData> datas)
        {
            var fc = AssetDatabase.LoadAssetAtPath<FloorConfig>(FloorConfig);
            if (fc == null)
            {
                Debug.LogWarning($"[BestiaryBuilder] {FloorConfig} не найден — добавь EnemyData в enemyPool вручную.");
                return;
            }
            fc.enemyPool = new System.Collections.Generic.List<EnemyData>(datas);
            EditorUtility.SetDirty(fc);
        }
    }
}
