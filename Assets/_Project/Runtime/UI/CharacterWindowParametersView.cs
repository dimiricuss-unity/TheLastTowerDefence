using System.Globalization;
using TMPro;
using UnityEngine;
using TheLastTowerDefence.Heroes.Systems;

namespace TheLastTowerDefence.UI
{
    /// <summary>
    /// Вкладка «Параметры»: вывод производных боевых величин из <see cref="CharacterHeroStats"/>.
    /// Вешается на объект <c>ParametersTab</c> внутри окна героя.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterWindowParametersView : MonoBehaviour
    {
        [SerializeField] CharacterHeroStats hero;

        [Header("Content (TMP)")]
        [SerializeField] TMP_Text hpContent;
        [SerializeField] TMP_Text manaContent;
        [SerializeField] TMP_Text manaRestoreContent;
        [SerializeField] TMP_Text minimumDamageContent;
        [SerializeField] TMP_Text maximumDamageContent;
        [SerializeField] TMP_Text critChanceContent;
        [SerializeField] TMP_Text criticalDamageContent;
        [SerializeField] TMP_Text attackSpeedContent;
        [SerializeField] TMP_Text damageResistanceContent;

        void Awake()
        {
            ResolveHeroIfMissing();
            BindContentTextsIfMissing();
        }

        void OnEnable()
        {
            ResolveHeroIfMissing();
            if (hero != null)
            {
                hero.HealthChanged += OnHealthOrManaChanged;
                hero.ManaChanged += OnHealthOrManaChanged;
                hero.DerivedStatsChanged += OnDerivedStatsChanged;
            }

            RefreshAll();
        }

        void OnDisable()
        {
            if (hero != null)
            {
                hero.HealthChanged -= OnHealthOrManaChanged;
                hero.ManaChanged -= OnHealthOrManaChanged;
                hero.DerivedStatsChanged -= OnDerivedStatsChanged;
            }
        }

        void OnHealthOrManaChanged(float _, float __) => RefreshHpMana();

        void OnDerivedStatsChanged() => RefreshAll();

        void ResolveHeroIfMissing()
        {
            if (hero != null)
            {
                return;
            }

            var window = transform.parent;
            if (window == null)
            {
                return;
            }

            var heroRootName = window.name switch
            {
                "WarriorWindow" => "Knight",
                "ArcherWindow" => "Archer",
                "ClericWindow" => "Cleric",
                _ => null
            };

            if (heroRootName == null)
            {
                return;
            }

            var all = FindObjectsByType<CharacterHeroStats>(FindObjectsSortMode.None);
            for (var i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].gameObject.name == heroRootName)
                {
                    hero = all[i];
                    return;
                }
            }
        }

        void BindContentTextsIfMissing()
        {
            BindByChildName(ref hpContent, "HpContent");
            BindByChildName(ref manaContent, "ManaContent");
            BindByChildName(ref manaRestoreContent, "ManaRestoreContent");
            BindByChildName(ref minimumDamageContent, "MinimumDamageContent");
            BindByChildName(ref maximumDamageContent, "MaximumDamageContent");
            BindByChildName(ref critChanceContent, "CritChanceContent");
            BindByChildName(ref criticalDamageContent, "CriticalDamageContent");
            BindByChildName(ref attackSpeedContent, "AttackSpeedContent");
            BindByChildName(ref damageResistanceContent, "DamageResistanceContent");
        }

        void BindByChildName(ref TMP_Text field, string objectName)
        {
            if (field != null)
            {
                return;
            }

            var texts = GetComponentsInChildren<TMP_Text>(true);
            for (var i = 0; i < texts.Length; i++)
            {
                if (texts[i].name == objectName)
                {
                    field = texts[i];
                    return;
                }
            }
        }

        void RefreshAll()
        {
            RefreshHpMana();
            RefreshDerived();
        }

        void RefreshHpMana()
        {
            if (!TryGetHeroForDisplay(out var h))
            {
                SetPlaceholder(hpContent);
                SetPlaceholder(manaContent);
                return;
            }

            SetText(hpContent, $"{FormatInt(h.CurrentHealth)}/{FormatInt(h.MaxHealth)}");
            SetText(manaContent, $"{FormatInt(h.CurrentMana)}/{FormatInt(h.MaxMana)}");
        }

        void RefreshDerived()
        {
            if (!TryGetHeroForDisplay(out var h))
            {
                SetPlaceholder(manaRestoreContent);
                SetPlaceholder(minimumDamageContent);
                SetPlaceholder(maximumDamageContent);
                SetPlaceholder(critChanceContent);
                SetPlaceholder(criticalDamageContent);
                SetPlaceholder(attackSpeedContent);
                SetPlaceholder(damageResistanceContent);
                return;
            }

            SetText(manaRestoreContent, $"{FormatInt(h.ManaRegenPerSecond)}/s");
            SetText(minimumDamageContent, FormatInt(h.MinDamage));
            SetText(maximumDamageContent, FormatInt(h.MaxDamage));
            SetText(critChanceContent, $"{FormatInt(h.CritChancePercent)}%");
            SetText(
                criticalDamageContent,
                $"{FormatInt(h.CriticalDamageMin)}-{FormatInt(h.CriticalDamageMax)}");
            SetText(
                attackSpeedContent,
                h.AttacksPerSecond.ToString("0.00", CultureInfo.InvariantCulture) + "/s");
            SetText(damageResistanceContent, h.DamageResistanceRating.ToString(CultureInfo.InvariantCulture));
        }

        bool TryGetHeroForDisplay(out CharacterHeroStats h)
        {
            h = hero;
            return h != null && h.Config != null;
        }

        static void SetPlaceholder(TMP_Text text)
        {
            if (text != null)
            {
                text.text = "--";
            }
        }

        static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        static string FormatInt(float value) => Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture);
    }
}
