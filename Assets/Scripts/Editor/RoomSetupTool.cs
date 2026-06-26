using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Game.Dungeon;
using Game.Data;

namespace Game.DungeonEditor
{
    /// <summary>
    /// Одноразовый помощник: превращает «голые» визуальные префабы из Assets/maps
    /// в рабочие комнаты новой системы. Добавляет RoomInstance на корень каждого
    /// префаба, создаёт RoomData-ассет и собирает RoomLibrary. Слой Obstacle
    /// создаётся автоматически. Типы комнат назначаются по умолчанию — поправь
    /// вручную в инспекторе при необходимости.
    ///
    /// Меню: TEW ▸ Rooms ▸ Setup Rooms From 'maps' Folder
    /// </summary>
    public static class RoomSetupTool
    {
        const string MapsFolder  = "Assets/maps";
        const string DataFolder  = "Assets/maps/RoomData";
        const string LibraryPath = "Assets/maps/RoomData/RoomLibrary.asset";
        const string ObstacleLayer = "Obstacle";

        [MenuItem("TEW/Rooms/Setup Rooms From 'maps' Folder")]
        public static void Setup()
        {
            EnsureLayer(ObstacleLayer);

            if (!AssetDatabase.IsValidFolder(DataFolder))
                AssetDatabase.CreateFolder(MapsFolder, "RoomData");

            // Собираем префабы из maps/ (по порядку имён).
            var prefabPaths = new List<string>();
            foreach (var g in AssetDatabase.FindAssets("t:Prefab", new[] { MapsFolder }))
                prefabPaths.Add(AssetDatabase.GUIDToAssetPath(g));
            prefabPaths.Sort();

            if (prefabPaths.Count == 0)
            {
                Debug.LogWarning("[RoomSetupTool] В Assets/maps нет префабов.");
                return;
            }

            var datas = new List<RoomData>();
            for (int i = 0; i < prefabPaths.Count; i++)
            {
                string path = prefabPaths[i];

                // 1. RoomInstance на корень префаба.
                var root = PrefabUtility.LoadPrefabContents(path);
                if (root.GetComponent<RoomInstance>() == null)
                    root.AddComponent<RoomInstance>();
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);

                var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                // 2. RoomData-ассет.
                string assetName = "Room_" + prefabAsset.name.Replace(" ", "_");
                string dataPath = $"{DataFolder}/{assetName}.asset";
                var rd = AssetDatabase.LoadAssetAtPath<RoomData>(dataPath);
                if (rd == null)
                {
                    rd = ScriptableObject.CreateInstance<RoomData>();
                    AssetDatabase.CreateAsset(rd, dataPath);
                }
                rd.prefab = prefabAsset;
                rd.roomType = GuessType(i, prefabPaths.Count);
                EditorUtility.SetDirty(rd);
                datas.Add(rd);
            }

