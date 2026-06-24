using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Полоска HP. Работает в двух режимах:
///
/// 1. Screen Space — для игрока:
///    - Создай Canvas (Screen Space - Overlay)
///    - Добавь Image (background) → дочерний Image (fill, Image Type = Filled, Fill Method = Horizontal)
///    - Повесь HealthBar на fill-Image, выставь mode = Player
///    - В PlayerHealth.OnHpChanged перетащи этот объект и выбери HealthBar.SetFill
///
/// 2. World Space — для врагов:
///    - Добавь дочерний Canvas (World Space, Sort Order 5) на префаб врага
///    - Внутри: Image (bg) → Image (fill)
///    - Повесь HealthBar на fill-Image, mode = Enemy
///    - Скрипт сам найдёт EnemyBase на родителе и подпишется
/// </summary>
public class HealthBar : MonoBehaviour
{
    public enum Mode { Player, Enemy }

    [SerializeField] Mode   mode   = Mode.Enemy;
    [SerializeField] Image  fill;                    // заполняемая полоска
    [SerializeField] Color  fullColor  = Color.green;
    [SerializeField] Color  lowColor   = Color.red;
    [Tooltip("Ниже этого порога полоска становится красной")]
    [SerializeField] float  lowThreshold = 0.3f;
    [Tooltip("Смещение над врагом (World Space)")]
    [SerializeField] Vector3 worldOffset = new Vector3(0f, 0.7f, 0f);

    Transform _followTarget;

    void Awake()
    {
        if (fill == null) fill = GetComponent<Image>();

        if (mode == Mode.Player)
        {
            var ph = FindFirstObjectByType<PlayerHealth>();
            if (ph != null)
            {
                ph.OnHpChanged.AddListener(SetFill);
                SetFill(1f);
            }
        }
        else
        {
            var enemy = GetComponentInParent<Enemy.EnemyBase>();
            if (enemy != null)
            {
                _followTarget = enemy.transform;
                SetFill(enemy.CurrentHp / enemy.MaxHp);
            }
        }
    }

    void LateUpdate()
    {
        if (mode == Mode.Enemy && _followTarget != null)
            transform.position = _followTarget.position + worldOffset;
    }

    /// <summary>value 0..1</summary>
    public void SetFill(float value)
    {
        if (fill == null) return;
        fill.fillAmount = Mathf.Clamp01(value);
        fill.color      = Color.Lerp(lowColor, fullColor,
            Mathf.InverseLerp(0f, lowThreshold, value));
    }
}
