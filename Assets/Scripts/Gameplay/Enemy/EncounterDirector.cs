using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.Data;
using Game.Combat;
using Game.Dungeon;

namespace Enemy
{
    /// <summary>
    /// Режиссёр инкаунтера (дизайн Фаза 4 — синергии групп). После спавна сканирует
    /// роли врагов в комнате и включает совместную тактику: Фаланга, Стая, Загон,
    /// Осада, Капкан (+ Рой как базовая для bruiser-групп). Каждая синергия меняет
    /// поведение группы — баффы и/или периодические действия toolkit'ом (HazardZone,
    /// TempWall), а не только статы. Одинаковый набор врагов в разных комнатах
    /// играется по-разному.
    ///
    /// Роли берутся из EnemyData.roles, а если их нет — выводятся из компонентов
    /// (HexbinderCaster=Support, EliteModifier=Elite) и типа AI (fallback), чтобы
    /// синергии работали и до авторинга бестиария (Фаза 6).
    ///
    /// Управляется <see cref="RoomEncounter"/>: Begin после спавна, End при зачистке.
    /// </summary>
    public class EncounterDirector : MonoBehaviour
    {
        readonly List<StateMachineEnemy> _enemies = new List<StateMachineEnemy>();
        readonly Dictionary<EnemyRole, List<StateMachineEnemy>> _byRole
            = new Dictionary<EnemyRole, List<StateMachineEnemy>>();

        readonly List<ActiveSynergy> _active = new List<ActiveSynergy>();
        Transform _player;
        bool _running;

        // ── Жизненный цикл ──────────────────────────────────────────────────────
        public void Begin(List<GameObject> spawned)
        {
            _enemies.Clear();
            _byRole.Clear();
            _active.Clear();

            foreach (var go in spawned)
            {
                if (go == null) continue;
                var e = go.GetComponent<StateMachineEnemy>();
                if (e != null) _enemies.Add(e);
            }
            if (_enemies.Count < 2) return; // группа из 1 — не для синергий (босс и т.п.)

            foreach (var e in _enemies)
            {
                EnemyRole roles = RolesOf(e);
                foreach (EnemyRole r in AllRoles)
                    if ((roles & r) != 0) Bucket(r).Add(e);
            }

            var p = Game.Core.PlayerRef.Resolve();
            if (p != null) _player = p.transform;

            foreach (var s in Rules)
                if (s.Condition(this))
                {
                    s.OnActivate?.Invoke(this);
                    if (s.OnTick != null) _active.Add(new ActiveSynergy(s));
                    Debug.Log($"[EncounterDirector] Синергия активна: {s.Name}");
                }

            _running = true;
        }

        public void End() => _running = false;

        void Update()
        {
            if (!_running) return;
            for (int i = 0; i < _active.Count; i++)
            {
                var a = _active[i];
                a.Timer -= Time.deltaTime;
                if (a.Timer <= 0f) { a.Timer = a.Rule.Interval; a.Rule.OnTick(this); }
            }
        }

        // ── Доступ для правил ───────────────────────────────────────────────────
        public int Count(EnemyRole r) => _byRole.TryGetValue(r, out var l) ? l.Count : 0;
        public List<StateMachineEnemy> With(EnemyRole r)
            => _byRole.TryGetValue(r, out var l) ? l : Empty;
        public Transform PlayerTf => _player;

        static readonly List<StateMachineEnemy> Empty = new List<StateMachineEnemy>();
        List<StateMachineEnemy> Bucket(EnemyRole r)
        {
            if (!_byRole.TryGetValue(r, out var l)) { l = new List<StateMachineEnemy>(); _byRole[r] = l; }
            return l;
        }

        // ── Роли ────────────────────────────────────────────────────────────────
        static readonly EnemyRole[] AllRoles =
        {
            EnemyRole.Bruiser, EnemyRole.Tank, EnemyRole.Support, EnemyRole.Summoner,
            EnemyRole.Controller, EnemyRole.Assassin, EnemyRole.Hunter, EnemyRole.Disruptor,
            EnemyRole.Siege, EnemyRole.Elite,
        };

        static EnemyRole RolesOf(StateMachineEnemy e)
        {
            EnemyRole r = e.Roles;                                            // из EnemyData
            if (e.GetComponent<HexbinderCaster>() != null) r |= EnemyRole.Support;
            if (e.GetComponent<EliteModifier>()   != null) r |= EnemyRole.Elite;
            if ((r & ~EnemyRole.Elite) == 0) r |= InferFromType(e);          // fallback по типу AI
            return r;
        }

