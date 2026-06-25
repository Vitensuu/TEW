using UnityEngine;

namespace Game.Dungeon
{
    /// <summary>
    /// Что обозначает точка спавна внутри комнаты-префаба.
    /// Дизайнер расставляет пустые трансформы с этим компонентом
    /// (ТЗ: точки спавна врагов/сундуков/NPC/входа/выхода/декораций).
    /// </summary>
    public enum SpawnKind { Enemy, Chest, NPC, Decoration, Trap, Entry, Exit }

    /// <summary>
    /// Маркер-точка спавна. Висит на дочернем пустом GameObject внутри
    /// Room Prefab. <see cref="RoomInstance"/> собирает все такие точки.
    /// </summary>
    public class SpawnPoint : MonoBehaviour
    {
        public SpawnKind kind = SpawnKind.Enemy;

        [Tooltip("Опц.: вес/приоритет точки (например, шанс что враг тут появится)")]
        [Range(0f, 1f)] public float chance = 1f;

        void OnDrawGizmos()
        {
            Gizmos.color = kind switch
            {
                SpawnKind.Enemy      => Color.red,
                SpawnKind.Chest      => Color.yellow,
                SpawnKind.NPC        => Color.cyan,
                SpawnKind.Decoration => new Color(0.5f, 0.5f, 0.5f),
                SpawnKind.Trap       => Color.magenta,
                SpawnKind.Entry      => Color.green,
                SpawnKind.Exit       => Color.blue,
                _                    => Color.white,
            };
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}
