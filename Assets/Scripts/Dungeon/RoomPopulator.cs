using System.Collections.Generic;
using UnityEngine;
using Dungeon.Procedural;

/// <summary>
/// Спавнит врагов в комнатах после генерации подземелья.
/// Повесь на тот же GameObject что и DungeonGenerator.
/// Вызывается генератором НЕ автоматически — дёрни Populate() после Generate(),
/// либо включи autoPopulateOnStart.
/// </summary>
[RequireComponent(typeof(DungeonGenerator))]
public class RoomPopulator : MonoBehaviour
{
    [Header("Враги")]
    [SerializeField] GameObject[] enemyPrefabs;
    [SerializeField] int minEnemiesPerRoom = 1;
    [SerializeField] int maxEnemiesPerRoom = 3;

    [Header("Правила спавна")]
    [Tooltip("Не спавнить врагов в стартовой комнате (индекс 0)")]
    [SerializeField] bool skipStartRoom = true;
    [Tooltip("Отступ от краёв комнаты, в мировых единицах")]
    [SerializeField] float spawnPadding = 1.5f;

    [Header("Запуск")]
    [SerializeField] bool autoPopulateOnStart = true;
    [Tooltip("Задержка кадров — дать генератору построиться")]
    [SerializeField] int startupFrameDelay = 1;

    DungeonGenerator _generator;
    readonly List<GameObject> _spawned = new List<GameObject>();

    void Awake() => _generator = GetComponent<DungeonGenerator>();

    void Start()
    {
        if (autoPopulateOnStart) StartCoroutine(PopulateNextFrame());
    }

    System.Collections.IEnumerator PopulateNextFrame()
    {
        for (int i = 0; i < Mathf.Max(1, startupFrameDelay); i++)
            yield return null;
        Populate();
    }

    /// <summary>Заспавнить врагов по всем комнатам (кроме стартовой).</summary>
    public void Populate()
    {
        foreach (var go in _spawned)
            if (go != null) Destroy(go);
        _spawned.Clear();

        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;

        var rooms = _generator.GetRooms();
        if (rooms == null || rooms.Count == 0) return;

        for (int i = 0; i < rooms.Count; i++)
        {
            if (skipStartRoom && i == 0) continue;

            int count = Random.Range(minEnemiesPerRoom, maxEnemiesPerRoom + 1);
            SpawnInRoom(rooms[i], count);
        }
    }

    void SpawnInRoom(Room room, int count)
    {
        Bounds b = room.WorldBounds;
        float xMin = b.min.x + spawnPadding;
        float xMax = b.max.x - spawnPadding;
        float yMin = b.min.y + spawnPadding;
        float yMax = b.max.y - spawnPadding;

        if (xMin >= xMax || yMin >= yMax) return;

        for (int i = 0; i < count; i++)
        {
            var pos = new Vector3(
                Random.Range(xMin, xMax),
                Random.Range(yMin, yMax), 0f);

            var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            _spawned.Add(Instantiate(prefab, pos, Quaternion.identity));
        }
    }
}
