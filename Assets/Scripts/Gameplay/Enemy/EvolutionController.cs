using System.Collections.Generic;
using UnityEngine;
using Game.Core;

namespace Enemy
{
    public enum EvolutionTrigger { HpBelow, CombatTime, AllyDeath }

    /// <summary>
    /// Одна стадия эволюции (дизайн Фаза 5). Срабатывает один раз по своему триггеру
    /// и применяет эффекты. Анимации не трогает — меняет статы/множители/тинт и
    /// при необходимости делится на копии.
    /// </summary>
    [System.Serializable]
    public class EvolutionStage
    {
        public string name = "Stage";

        [Header("Триггер")]
        public EvolutionTrigger trigger = EvolutionTrigger.HpBelow;
        [Range(0f, 1f)] public float hpThreshold = 0.5f; // для HpBelow
        public float combatTime = 10f;                   // для CombatTime (сек боя)
        public int   allyDeaths = 1;                     // для AllyDeath

        [Header("Эффект")]
        public float maxHpMult  = 1f;
        public bool  healToFull = false;
        public float damageMult = 1f;
        public float speedMult  = 1f;
        [Tooltip("Сколько копий породить (деление); 0 — без деления")]
        public int   splitCount = 0;
        public bool  useTint = false;
        public Color tint = Color.white;

        [System.NonSerialized] public bool fired;
    }

    /// <summary>
    /// Контроллер эволюции (дизайн Фаза 5): враг мутирует прямо в бою по триггерам
    /// (порог HP, время боя, смерть союзников). Компонуемый (как EliteModifier):
    /// вешается на любого врага, конфигурируется списком стадий в инспекторе или
    /// пресетами <see cref="AttachEnrage"/>/<see cref="AttachSplit"/>.
    /// Деление клонирует самого врага (без дальнейшей эволюции у копий).
    /// </summary>
    public class EvolutionController : MonoBehaviour
    {
        [SerializeField] List<EvolutionStage> stages = new List<EvolutionStage>();

        EnemyBase _eb;
        StateMachineEnemy _sm;
        float _time;
        int   _allyDeaths;
        bool  _disabled;

        void Awake()
        {
            _eb = GetComponent<EnemyBase>();
            _sm = _eb as StateMachineEnemy;
        }

        void OnEnable()  => EventBus.OnEnemyKilled += OnAllyKilled;
        void OnDisable() => EventBus.OnEnemyKilled -= OnAllyKilled;

        void OnAllyKilled(Game.Core.IEnemy e)
        {
            if (e != null && !ReferenceEquals(e, _eb)) _allyDeaths++;
        }

        void Update()
        {
            if (_disabled || _eb == null || _eb.Dead) return;
            _time += Time.deltaTime;

            for (int i = 0; i < stages.Count; i++)
            {
                var s = stages[i];
                if (s.fired || !Triggered(s)) continue;
                s.fired = true;
                Evolve(s);
            }
        }

        bool Triggered(EvolutionStage s) => s.trigger switch
        {
            EvolutionTrigger.HpBelow    => _eb.CurrentHp / Mathf.Max(1f, _eb.MaxHp) <= s.hpThreshold,
            EvolutionTrigger.CombatTime => _time >= s.combatTime,
            EvolutionTrigger.AllyDeath  => _allyDeaths >= s.allyDeaths,
            _                           => false,
        };

        void Evolve(EvolutionStage s)
        {
            if (s.maxHpMult != 1f || s.healToFull) _eb.EvolveStats(s.maxHpMult, s.healToFull);
            if (s.damageMult != 1f) _eb.damageDealtMultiplier *= s.damageMult;
            if (s.speedMult  != 1f && _sm != null) _sm.SpeedBuff *= s.speedMult;
            if (s.useTint)
            {
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = s.tint;
            }
            for (int i = 0; i < s.splitCount; i++) SpawnSplit();
        }

        void SpawnSplit()
        {
            Vector3 pos = transform.position + (Vector3)(Random.insideUnitCircle * 1.2f);
            var clone = Instantiate(gameObject, pos, transform.rotation);
            clone.name = gameObject.name + "_split";
            clone.transform.localScale = transform.localScale * 0.8f;

            // Копии не делятся дальше и слабее.
            var ev = clone.GetComponent<EvolutionController>();
            if (ev != null) ev.DisableFurtherEvolution();
            var eb = clone.GetComponent<EnemyBase>();
            if (eb != null) eb.EvolveStats(0.5f, true);
        }

        /// <summary>Запретить эволюцию (для порождённых копий — чтобы не делились бесконечно).</summary>
        public void DisableFurtherEvolution() => _disabled = true;

        // ── Пресеты для быстрого использования / тестов ─────────────────────────
        /// <summary>Ярость на пороге HP: тинт + ускорение/урон, без деления.</summary>
        public static EvolutionController AttachEnrage(GameObject go,
            float hpThreshold = 0.5f, float speedMult = 1.3f, float damageMult = 1.3f)
        {
            var ev = go.AddComponent<EvolutionController>();
            ev.stages.Add(new EvolutionStage
            {
                name = "Enrage",
                trigger = EvolutionTrigger.HpBelow,
                hpThreshold = hpThreshold,
                speedMult = speedMult,
                damageMult = damageMult,
                useTint = true,
                tint = new Color(1f, 0.5f, 0.3f),
            });
            return ev;
        }

        /// <summary>Деление на пороге HP (как у «Пожирателя Эха»).</summary>
        public static EvolutionController AttachSplit(GameObject go,
            float hpThreshold = 0.5f, int count = 2)
        {
            var ev = go.AddComponent<EvolutionController>();
            ev.stages.Add(new EvolutionStage
            {
                name = "Split",
                trigger = EvolutionTrigger.HpBelow,
                hpThreshold = hpThreshold,
                splitCount = count,
                useTint = true,
                tint = new Color(0.6f, 0.4f, 0.9f),
            });
            return ev;
        }
    }
}
