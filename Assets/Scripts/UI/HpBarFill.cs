using UnityEngine;
using UnityEngine.UI;
using Game.Core;

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
        var ph = ServiceLocator.Get<PlayerHealth>(); // ТЗ §6 — развязка
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