            // 3. RoomLibrary.
            var lib = AssetDatabase.LoadAssetAtPath<RoomRegistry>(LibraryPath);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<RoomRegistry>();
                AssetDatabase.CreateAsset(lib, LibraryPath);
            }
            var so = new SerializedObject(lib);
            var arr = so.FindProperty("rooms");
            arr.ClearArray();
            for (int i = 0; i < datas.Count; i++)
            {
                arr.InsertArrayElementAtIndex(i);
                arr.GetArrayElementAtIndex(i).objectReferenceValue = datas[i];
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(lib);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[RoomSetupTool] Готово: {datas.Count} RoomData + RoomLibrary ({LibraryPath}).\n" +
                      "Назначь RoomLibrary в DungeonAssembler, проверь типы комнат и слой Obstacle в obstacleMask.");
            Selection.activeObject = lib;
        }

        const string FloorConfigPath = "Assets/maps/RoomData/Floor_Default.asset";

        /// <summary>
        /// Собирает рабочую сборочную систему в ТЕКУЩЕЙ открытой сцене (например test1):
        /// объект DungeonSystem с DungeonAssembler + RoomManager + RoomPopulator,
        /// прописывает RoomLibrary, слой Obstacle, игрока и дефолтный FloorConfig.
        /// Удаляет «битые» (missing-script) компоненты со сцены.
        /// </summary>
        [MenuItem("TEW/Rooms/Build Dungeon System In Active Scene")]
        public static void BuildInActiveScene()
        {
            // Гарантируем, что библиотека и слой готовы (запускаем Setup при необходимости).
            EnsureLayer(ObstacleLayer);
            var lib = AssetDatabase.LoadAssetAtPath<RoomRegistry>(LibraryPath);
            if (lib == null)
            {
                Debug.Log("[RoomSetupTool] RoomLibrary не найдена — запускаю Setup из maps/.");
                Setup();
                lib = AssetDatabase.LoadAssetAtPath<RoomRegistry>(LibraryPath);
                if (lib == null) { Debug.LogError("[RoomSetupTool] Нет RoomLibrary. Прерываю."); return; }
            }

            var floor = EnsureDefaultFloorConfig();

            // Чистим старые missing-script компоненты (бывший DungeonGenerator и т.п.).
            int removed = 0;
            foreach (var go in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (removed > 0) Debug.Log($"[RoomSetupTool] Удалено missing-script компонентов: {removed}.");

            // Создаём (или находим) объект системы.
            var existing = GameObject.Find("DungeonSystem");
            var sysGO = existing != null ? existing : new GameObject("DungeonSystem");

            var assembler = sysGO.GetComponent<DungeonAssembler>() ?? sysGO.AddComponent<DungeonAssembler>();
            var populator = sysGO.GetComponent<RoomPopulator>() ?? sysGO.AddComponent<RoomPopulator>();
            var manager   = sysGO.GetComponent<RoomManager>()   ?? sysGO.AddComponent<RoomManager>();

            // Прописываем поля ассемблера.
            var aso = new SerializedObject(assembler);
            SetObj(aso, "library", lib);
            SetMask(aso, "obstacleMask", ObstacleLayer);
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null) SetObj(aso, "player", playerGO.transform);
            var fcArr = aso.FindProperty("floorConfigs");
            if (fcArr != null) { fcArr.ClearArray(); fcArr.InsertArrayElementAtIndex(0);
                fcArr.GetArrayElementAtIndex(0).objectReferenceValue = floor; }
            aso.ApplyModifiedProperties();

            // Поля менеджера.
            var mso = new SerializedObject(manager);
            SetObj(mso, "assembler", assembler);
            SetObj(mso, "populator", populator);
            mso.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(sysGO.scene);
            Selection.activeObject = sysGO;
            Debug.Log("[RoomSetupTool] DungeonSystem собран в активной сцене. " +
                      (playerGO == null ? "ВНИМАНИЕ: не найден объект с тегом Player — назначь вручную. " : "") +
                      "Нажми Play.");
        }

        static FloorConfig EnsureDefaultFloorConfig()
        {
            var fc = AssetDatabase.LoadAssetAtPath<FloorConfig>(FloorConfigPath);
            if (fc != null) return fc;

            fc = ScriptableObject.CreateInstance<FloorConfig>();
            fc.floorNumber = 1;
            fc.minRooms = 3;
            fc.maxRooms = 5;
            fc.hasBoss = false;          // на 1-м этаже без босса (под имеющиеся типы комнат)
            fc.shopChance = 0f;
            fc.shrineChance = 0f;
            fc.treasureChance = 0f;
            fc.minEnemiesPerRoom = 2;
            fc.maxEnemiesPerRoom = 4;
            AssetDatabase.CreateAsset(fc, FloorConfigPath);
            EditorUtility.SetDirty(fc);
            return fc;
        }

        static void SetObj(SerializedObject so, string prop, Object value)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.objectReferenceValue = value;
        }

        static void SetMask(SerializedObject so, string prop, string layerName)
        {
            var p = so.FindProperty(prop);
            int layer = LayerMask.NameToLayer(layerName);
            if (p != null && layer >= 0) p.intValue = 1 << layer;
        }

        // Типы по умолчанию: первая = Start, последняя = Exit, предпоследняя = Boss,
        // остальные = Normal. Минимум для валидного этажа.
        static RoomType GuessType(int i, int total)
        {
            if (i == 0)           return RoomType.Start;
            if (i == total - 1)   return RoomType.Exit;
            if (i == total - 2)   return RoomType.Boss;
            return RoomType.Normal;
        }

        static void EnsureLayer(string layerName)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;

            var tagManager = new SerializedObject(assets[0]);
            var layers = tagManager.FindProperty("layers");
            if (layers == null) return;

            for (int i = 0; i < layers.arraySize; i++)
                if (layers.GetArrayElementAtIndex(i).stringValue == layerName) return;

            // Пользовательские слои начинаются с индекса 8.
            for (int i = 8; i < layers.arraySize; i++)
            {
                var el = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(el.stringValue))
                {
                    el.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    Debug.Log($"[RoomSetupTool] Создан слой '{layerName}' (индекс {i}).");
                    return;
                }
            }
            Debug.LogWarning($"[RoomSetupTool] Нет свободного слота для слоя '{layerName}'. Создай вручную.");
        }
    }
}
