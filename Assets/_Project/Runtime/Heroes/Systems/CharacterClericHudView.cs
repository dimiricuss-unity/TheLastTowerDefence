using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TheLastTowerDefence.UI;

namespace TheLastTowerDefence.Heroes.Systems
{
    /// <summary>
    /// UI на <c>Cleric_icon</c>: мана (синяя сразу, белая догоняющая — через <see cref="UiFilledImageCatchUp"/>),
    /// текст маны, отдельная полоска кулдауна дальнего боя/лечения. HP остаётся на <see cref="CharacterHeroHitBarView"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterClericHudView : MonoBehaviour
    {
        [Tooltip("Стат клирика. Пусто — один герой в сцене или поиск по имени объекта (Cleric_icon / Cleric).")]
        [SerializeField] CharacterHeroStats hero;

        [Tooltip("Дальний бой (кулдаун атаки и лечения). Пусто — ищется на том же герое.")]
        [SerializeField] CharacterHeroRangeAttack rangeAttack;

        [SerializeField] Image blueManaBar;
        [SerializeField] Image whiteManaCatchUpBar;
        [SerializeField] Image whiteCooldownBar;
        [SerializeField] TMP_Text manaText;
        [SerializeField] float whiteManaCatchUpSpeed = 8f;

        void Awake()
        {
            TryResolveHero();
            if (rangeAttack == null && hero != null)
                rangeAttack = hero.GetComponentInChildren<CharacterHeroRangeAttack>(true);
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

            if (rangeAttack == null || hero == null || !hero.IsAlive)
            {
                whiteCooldownBar.fillAmount = 0f;
                return;
            }

            var remaining = rangeAttack.AttackCooldownRemaining01;
            whiteCooldownBar.fillAmount = Mathf.Clamp01(1f - remaining);
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
            if (icon.IndexOf("Cleric", StringComparison.OrdinalIgnoreCase) < 0)
                return;

            foreach (var h in all)
            {
                if (h == null)
                    continue;
                if (h.gameObject.name.IndexOf("Cleric", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    h.transform.root.name.IndexOf("Cleric", StringComparison.OrdinalIgnoreCase) >= 0)
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
                              ?? transform.Find("ManaBar/BlueBar")?.GetComponent<Image>()
                              ?? transform.Find("BlueBar")?.GetComponent<Image>();
            }

            if (whiteManaCatchUpBar == null)
            {
                whiteManaCatchUpBar = transform.Find("ManaBar/BlackBack/WhiteBar")?.GetComponent<Image>()
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
