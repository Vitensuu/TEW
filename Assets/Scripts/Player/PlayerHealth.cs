using System;
using UnityEngine;
using UnityEngine.Events;
using Game.Core;

/// <summary>
/// Здоровье игрока (реализует IDamageable — ТЗ §5).
/// При обнулении HP шлёт EventBus.OnPlayerDeath (GameManager → экран смерти).
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] float maxHp = 100f;

    public float CurrentHp { get; private set; }
    public float MaxHp     => maxHp;
    public bool  IsAlive   => CurrentHp > 0f;

    public UnityEvent<float> OnHpChanged;   // 0..1 normalized (для HpBarFill)
    public UnityEvent        OnDeath;
    public event Action<float, float> OnHealthChanged; // current, max (IDamageable)

    void Awake()
    {
        // Развязка (ТЗ §6): регистрируем игрока в сервис-локаторе и PlayerRef,
        // чтобы UI/враги не искали его через FindObjectOfType/тег.
        ServiceLocator.Register(this);
        PlayerRef.Set(transform);

        // Если есть активный забег — берём максимум из статов класса.
        var run = GameManager.Instance != null ? GameManager.Instance.Run : null;
        if (run != null && run.stats != null && run.stats.maxHealth > 0f)
        {
            maxHp = run.stats.maxHealth;
            CurrentHp = run.currentHealth > 0f ? run.currentHealth : maxHp;
        }
        else CurrentHp = maxHp;
    }

    void OnDestroy()
    {
        ServiceLocator.Unregister(this);
        PlayerRef.Clear(transform);
    }

    public void TakeDamage(float amount, DamageType type = DamageType.Physical)
    {
        if (CurrentHp <= 0f) return;
        CurrentHp = Mathf.Max(0f, CurrentHp - amount);
        SyncRun();
        OnHpChanged?.Invoke(CurrentHp / maxHp);
        OnHealthChanged?.Invoke(CurrentHp, maxHp);
        if (CurrentHp <= 0f)
        {
            OnDeath?.Invoke();
            EventBus.TriggerPlayerDeath();
        }
    }

    // Совместимость со старым кодом (EnemyBase.ApplyAttackDamage).
    public void TakeDamage(float amount) => TakeDamage(amount, DamageType.Physical);

    public void Heal(float amount)
    {
        if (CurrentHp <= 0f) return;
        CurrentHp = Mathf.Min(maxHp, CurrentHp + amount);
        SyncRun();
        OnHpChanged?.Invoke(CurrentHp / maxHp);
        OnHealthChanged?.Invoke(CurrentHp, maxHp);
    }

    void SyncRun()
    {
        var run = GameManager.Instance != null ? GameManager.Instance.Run : null;
        if (run != null) run.currentHealth = CurrentHp;
    }
}
