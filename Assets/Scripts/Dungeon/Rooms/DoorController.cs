using UnityEngine;

namespace Game.Dungeon
{
    /// <summary>
    /// Дверь комнаты (ТЗ «Система дверей»): направление, тип, состояние.
    /// Open/Closed/Locked управляют блокирующим коллайдером и визуалом.
    /// Закрывается на время Encounter, открывается после зачистки
    /// (см. <see cref="RoomEncounter"/>).
    /// </summary>
    public class DoorController : MonoBehaviour
    {
        [Header("Параметры")]
        public Direction direction = Direction.North;
        public DoorType  type      = DoorType.Normal;
        [SerializeField] DoorState state = DoorState.Open;

        [Header("Визуал/коллизия")]
        [Tooltip("Коллайдер, который перекрывает проход когда дверь закрыта")]
        [SerializeField] Collider2D blocker;
        [Tooltip("Объект открытой двери (вкл. при Open)")]
        [SerializeField] GameObject openVisual;
        [Tooltip("Объект закрытой двери (вкл. при Closed/Locked)")]
        [SerializeField] GameObject closedVisual;

        public DoorState State => state;
        public bool IsPassable => state == DoorState.Open;

        void Awake()
        {
            if (blocker == null) blocker = GetComponent<Collider2D>();
            Apply();
        }

        public void Open()  { if (type != DoorType.Locked || state != DoorState.Locked) Set(DoorState.Open); }
        public void Close() => Set(DoorState.Closed);
        public void Lock()  => Set(DoorState.Locked);

        /// <summary>Разблокировать запертую дверь (ключ/событие).</summary>
        public void Unlock() => Set(DoorState.Open);

        void Set(DoorState s)
        {
            state = s;
            Apply();
        }

        void Apply()
        {
            // Блокер активен когда дверь не проходима.
            if (blocker != null) blocker.enabled = !IsPassable;
            if (openVisual   != null) openVisual.SetActive(IsPassable);
            if (closedVisual != null) closedVisual.SetActive(!IsPassable);
        }
    }
}
