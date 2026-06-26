using System.Collections.Generic;
using UnityEngine;
using Game.Dungeon;

namespace Enemy
{
    /// <summary>
    /// Ближний враг на единой FSM (<see cref="StateMachineEnemy"/>):
    /// Idle → Chase (A* по NavGrid) → Attack. Поведение идентично прежней версии,
    /// переходы выражены через канонические состояния.
    /// </summary>
    public class EnemyAI : StateMachineEnemy
    {
        [Header("A* погоня")]
        [SerializeField] float pathRefreshRate  = 0.1f;
        [SerializeField] float waypointReachDist = 0.25f;

        List<Vector2Int> _path = new List<Vector2Int>();
        int   _pathIdx;
        float _pathTimer;

        NavGrid Nav => RoomManager.Instance != null ? RoomManager.Instance.Nav : null;

        protected override EnemyState DecideState(float dist)
            => dist <= attackRange    ? EnemyState.Attack
             : dist <= detectionRange ? EnemyState.Chase
             :                          EnemyState.Idle;

        protected override void OnIdle()
        {
            StopMoving();
            _path.Clear();
        }

        protected override void OnChase()
        {
            _pathTimer -= Time.deltaTime;
            if (_pathTimer <= 0f || _path.Count == 0)
            {
                _pathTimer = pathRefreshRate;
                RefreshPath();
            }
            MoveAlongPath();
        }

        protected override void OnAttack()
        {
            StopMoving();
            FaceDirection(DirToPlayer);
            PerformAttack(Player);
        }

        // ── A* ──────────────────────────────────────────────────────────────────
        void RefreshPath()
        {
            var nav = Nav;
            if (nav == null || Player == null) return;

            Vector2Int fromCell = nav.WorldToCell(transform.position);
            Vector2Int toCell   = nav.WorldToCell(Player.position);

            _path    = GridPathfinder.FindPath(nav, fromCell, toCell);
            _pathIdx = 0;
        }

        void MoveAlongPath()
        {
            if (_path == null || _pathIdx >= _path.Count) { StopMoving(); return; }

            Vector3 target = Nav != null ? Nav.CellToWorldCenter(_path[_pathIdx]) : transform.position;
            Vector2 dir    = (target - transform.position).normalized;
            MoveInDirection(dir, moveSpeed * SpeedMul);

            if (Vector2.Distance(transform.position, target) < waypointReachDist)
                _pathIdx++;
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
