using System.Collections.Generic;
using UnityEngine;
using Dungeon;

/// <summary>
/// Спавнит врагов в комнатах после генерации подземелья.
/// Добавь на тот же GameObject что и DungeonGenerator.
/// </summary>
public class RoomPopulator : MonoBehaviour
{
    [Header("Враги")]
    [SerializeField] GameObject[] enemyPrefabs;
    [SerializeField] int minEnemiesPerRoom = 1;
    [SerializeField] int maxEnemiesPerRoom = 3;

    [Header("Стартовая комната")]
    [Tooltip("Индекс комнаты где спавнится игрок — туда врагов не ставим")]
    [SerializeField] int startRoomIndex = 0;

    [Header("Отступ от стен (в тайлах)")]
    [SerializeField] int spawnPadding = 2;

    DungeonGenerator _generator;
    readonly List<GameObject> _spawned = new List<GameObject>();

    void Awake() => _generator = GetComponent<DungeonGenerator>();

    /// <summary>Вызови после DungeonGenerator.Generate()</summary>
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
            if (i == startRoomIndex) continue;

            int count = Random.Range(minEnemiesPerRoom, maxEnemiesPerRoom + 1);
            SpawnInRoom(rooms[i], count);
        }
    }

    void SpawnInRoom(RoomInfo room, int count)
    {
        var area = room.Area;
        int xMin = area.x + spawnPadding;
        int xMax = area.xMax - spawnPadding - 1;
        int yMin = area.y + spawnPadding;
        int yMax = area.yMax - spawnPadding - 1;

        if (xMin > xMax || yMin > yMax) return;

        // Получаем tilemap для перевода клетки в мировые координаты
        var tilemap = _generator.FloorTilemap;

        for (int i = 0; i < count; i++)
        {
            int tx = Random.Range(xMin, xMax + 1);
            int ty = Random.Range(yMin, yMax + 1);

            Vector3 worldPos = tilemap != null
                ? tilemap.GetCellCenterWorld(new Vector3Int(tx, ty, 0))
                : new Vector3(tx + 0.5f, ty + 0.5f, 0f);

            var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            var go = Instantiate(prefab, worldPos, Quaternion.identity);
            _spawned.Add(go);
        }
    }
}
