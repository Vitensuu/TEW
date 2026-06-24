using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Повесь на Image-объект "bar" (тот у которого Image Type = Filled).
/// Автоматически найдёт PlayerHealth и подпишется на изменение HP.
/// </summary>
[RequireComponent(typeof(Image))]
public class HpBarFill : MonoBehaviour
{
    Image _fill;

    void Awake()
    {
        _fill = GetComponent<Image>();
    }

    void Start()
    {
        var ph = FindFirstObjectByType<PlayerHealth>();
        if (ph != null)
        {
            ph.OnHpChanged.AddListener(SetFill);
            SetFill(ph.CurrentHp / ph.MaxHp);
        }
        else
            Debug.LogWarning("[HpBarFill] PlayerHealth не найден на сцене!");
    }

    void SetFill(float value)
    {
        _fill.fillAmount = Mathf.Clamp01(value);
    }
}
