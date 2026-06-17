using UnityEngine;

namespace Dungeon
{
    /// <summary>
    /// Висит на объекте "выхода". При входе игрока вызывает перегенерацию
    /// подземелья (следующий этаж). Привязывается к игроку через Init(),
    /// поэтому не требует тега "Player" — но если тег есть, он тоже сработает.
    /// </summary>
    public class NextFloorTrigger : MonoBehaviour
    {
        private DungeonGenerator _generator;
        private Transform _player;
        private bool _used;

        public void Init(DungeonGenerator generator, Transform player)
        {
            _generator = generator;
            _player = player;
            _used = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_used) return;

            bool isPlayer = (_player != null && other.transform == _player) || other.CompareTag("Player");
            if (!isPlayer) return;

            _used = true;
            _generator.GenerateNextFloor();
        }
    }
}