        static EnemyRole InferFromType(StateMachineEnemy e)
        {
            if (e is RangedEnemy)   return EnemyRole.Hunter;
            if (e is SummonerEnemy) return EnemyRole.Summoner;
            return EnemyRole.Bruiser;                                         // EnemyAI/Charger
        }

        // ── Применение эффектов ─────────────────────────────────────────────────
        static void Speed(List<StateMachineEnemy> list, float mult)
        {
            foreach (var e in list) if (e != null) e.SpeedBuff *= mult;
        }
        static void Damage(List<StateMachineEnemy> list, float mult)
        {
            foreach (var e in list) if (e != null) e.damageDealtMultiplier *= mult;
        }
        static void Shield(List<StateMachineEnemy> list, float keepFraction)
        {
            foreach (var e in list)
                if (e != null && e.IncomingDamageFilter == null)
                    e.IncomingDamageFilter = (amt, t) => amt * keepFraction;
        }

        Vector3 NearPlayer(float spread)
            => (_player != null ? _player.position : transform.position)
               + (Vector3)(Random.insideUnitCircle * spread);

        // ── Каталог синергий ────────────────────────────────────────────────────
        static readonly Synergy[] Rules =
        {
            // Фаланга: танк держит фронт (щит), хантеры бьют из-за спины.
            new Synergy("Фаланга", d => d.Count(EnemyRole.Tank) >= 1 &&
                                        d.Count(EnemyRole.Support) >= 1 &&
                                        d.Count(EnemyRole.Hunter) >= 1,
                d => { Shield(d.With(EnemyRole.Tank), 0.65f); Damage(d.With(EnemyRole.Hunter), 1.3f); }),

            // Стая: 2+ ассасина — синхронный быстрый заход.
            new Synergy("Стая", d => d.Count(EnemyRole.Assassin) >= 2,
                d => Speed(d.With(EnemyRole.Assassin), 1.35f)),

            // Загон: контроллер зонами гонит игрока к скоплению.
            new Synergy("Загон", d => d.Count(EnemyRole.Summoner) >= 1 &&
                                      d.Count(EnemyRole.Controller) >= 1,
                null, 3.5f,
                d => HazardZone.SpawnCircle(d.NearPlayer(2f), 1.5f, 3f, 3f, DamageType.Magic,
                        StatusEffect.Freeze, 0.8f, 0.5f)),

            // Осада: siege режет комнату стенами, хантеры простреливают коридор.
            new Synergy("Осада", d => d.Count(EnemyRole.Siege) >= 1 &&
                                      d.Count(EnemyRole.Hunter) >= 1,
                d => Damage(d.With(EnemyRole.Hunter), 1.2f), 5f,
                d => TempWall.Spawn(d.NearPlayer(2.5f), new Vector2(4f, 0.6f), 4f)),

            // Капкан: disruptor запирает, hunter гонит.
            new Synergy("Капкан", d => d.Count(EnemyRole.Disruptor) >= 1 &&
                                       d.Count(EnemyRole.Hunter) >= 1,
                d => Speed(d.With(EnemyRole.Hunter), 1.25f)),

            // Рой (база): 3+ bruiser давят согласованно.
            new Synergy("Рой", d => d.Count(EnemyRole.Bruiser) >= 3,
                d => { Speed(d.With(EnemyRole.Bruiser), 1.2f); Damage(d.With(EnemyRole.Bruiser), 1.15f); }),
        };

        // ── Правило (неизменяемое, общее для всех комнат) ────────────────────────
        class Synergy
        {
            public readonly string Name;
            public readonly System.Func<EncounterDirector, bool> Condition;
            public readonly System.Action<EncounterDirector> OnActivate;
            public readonly System.Action<EncounterDirector> OnTick;
            public readonly float Interval;

            public Synergy(string name, System.Func<EncounterDirector, bool> cond,
                System.Action<EncounterDirector> onActivate,
                float interval = 0f, System.Action<EncounterDirector> onTick = null)
            {
                Name = name; Condition = cond; OnActivate = onActivate;
                Interval = interval; OnTick = onTick;
            }
        }

        // ── Per-instance состояние таймера тика ──────────────────────────────────
        class ActiveSynergy
        {
            public readonly Synergy Rule;
            public float Timer;
            public ActiveSynergy(Synergy rule) { Rule = rule; Timer = rule.Interval; }
        }
    }
}
