using System.Collections.Generic;
using UnityEngine;
using Game.Dungeon;

namespace Enemy
{
    public class EnemyAI : EnemyBase
    {
        [Header("AI")]
        [SerializeField] float moveSpeed       = 2.5f;
        [SerializeField] float detectionRange  = 7f;
        [SerializeField] float pathRefreshRate = 0.4f;
        [SerializeField] float waypointReachDist = 0.25f;

        enum State { Idle, Chase, Attack }

        State                _state;
        Transform            _player;
        List<Vector2Int>     _path    = new List<Vector2Int>();
        int                  _pathIdx;
        float                _pathTimer;
        Game.Combat.StatusEffectHandler _status;

        protected override void Awake()
        {
            base.Awake();
            _status = GetComponent<Game.Combat.StatusEffectHandler>();
        }

        void Start()
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null) _player = playerGO.transform;
        }

        NavGrid Nav => RoomManager.Instance != null ? RoomManager.Instance.Nav : null;

        protected override void Update()
        {
            base.Update();
            if (IsDead || _player == null) return;

            // Stun (ТЗ §4): полная остановка, не двигаемся и не атакуем.
            if (_status != null && _status.IsStunned)
            {
                Rb.linearVelocity = Vector2.zero;
                SetMoving(Vector2.zero);
                return;
            }

            float dist = Vector2.Distance(transform.position, _player.position);

            _state = dist <= attackRange   ? State.Attack
                   : dist <= detectionRange ? State.Chase
                   : State.Idle;

            switch (_state)
            {
                case State.Idle:   DoIdle();   break;
                case State.Chase:  DoChase();  break;
                case State.Attack: DoAttack(); break;
            }
        }

        // ── Idle ──────────────────────────────────────────────────────────────

        void DoIdle()
        {
            Rb.linearVelocity = Vector2.zero;
            SetMoving(Vector2.zero);
            _path.Clear();
        }

        // ── Chase ─────────────────────────────────────────────────────────────

        void DoChase()
        {
            _pathTimer -= Time.deltaTime;
            if (_pathTimer <= 0f || _path.Count == 0)
            {
                _pathTimer = pathRefreshRate;
                RefreshPath();
            }

            MoveAlongPath();
        }

        void RefreshPath()
        {
            var nav = Nav;
            if (nav == null || _player == null) return;

            // Конвертируем world → grid cell (NavGrid строится из реальной геометрии комнат)
            Vector2Int fromCell = nav.WorldToCell(transform.position);
            Vector2Int toCell   = nav.WorldToCell(_player.position);

            _path    = GridPathfinder.FindPath(nav, fromCell, toCell);
            _pathIdx = 0;
        }

        void MoveAlongPath()
        {
            if (_path == null || _pathIdx >= _path.Count)
            {
                Rb.linearVelocity = Vector2.zero;
                SetMoving(Vector2.zero);
                return;
            }

            Vector3 target = Nav != null ? Nav.CellToWorldCenter(_path[_pathIdx]) : transform.position;
            Vector2 dir    = (target - transform.position).normalized;
            float speedMul = _status != null ? _status.SpeedMultiplier : 1f; // Freeze (ТЗ §4)
            Rb.linearVelocity = dir * moveSpeed * speedMul;
            SetMoving(dir);

            if (Vector2.Distance(transform.position, target) < waypointReachDist)
                _pathIdx++;
        }

        // ── Attack ────────────────────────────────────────────────────────────

        void DoAttack()
        {
            Rb.linearVelocity = Vector2.zero;
            SetMoving(Vector2.zero);
            // Смотрим в сторону игрока во время атаки
            Vector2 toPlayer = (_player.position - transform.position).normalized;
            SetDirection(toPlayer);
            PerformAttack(_player);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
