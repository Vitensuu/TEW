using UnityEngine;
using Game.Core;
using Game.Combat;
using Game.Dungeon;

namespace Enemy
{
    /// <summary>
    /// Способность «Хладного Глашатая» (роль Controller, Фаза 6): периодически
    /// бросает ледяную <see cref="HazardZone"/> под игрока (урон + Freeze с
    /// телеграфом) и изредка поднимает ледяную <see cref="TempWall"/>, режущую
    /// комнату. Компонуемый — вешается на дальнобойного врага (RangedEnemy).
    /// </summary>
    public class FrostHerald : MonoBehaviour
    {
        [SerializeField] float castInterval = 3f;
        [SerializeField] float range        = 9f;
        [SerializeField] float wallInterval = 9f;

        EnemyBase _eb;
        Transform _player;
        float _t, _wallT;

        void Awake() => _eb = GetComponent<EnemyBase>();

        void Start()
        {
            var p = Game.Core.PlayerRef.Resolve();
            if (p != null) _player = p.transform;
            _t = castInterval; _wallT = wallInterval;
        }

        void Update()
        {
            if (_eb == null || _eb.Dead || _player == null) return;
            if (Vector2.Distance(transform.position, _player.position) > range) return;

            _t -= Time.deltaTime;
            if (_t <= 0f)
            {
                _t = castInterval;
                HazardZone.SpawnCircle(_player.position, 1.5f, 4f, 3f, DamageType.Ice,
                    StatusEffect.Freeze, 1f, 0.5f, startDelay: 0.5f);   // 0.5с телеграф
            }

            _wallT -= Time.deltaTime;
            if (_wallT <= 0f)
            {
                _wallT = wallInterval;
                Vector3 mid = (transform.position + _player.position) * 0.5f;
                TempWall.Spawn(mid, new Vector2(3.5f, 0.6f), 5f);
            }
        }
    }
}
