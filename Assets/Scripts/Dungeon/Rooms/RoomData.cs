using UnityEngine;

namespace Game.Dungeon
{
    /// <summary>
    /// Метаданные готовой комнаты-префаба (ТЗ «Система комнат» — RoomData).
    ///
    /// ВАЖНО (новая архитектура): сами точки спавна (враги/сундуки/декорации) и
    /// двери живут НА ПРЕФАБЕ как дочерние трансформы с компонентами
    /// <see cref="RoomConnectionPoint"/> / <see cref="SpawnPoint"/>, потому что
    /// ScriptableObject не может ссылаться на сценовые трансформы. RoomData хранит
    /// метаданные и ссылку на префаб; точки собирает <see cref="RoomInstance"/>
    /// в рантайме. Так дизайнер расставляет всё визуально внутри префаба.
    /// </summary>
    [CreateAssetMenu(menuName = "TEW/Rooms/RoomData", fileName = "Room_")]
    public class RoomData : ScriptableObject
    {
        [Header("Идентификация")]
        [SerializeField] string roomID;
        public RoomType roomType = RoomType.Normal;

        [Header("Префаб (содержит визуал, двери, точки спавна)")]
        [Tooltip("Корень префаба должен иметь компонент RoomInstance")]
        public GameObject prefab;

        [Header("Подбор / веса")]
        [Tooltip("Вес сложности — для баланса этажа")]
        public float difficultyWeight = 1f;
        [Tooltip("Вес появления в рулетке выбора префаба")]
        public float spawnWeight = 1f;

        [Header("Диапазон этажей")]
        public int minFloor = 1;
        public int maxFloor = 999;

        [Header("Сборка")]
        [Tooltip("Можно ли вращать комнату на ×90° для стыковки дверей")]
        public bool canRotate = true;

        public string RoomID => string.IsNullOrEmpty(roomID) ? name : roomID;

        public bool FitsFloor(int floor) => floor >= minFloor && floor <= maxFloor;
    }
}
