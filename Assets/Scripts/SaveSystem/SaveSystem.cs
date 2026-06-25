using System.IO;
using UnityEngine;
using Game.Core;

namespace Game.Save
{
    /// <summary>
    /// Сериализация мета-данных (ТЗ §5 — SaveSystem, паттерн Service).
    /// MetaSaveData.json через JsonUtility в Application.persistentDataPath.
    /// Живёт как DontDestroyOnLoad-синглтон; грузит сейв в Awake.
    /// </summary>
    public class SaveSystem : Singleton<SaveSystem>
    {
        const string FileName = "MetaSaveData.json";

        public MetaSaveData Meta { get; private set; } = new MetaSaveData();

        string Path => System.IO.Path.Combine(Application.persistentDataPath, FileName);

        protected override void Awake()
        {
            base.Awake();
            if (Instance == this) Load();
        }

        // ── Загрузка / сохранение ───────────────────────────────────────────────
        public void Load()
        {
            try
            {
                if (File.Exists(Path))
                {
                    string json = File.ReadAllText(Path);
                    Meta = JsonUtility.FromJson<MetaSaveData>(json) ?? new MetaSaveData();
                }
                else Meta = new MetaSaveData();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Не удалось загрузить сейв: {e.Message}");
                Meta = new MetaSaveData();
            }
        }

        public void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(Meta, true);
                File.WriteAllText(Path, json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Не удалось сохранить: {e.Message}");
            }
        }

        // ── Осколки душ (мета-валюта) ───────────────────────────────────────────
        public int SoulShards => Meta.soulShards;

        public void AddSoulShards(int amount)
        {
            if (amount <= 0) return;
            Meta.soulShards += amount;
            Save();
            EventBus.TriggerSoulShardsChanged(Meta.soulShards);
        }

        public bool SpendSoulShards(int amount)
        {
            if (Meta.soulShards < amount) return false;
            Meta.soulShards -= amount;
            Save();
            EventBus.TriggerSoulShardsChanged(Meta.soulShards);
            return true;
        }

        // ── Разблокировки ───────────────────────────────────────────────────────
        public void UnlockCharacter(string id)
        {
            if (!Meta.unlockedCharacters.Contains(id))
            {
                Meta.unlockedCharacters.Add(id);
                Save();
            }
        }

        public void UnlockItem(string id)
        {
            if (!Meta.unlockedItems.Contains(id))
            {
                Meta.unlockedItems.Add(id);
                Save();
            }
        }

        public void RecordRunResult(int floorReached, int enemiesKilled)
        {
            Meta.totalRuns++;
            Meta.totalEnemiesKilled += enemiesKilled;
            if (floorReached > Meta.bestFloor) Meta.bestFloor = floorReached;
            Save();
        }

        [ContextMenu("Delete Save")]
        public void DeleteSave()
        {
            if (File.Exists(Path)) File.Delete(Path);
            Meta = new MetaSaveData();
        }
    }
}
