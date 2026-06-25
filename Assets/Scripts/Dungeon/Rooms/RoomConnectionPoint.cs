using UnityEngine;

namespace Game.Dungeon
{
    /// <summary>
    /// Точка стыковки комнаты — «дверной сокет» (ТЗ ЭТАП 4 — RoomConnectionPoint).
    /// Дизайнер ставит её на дочерний трансформ у каждого прохода комнаты-префаба.
    /// direction задаёт, в какую сторону смотрит проход ОТНОСИТЕЛЬНО комнаты
    /// (до вращения). Ассемблер вращает комнату так, чтобы её свободный сокет
    /// смотрел в сторону, противоположную сокету родителя.
    /// </summary>
    public class RoomConnectionPoint : MonoBehaviour
    {
        [Tooltip("Сторона комнаты, куда смотрит этот проход (локально)")]
        public Direction direction = Direction.North;

        public DoorType doorType = DoorType.Normal;

        [Tooltip("Дверь на этом проходе (опц.). Управляется при бое.")]
        public DoorController door;

        /// <summary>Занят ли сокет (соединён с другой комнатой при сборке).</summary>
        [System.NonSerialized] public bool used;

        /// <summary>Мировое направление прохода с учётом текущего поворота комнаты.</summary>
        public Direction WorldDirection(int roomRotationStepsCW)
            => direction.Rotate(roomRotationStepsCW);

        void OnDrawGizmos()
        {
            Gizmos.color = used ? Color.gray : Color.green;
            Vector3 p = transform.position;
            Gizmos.DrawWireCube(p, Vector3.one * 0.4f);
            Gizmos.DrawLine(p, p + (Vector3)(Vector2)direction.ToVector());
        }
    }
}
