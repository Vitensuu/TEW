using UnityEngine;
using UnityEngine.UI;
using Game.Core;
using Game.Items;

namespace Game.UI
{
    /// <summary>
    /// HUD (ТЗ §6): сегментированное HP/MP, слот активного оружия + 2 быстрых
    /// предмета, счётчик монет, текущий этаж, иконки реликвий (первые 5).
    /// Подписывается на PlayerHealth/Mana и EventBus.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        [Header("Здоровье / Мана (Filled Image или Slider)")]
        [SerializeField] Image healthFill;
        [SerializeField] Image manaFill;

        [Header("Тексты")]
        [SerializeField] Text goldText;
        [SerializeField] Text floorText;

        [Header("Оружие / реликвии")]
        [SerializeField] Image activeWeaponIcon;
        [SerializeField] Image[] relicIcons;   // первые 5 видимых (ТЗ §6)

        PlayerHealth _hp;
        PlayerMana   _mp;

        void Start()
        {
            _hp = FindFirstObjectByType<PlayerHealth>();
            _mp = FindFirstObjectByType<PlayerMana>();

            if (_hp != null)
            {
                _hp.OnHpChanged.AddListener(SetHealth);
                SetHealth(_hp.CurrentHp / _hp.MaxHp);
            }
            if (_mp != null)
            {
                _mp.OnMpChanged.AddListener(SetMana);
                SetMana(_mp.CurrentMp / _mp.MaxMp);
            }

            EventBus.OnGoldChanged    += SetGold;
            EventBus.OnFloorGenerated += SetFloor;

            if (GameManager.Instance?.Run != null)
            {
                SetGold(GameManager.Instance.Run.gold);
                SetFloor(GameManager.Instance.Run.currentFloor);
            }

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnChanged += RefreshItems;
                RefreshItems();
            }
        }

        void OnDestroy()
        {
            EventBus.OnGoldChanged    -= SetGold;
            EventBus.OnFloorGenerated -= SetFloor;
            if (InventoryManager.Instance != null)
                InventoryManager.Instance.OnChanged -= RefreshItems;
        }

        void SetHealth(float n01) { if (healthFill != null) healthFill.fillAmount = Mathf.Clamp01(n01); }
        void SetMana(float n01)   { if (manaFill   != null) manaFill.fillAmount   = Mathf.Clamp01(n01); }
        void SetGold(int g)       { if (goldText  != null) goldText.text  = g.ToString(); }
        void SetFloor(int f)      { if (floorText != null) floorText.text = $"Этаж {f}"; }

        void RefreshItems()
        {
            var inv = InventoryManager.Instance;
            if (inv == null) return;

            if (activeWeaponIcon != null)
            {
                var w = inv.ActiveWeapon;
                activeWeaponIcon.enabled = w != null && w.sprite != null;
                if (w != null) activeWeaponIcon.sprite = w.sprite;
            }

            if (relicIcons != null)
                for (int i = 0; i < relicIcons.Length; i++)
                {
                    if (relicIcons[i] == null) continue;
                    bool has = i < inv.Relics.Count && inv.Relics[i] != null;
                    relicIcons[i].enabled = has;
                    if (has) relicIcons[i].sprite = inv.Relics[i].sprite;
                }
        }
    }
}
