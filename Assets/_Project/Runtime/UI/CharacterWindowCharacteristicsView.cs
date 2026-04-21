using System.Globalization;
using TMPro;
using UnityEngine;
using TheLastTowerDefence.Heroes.Systems;

namespace TheLastTowerDefence.UI
{
    /// <summary>
    /// Вкладка «Характеристики»: вывод итоговых статов из <see cref="CharacterHeroStats.CoreStats"/>
    /// (конфиг + модификаторы инспектора + закреплённые и сессионные очки характеристик).
    /// Вешается на объект <c>CharacteristicsTab</c> внутри окна героя.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterWindowCharacteristicsView : MonoBehaviour
    {
        [SerializeField] CharacterHeroStats hero;

        [Header("Content (TMP)")]
        [SerializeField] TMP_Text strengthContent;
        [SerializeField] TMP_Text dexterityContent;
        [SerializeField] TMP_Text staminaContent;
        [SerializeField] TMP_Text intelligenceContent;
        [SerializeField] TMP_Text willpowerContent;
        [SerializeField] TMP_Text luckContent;

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
                hero.DerivedStatsChanged += OnDerivedStatsChanged;
            }

            RefreshAll();
        }

        void OnDisable()
        {
            if (hero != null)
            {
                hero.DerivedStatsChanged -= OnDerivedStatsChanged;
            }
        }

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
            BindByChildName(ref strengthContent, "StrengthContent");
            BindByChildName(ref dexterityContent, "DexterityContent");
            BindByChildName(ref staminaContent, "StaminaContent");
            BindByChildName(ref intelligenceContent, "IntelligenceContent");
            BindByChildName(ref willpowerContent, "WillpowerContent");
            BindByChildName(ref luckContent, "LuckContent");
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
            if (hero == null || hero.Config == null)
            {
                SetPlaceholder(strengthContent);
                SetPlaceholder(dexterityContent);
                SetPlaceholder(staminaContent);
                SetPlaceholder(intelligenceContent);
                SetPlaceholder(willpowerContent);
                SetPlaceholder(luckContent);
                return;
            }

            var c = hero.CoreStats;
            SetText(strengthContent, c.Strength.ToString(CultureInfo.InvariantCulture));
            SetText(dexterityContent, c.Dexterity.ToString(CultureInfo.InvariantCulture));
            SetText(staminaContent, c.Stamina.ToString(CultureInfo.InvariantCulture));
            SetText(intelligenceContent, c.Intelligence.ToString(CultureInfo.InvariantCulture));
            SetText(willpowerContent, c.Willpower.ToString(CultureInfo.InvariantCulture));
            SetText(luckContent, c.Luck.ToString(CultureInfo.InvariantCulture));
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
    }
}
