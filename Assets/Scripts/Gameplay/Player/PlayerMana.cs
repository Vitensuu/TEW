using UnityEngine;
using UnityEngine.Events;
using Game.Core;

public class PlayerMana : MonoBehaviour
{
    [SerializeField] float maxMp = 100f;
    [SerializeField] float regenPerSecond = 5f;

    public float CurrentMp { get; private set; }
    public float MaxMp     => maxMp;

    public UnityEvent<float> OnMpChanged; // 0..1 normalized

    void Awake()
    {
        CurrentMp = maxMp;
        ServiceLocator.Register(this); // ТЗ §6 — развязка вместо FindObjectOfType
    }

    void OnDestroy() => ServiceLocator.Unregister(this);

    void Update()
    {
        if (CurrentMp < maxMp)
        {
            CurrentMp = Mathf.Min(maxMp, CurrentMp + regenPerSecond * Time.deltaTime);
            OnMpChanged?.Invoke(CurrentMp / maxMp);
        }
    }

    public bool UseMana(float amount)
    {
        if (CurrentMp < amount) return false;
        CurrentMp -= amount;
        OnMpChanged?.Invoke(CurrentMp / maxMp);
        return true;
    }

    public void RestoreMana(float amount)
    {
        CurrentMp = Mathf.Min(maxMp, CurrentMp + amount);
        OnMpChanged?.Invoke(CurrentMp / maxMp);
    }
}
