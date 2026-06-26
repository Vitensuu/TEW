using UnityEngine;
using Game.Dungeon;

namespace Enemy
{
    /// <summary>
    /// Способность «осадного» врага (роль Siege, Фаза 6): периодически возводит
    /// <see cref="TempWall"/> рядом с игроком, перестраивая комнату и заставляя
    /// обходить (A* учитывает стены через RoomManager.RefreshNav). Используется
    /// «Башней Скорби»; переиспользуется боссом в Фазе 7.
    /// </summary>
    public class SiegeBuilder : MonoBehaviour
    {
        [SerializeField] float buildInterval = 6f;
        [SerializeField] float range         = 10f;
        [SerializeField] Vector2 wallSize    = new Vector2(4f, 0.6f);
        [SerializeField] float wallLifetime  = 5f;

        EnemyBase _eb;
        Transform _player;
        float _t;

        void Awake() => _eb = GetComponent<EnemyBase>();

        void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _player = p.transform;
            _t = buildInterval;
        }

        void Update()
        {
            if (_eb == null || _eb.Dead || _player == null) return;
            if (Vector2.Distance(transform.position, _player.position) > range) return;

            _t -= Time.deltaTime;
            if (_t <= 0f)
            {
                _t = buildInterval;
                Vector2 dir = Random.insideUnitCircle.normalized;
                if (dir == Vector2.zero) dir = Vector2.right;
                Vector3 near = _player.position + (Vector3)(dir * 2.5f);
                TempWall.Spawn(near, wallSize, wallLifetime);
            }
        }
    }
}
