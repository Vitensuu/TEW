using UnityEngine;

namespace Enemy
{
    /// <summary>10 элитных модификаторов (дизайн Фаза 2). Каждый меняет ПОВЕДЕНИЕ, не только статы.</summary>
    public enum EliteModifierType
    {
        Berserker,    // <50% HP — урон/скорость ×2
        VoidTouched,  // оставляет Void-зоны на пути; смерть → провал-зона
        Arcane,       // периодический блинк за спину игрока
        Toxic,        // атаки травят (Poison); труп — ядовитое облако
        Frozen,       // аура Freeze в радиусе
        Mirror,       // отражает часть получаемого урона обратно
        Temporal,     // периодический откат HP/позиции на 2.5 сек назад
        Gravity,      // тянет игрока к себе
        Vampiric,     // лечится от нанесённого урона
        Explosive,    // смерть → взрыв с отбросом
    }

    /// <summary>
    /// Слой элитного модификатора (дизайн Фаза 2). Один компонент на враге;
    /// конкретное поведение выбирается полем <see cref="type"/> и реализуется
    /// плоской стратегией <see cref="EliteStrategy"/> (см. EliteModifierStrategies.cs).
    /// Навешивается <see cref="Game.Dungeon.RoomPopulator"/>'ом с шансом, либо вручную
    /// в инспекторе. Саморегистрируется в <see cref="EnemyBase"/> для перехвата
    /// урона/смерти. Анимации не трогает — только тинтит спрайт под цвет модификатора.
    /// </summary>
    public class EliteModifier : MonoBehaviour
    {
        public EliteModifierType type = EliteModifierType.Berserker;

        EliteStrategy _strategy;
        EnemyBase _host;
        bool _init;

        /// <summary>Навесить модификатор программно (RoomPopulator). Инициализирует сразу.</summary>
        public static EliteModifier Attach(GameObject go, EliteModifierType type)
        {
            var m = go.AddComponent<EliteModifier>();
            m.type = type;
            m.Init();
            return m;
        }

        void Start() => Init();   // путь дизайнера (тип задан в инспекторе)

        void Init()
        {
            if (_init) return;
            _init = true;

            _host = GetComponent<EnemyBase>();
            _strategy = EliteStrategyFactory.Create(type);
            if (_host != null) _host.RegisterModifier(this);
            _strategy.OnSpawn(_host);

            if (_strategy.Tint.HasValue)
            {
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = _strategy.Tint.Value;
            }
        }

        void Update()
        {
            if (_init) _strategy.Tick(Time.deltaTime);
        }

        // ── Хуки, вызываемые EnemyBase ──────────────────────────────────────────
        public float ModifyIncomingDamage(float amount)
            => _init ? _strategy.ModifyIncoming(amount) : amount;

        public void OnDealtDamage(Game.Core.IDamageable target, float amount)
        {
            if (_init) _strategy.OnDealtDamage(target, amount);
        }

        public void OnHostDeath()
        {
            if (_init) _strategy.OnDeath();
        }

        public EliteModifierType Type => type;
    }
}
