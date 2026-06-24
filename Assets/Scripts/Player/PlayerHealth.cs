using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] float maxHp = 100f;

    public float CurrentHp { get; private set; }
    public float MaxHp     => maxHp;

    public UnityEvent<float> OnHpChanged; // 0..1 normalized
    public UnityEvent        OnDeath;

    void Awake() => CurrentHp = maxHp;

    public void TakeDamage(float amount)
    {
        if (CurrentHp <= 0f) return;
        CurrentHp = Mathf.Max(0f, CurrentHp - amount);
        OnHpChanged?.Invoke(CurrentHp / maxHp);
        if (CurrentHp <= 0f) OnDeath?.Invoke();
    }

    public void Heal(float amount)
    {
        CurrentHp = Mathf.Min(maxHp, CurrentHp + amount);
        OnHpChanged?.Invoke(CurrentHp / maxHp);
    }
}
