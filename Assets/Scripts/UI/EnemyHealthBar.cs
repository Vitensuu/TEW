using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Плавающая полоска HP над врагом. Строится целиком в рантайме (две
    /// SpriteRenderer'ы: фон + заливка), не требует префаба/Canvas. Подписывается
    /// на <see cref="EnemyBase.OnHpChanged"/> (нормализованное 0..1).
    ///
    /// Авто-добавляется из <see cref="EnemyBase.Awake"/>, так что каждый враг
    /// получает бар без ручной правки префабов. Бар скрыт при полном HP и появляется
    /// после первого урона — чтобы было видно, что урон проходит.
    /// </summary>
    public class EnemyHealthBar : MonoBehaviour
    {
        [Tooltip("Ширина/высота полоски в мировых единицах")]
        [SerializeField] float width = 0.8f, height = 0.12f;
        [Tooltip("Смещение над врагом по Y")]
        [SerializeField] float yOffset = 0.7f;
        [Tooltip("Прятать полоску, пока враг на полном HP")]
        [SerializeField] bool hideWhenFull = true;

        Enemy.EnemyBase _enemy;
        Transform   _root;
        Transform   _fill;
        SpriteRenderer _fillSr, _bgSr;
        float _ratio = 1f;

        static Sprite _whiteCenter, _whiteLeft;

        void Awake()
        {
            _enemy = GetComponent<Enemy.EnemyBase>();
            BuildBar();
        }

        void OnEnable()
        {
            if (_enemy != null && _enemy.OnHpChanged != null)
                _enemy.OnHpChanged.AddListener(SetRatio);
        }

        void OnDisable()
        {
            if (_enemy != null && _enemy.OnHpChanged != null)
                _enemy.OnHpChanged.RemoveListener(SetRatio);
        }

        void BuildBar()
        {
            EnsureSprites();

            // Корень бара — отдельный объект, НЕ потомок врага, чтобы не наследовать
            // его поворот/флип спрайта и не масштабироваться вместе с ним.
            _root = new GameObject("EnemyHealthBar").transform;

            int order = (_enemy != null ? _enemy.SpriteSortingOrder : 6) + 1;

            _bgSr = MakePart(_root, _whiteCenter, new Color(0f, 0f, 0f, 0.6f), order);
            _bgSr.transform.localScale = new Vector3(width, height, 1f);

            _fill = new GameObject("Fill").transform;
            _fill.SetParent(_root, false);
            _fillSr = _fill.gameObject.AddComponent<SpriteRenderer>();
            _fillSr.sprite = _whiteLeft;          // pivot слева → заливка тянется слева направо
            _fillSr.sortingOrder = order + 1;
            // Заливка прижата к левому краю фона.
            _fill.localPosition = new Vector3(-width * 0.5f, 0f, 0f);

            ApplyRatio();
            UpdateVisibility();
        }

        static SpriteRenderer MakePart(Transform parent, Sprite sprite, Color c, int order)
        {
            var go = new GameObject("Part");
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = c;
            sr.sortingOrder = order;
            return sr;
        }

        void LateUpdate()
        {
            if (_root == null) return;
            // Полоска следует за врагом, оставаясь в мировых осях (без поворота/флипа).
            _root.position = transform.position + Vector3.up * yOffset;
            _root.rotation = Quaternion.identity;
        }

        void SetRatio(float r)
        {
            _ratio = Mathf.Clamp01(r);
            ApplyRatio();
            UpdateVisibility();
        }

        void ApplyRatio()
        {
            if (_fill == null) return;
            _fill.localScale = new Vector3(width * _ratio, height, 1f);
            _fillSr.color = _ratio > 0.5f ? Color.green
                          : _ratio > 0.25f ? new Color(1f, 0.7f, 0f)
                          : Color.red;
        }

        void UpdateVisibility()
        {
            bool show = !(hideWhenFull && _ratio >= 0.999f) && _ratio > 0f;
            if (_root != null) _root.gameObject.SetActive(show);
        }

        void OnDestroy()
        {
            if (_root != null) Destroy(_root.gameObject);
        }

        // 1×1 белые спрайты с разным pivot (центр для фона, левый край для заливки).
        static void EnsureSprites()
        {
            if (_whiteCenter != null) return;
            var tex = Texture2D.whiteTexture;
            var rect = new Rect(0, 0, tex.width, tex.height);
            _whiteCenter = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), tex.width);
            _whiteLeft   = Sprite.Create(tex, rect, new Vector2(0f, 0.5f),   tex.width);
        }
    }
}
