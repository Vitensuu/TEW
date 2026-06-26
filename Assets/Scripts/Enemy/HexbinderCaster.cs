using UnityEngine;

namespace Enemy
{
    /// <summary>
    /// Поведение «Чародея-Звена» (дизайн Фаза 3, роль Support): враг тянет
    /// <see cref="CorruptionLink"/> к сильнейшему живому союзнику рядом и держит его
    /// (лечение + щит). Если связь рвётся (игрок пересёк луч / союзник погиб) —
    /// после паузы ищет новую цель. Компонент компонуемый: вешается на любого
    /// врага (как EliteModifier); сам по себе движение/AI не меняет.
    /// </summary>
    public class HexbinderCaster : MonoBehaviour
    {
        [SerializeField] float allyScanRadius  = 8f;
        [SerializeField] float retargetInterval = 2f;
        [SerializeField] float healPerSecond    = 4f;
        [Range(0f, 1f)] [SerializeField] float shieldFraction = 0.5f;

        EnemyBase _self;
        CorruptionLink _link;
        float _t;

        void Awake() => _self = GetComponent<EnemyBase>();

        void Update()
        {
            if (_self == null || _self.Dead) return;

            // _link == null истинно и когда связь разорвалась (объект уничтожен).
            if (_link == null)
            {
                _t -= Time.deltaTime;
                if (_t <= 0f) { _t = retargetInterval; AcquireTarget(); }
            }
        }

        void AcquireTarget()
        {
            EnemyBase best = null;
            float bestHp = -1f;

            var hits = Physics2D.OverlapCircleAll(transform.position, allyScanRadius);
            foreach (var h in hits)
            {
                var eb = h.GetComponentInParent<EnemyBase>();
                if (eb == null || eb == _self || !eb.IsAlive) continue;
                if (eb is BossController) continue;           // босса не линкуем
                if (eb.MaxHp > bestHp) { bestHp = eb.MaxHp; best = eb; }
            }

            if (best != null)
                _link = CorruptionLink.Create(_self, best,
                    healPerSecond: healPerSecond, shieldFraction: shieldFraction);
        }

        void OnDestroy() { if (_link != null) _link.Break(); }
    }
}
