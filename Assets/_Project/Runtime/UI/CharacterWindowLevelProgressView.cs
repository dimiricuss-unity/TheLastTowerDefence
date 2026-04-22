using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TheLastTowerDefence.Heroes.Systems;

namespace TheLastTowerDefence.UI
{
    /// <summary>
    /// Прогресс уровня под вкладкой «Характеристики»: LevelContent, Exp — <b>накопительный</b> XP
    /// (<see cref="CharacterHeroStats.CumulativeXpDisplay"/> / <see cref="CharacterHeroStats.CumulativeXpNextBoundary"/>),
    /// вехи задаются в <c>ExperienceSteps</c> как накопительные пороги. LevelLine — доля внутри текущего сегмента.
    /// На максимальном уровне — накопительный итог / сумма всех сегментов (последняя веха).
    /// Вешается на <c>CharacteristicsTab</c> рядом с остальными видами окна героя.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterWindowLevelProgressView : MonoBehaviour
    {
        [SerializeField] CharacterHeroStats hero;
        [SerializeField] TMP_Text levelContent;
        [SerializeField] TMP_Text expText;
        [SerializeField] Image levelLine;

        void Awake()
        {
            ResolveHeroIfMissing();
            BindUiIfMissing();
        }

        void OnEnable()
        {
            ResolveHeroIfMissing();
            if (hero != null)
                hero.ProgressionChanged += OnProgressionChanged;

            RefreshAll();
        }

        void OnDisable()
        {
            if (hero != null)
                hero.ProgressionChanged -= OnProgressionChanged;
        }

        void OnProgressionChanged() => RefreshAll();

        void ResolveHeroIfMissing()
        {
            if (hero != null)
                return;

            var window = transform.parent;
            if (window == null)
                return;

            var heroRootName = window.name switch
            {
                "WarriorWindow" => "Knight",
                "ArcherWindow" => "Archer",
                "ClericWindow" => "Cleric",
                _ => null,
            };

            if (heroRootName == null)
                return;

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

        void BindUiIfMissing()
        {
            var levelProgress = transform.Find("LevelProgress");
            if (levelProgress == null)
                return;

            if (levelContent == null)
            {
                var texts = levelProgress.GetComponentsInChildren<TMP_Text>(true);
                for (var i = 0; i < texts.Length; i++)
                {
                    if (texts[i].name == "LevelContent")
                    {
                        levelContent = texts[i];
                        break;
                    }
                }
            }

            if (expText == null)
            {
                var texts = levelProgress.GetComponentsInChildren<TMP_Text>(true);
                for (var i = 0; i < texts.Length; i++)
                {
                    if (texts[i].name == "Exp")
                    {
                        expText = texts[i];
                        break;
                    }
                }
            }

            if (levelLine == null)
            {
                var images = levelProgress.GetComponentsInChildren<Image>(true);
                for (var i = 0; i < images.Length; i++)
                {
                    if (images[i].name == "LevelLine")
                    {
                        levelLine = images[i];
                        break;
                    }
                }
            }
        }

        void RefreshAll()
        {
            if (hero == null || hero.Config == null)
            {
                SetPlaceholder(levelContent);
                SetPlaceholder(expText);
                if (levelLine != null)
                    levelLine.fillAmount = 0f;
                return;
            }

            SetText(levelContent, hero.DisplayLevel.ToString(CultureInfo.InvariantCulture));

            var steps = hero.Config.experienceSteps;
            if (steps == null || steps.xpThresholdsForNextLevel == null || steps.xpThresholdsForNextLevel.Count == 0)
            {
                SetPlaceholder(expText);
                if (levelLine != null)
                    levelLine.fillAmount = 0f;
                return;
            }

            var cur = hero.CumulativeXpDisplay;
            var cap = hero.CumulativeXpNextBoundary;
            SetText(
                expText,
                $"{cur.ToString(CultureInfo.InvariantCulture)} / {cap.ToString(CultureInfo.InvariantCulture)}");

            if (levelLine != null)
                levelLine.fillAmount = hero.XpThresholdForNextLevel > 0 ? hero.LevelProgressFill01 : 1f;
        }

        static void SetPlaceholder(TMP_Text text)
        {
            if (text != null)
                text.text = "--";
        }

        static void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value;
        }
    }
}
