using System.Collections.Generic;
using UnityEngine;
using Dungeon;
using Dungeon.Procedural;

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
        DungeonGenerator     _gen;
        DungeonGrid          _grid;
        List<Vector2Int>     _path    = new List<Vector2Int>();
        int                  _pathIdx;
        float                _pathTimer;

        protected override void Awake()
        {
            base.Awake();
        }

        void Start()
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null) _player = playerGO.transform;

            StartCoroutine(InitGridNextFrame());
        }

        System.Collections.IEnumerator InitGridNextFrame()
        {
            // Ждём один кадр — DungeonGenerator точно успеет сгенерировать
            yield return null;
            _gen = FindFirstObjectByType<DungeonGenerator>();
            if (_gen != null) _grid = _gen.GetGrid();
        }

        protected override void Update()
        {
            base.Update();
            if (IsDead || _player == null) return;

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
            if (_grid == null || _player == null) return;

            // Конвертируем world → grid cell
            Vector2Int fromCell = WorldToCell(transform.position);
            Vector2Int toCell   = WorldToCell(_player.position);

            _path    = GridPathfinder.FindPath(_grid, fromCell, toCell);
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

            Vector3 target = CellToWorld(_path[_pathIdx]);
            Vector2 dir    = (target - transform.position).normalized;
            Rb.linearVelocity = dir * moveSpeed;
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

        // ── Вспомогательное ───────────────────────────────────────────────────

        // Конвертация через генератор — он знает смещение сетки (_gridOrigin)
        Vector2Int WorldToCell(Vector3 world)
            => _gen != null
                ? _gen.WorldToCell(world)
                : new Vector2Int(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y));

        Vector3 CellToWorld(Vector2Int cell)
            => _gen != null
                ? _gen.CellToWorldCenter(cell)
                : new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
