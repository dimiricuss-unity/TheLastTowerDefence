using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TheLastTowerDefence.UI;

namespace TheLastTowerDefence.Heroes.Systems
{
    /// <summary>
    /// UI на иконке героя (<c>Cleric_icon</c>, <c>Knight_icon</c>, <c>Archer_icon</c>): мана (синяя сразу,
    /// белая догоняющая — через <see cref="UiFilledImageCatchUp"/>), текст маны, опционально полоска кулдауна атаки.
    /// HP остаётся на <see cref="CharacterHeroHitBarView"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterClericHudView : MonoBehaviour
    {
        const string RangeHeroTag = "RangeHero";

        [Tooltip("Стат героя. Пусто — один герой в сцене или подбор по имени иконки (Knight / Archer / Cleric).")]
        [SerializeField] CharacterHeroStats hero;

        [Tooltip("Дальний бой (кулдаун выстрела/лечения). Пусто — ищется на герое, если есть.")]
        [SerializeField] CharacterHeroRangeAttack rangeAttack;

        [SerializeField] Image blueManaBar;
        [SerializeField] Image whiteManaCatchUpBar;
        [SerializeField] Image whiteCooldownBar;
        [SerializeField] TMP_Text manaText;
        [SerializeField] float whiteManaCatchUpSpeed = 8f;

        CharacterHeroMeleeAttack _melee;

        void Awake()
        {
            TryResolveHero();
            if (hero != null)
            {
                if (rangeAttack == null)
                    rangeAttack = hero.GetComponentInChildren<CharacterHeroRangeAttack>(true);
                _melee = hero.GetComponentInChildren<CharacterHeroMeleeAttack>(true);
            }

            TryAutoWire();
        }

        void OnEnable()
        {
            if (hero != null)
            {
                hero.ManaChanged += OnManaChanged;
                OnManaChanged(hero.CurrentMana, hero.MaxMana);
                if (whiteManaCatchUpBar != null && blueManaBar != null)
                    whiteManaCatchUpBar.fillAmount = blueManaBar.fillAmount;
            }

            RefreshCooldownVisual();
        }

        void OnDisable()
        {
            if (hero != null)
                hero.ManaChanged -= OnManaChanged;
        }

        void Update()
        {
            if (hero == null || !hero.IsAlive)
            {
                ApplyDeadVisuals();
                return;
            }

            UiFilledImageCatchUp.Tick(whiteManaCatchUpBar, blueManaBar, whiteManaCatchUpSpeed);
            RefreshCooldownVisual();
        }

        void OnManaChanged(float current, float max)
        {
            if (hero == null)
                return;

            var maxMana = Mathf.Max(0f, max);
            var cur = Mathf.Clamp(current, 0f, maxMana > 0f ? maxMana : 0f);

            if (blueManaBar != null)
                blueManaBar.fillAmount = maxMana > 1e-6f ? Mathf.Clamp01(cur / maxMana) : 0f;

            if (manaText != null)
            {
                if (maxMana <= 1e-6f)
                    manaText.text = "0 / 0";
                else
                    manaText.text = $"{Mathf.RoundToInt(cur)} / {Mathf.RoundToInt(maxMana)}";
            }
        }

        void RefreshCooldownVisual()
        {
            if (whiteCooldownBar == null)
                return;

            if (hero == null || !hero.IsAlive)
            {
                whiteCooldownBar.fillAmount = 0f;
                return;
            }

            var remaining = GetAttackCooldownRemaining01();
            whiteCooldownBar.fillAmount = Mathf.Clamp01(1f - remaining);
        }

        float GetAttackCooldownRemaining01()
        {
            if (rangeAttack != null)
                return rangeAttack.AttackCooldownRemaining01;
            if (_melee != null)
                return _melee.AttackCooldownRemaining01;
            return 0f;
        }

        void ApplyDeadVisuals()
        {
            if (blueManaBar != null)
                blueManaBar.fillAmount = 0f;
            if (whiteManaCatchUpBar != null)
                whiteManaCatchUpBar.fillAmount = 0f;
            if (whiteCooldownBar != null)
                whiteCooldownBar.fillAmount = 0f;
            if (manaText != null)
                manaText.text = "0 / 0";
        }

        void TryResolveHero()
        {
            if (hero != null)
                return;

            var all = UnityEngine.Object.FindObjectsByType<CharacterHeroStats>(FindObjectsSortMode.None);
            if (all.Length == 1)
            {
                hero = all[0];
                return;
            }

            var icon = gameObject.name;
            foreach (var h in all)
            {
                if (h == null)
                    continue;

                if (icon.IndexOf("Knight", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    !h.CompareTag(RangeHeroTag))
                {
                    hero = h;
                    return;
                }

                if (icon.IndexOf("Warrior", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    !h.CompareTag(RangeHeroTag))
                {
                    hero = h;
                    return;
                }

                if (icon.IndexOf("Archer", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    h.CompareTag(RangeHeroTag))
                {
                    hero = h;
                    return;
                }

                if (icon.IndexOf("Cleric", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    (h.gameObject.name.IndexOf("Cleric", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     h.transform.root.name.IndexOf("Cleric", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    hero = h;
                    return;
                }
            }
        }

        void TryAutoWire()
        {
            if (blueManaBar == null)
            {
                blueManaBar = transform.Find("ManaBar/BlackBack/BlueBar")?.GetComponent<Image>()
                              ?? transform.Find("ManaBar/BlackBack/BlueLine")?.GetComponent<Image>()
                              ?? transform.Find("ManaBar/BlueBar")?.GetComponent<Image>()
                              ?? transform.Find("BlueBar")?.GetComponent<Image>();
            }

            if (whiteManaCatchUpBar == null)
            {
                whiteManaCatchUpBar = transform.Find("ManaBar/BlackBack/WhiteBar")?.GetComponent<Image>()
                                      ?? transform.Find("ManaBar/BlackBack/WhiteBlack")?.GetComponent<Image>()
                                      ?? transform.Find("ManaBar/WhiteBar")?.GetComponent<Image>();
            }

            if (whiteCooldownBar == null)
            {
                whiteCooldownBar = transform.Find("ReloadBar/BlackBack/WhiteBar")?.GetComponent<Image>()
                                   ?? transform.Find("ReloadBar/WhiteBar")?.GetComponent<Image>()
                                   ?? transform.Find("CooldownBar/BlackBack/WhiteBar")?.GetComponent<Image>()
                                   ?? transform.Find("CooldownBar/WhiteBar")?.GetComponent<Image>();
            }

            if (manaText == null)
            {
                manaText = transform.Find("ManaBar/BlackBack/ManaText")?.GetComponent<TMP_Text>()
                           ?? transform.Find("ManaBar/ManaText")?.GetComponent<TMP_Text>()
                           ?? transform.Find("ManaText")?.GetComponent<TMP_Text>();
            }
        }
    }
}
