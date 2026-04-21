using System;
using UnityEngine;

namespace TheLastTowerDefence.Heroes.Systems
{
    /// <summary>
    /// На <c>Knight_icon</c> / <c>Archer_icon</c> / <c>Cleric_icon</c>: дочерний <c>LvIndicator</c> виден,
    /// когда у героя есть свободные очки характеристик (<see cref="CharacterHeroStats.AvailableSkillPoints"/> &gt; 0).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterHeroLvIndicatorView : MonoBehaviour
    {
        [Tooltip("Если задано — используется вместо авто-подбора по имени иконки.")]
        [SerializeField] CharacterHeroStats hero;

        [Tooltip("Объект LvIndicator. Пусто — дочерний с именем LvIndicator.")]
        [SerializeField] GameObject lvIndicator;

        void Awake()
        {
            TryResolveHero();
            TryBindLvIndicator();
        }

        void OnEnable()
        {
            if (hero != null)
            {
                hero.DerivedStatsChanged += OnHeroStatsChanged;
                hero.ProgressionChanged += OnHeroStatsChanged;
            }

            Refresh();
        }

        void Start() => Refresh();

        void OnDisable()
        {
            if (hero != null)
            {
                hero.DerivedStatsChanged -= OnHeroStatsChanged;
                hero.ProgressionChanged -= OnHeroStatsChanged;
            }
        }

        void OnHeroStatsChanged() => Refresh();

        void TryResolveHero()
        {
            if (hero != null)
                return;

            var all = FindObjectsByType<CharacterHeroStats>(FindObjectsSortMode.None);
            if (all.Length == 1)
            {
                hero = all[0];
                return;
            }

            var wantRoot = MapIconNameToHeroRootName(gameObject.name);
            if (wantRoot == null)
                return;

            for (var i = 0; i < all.Length; i++)
            {
                if (all[i] != null && string.Equals(all[i].gameObject.name, wantRoot, StringComparison.Ordinal))
                {
                    hero = all[i];
                    return;
                }
            }
        }

        static string MapIconNameToHeroRootName(string iconName)
        {
            if (iconName.IndexOf("Knight", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Knight";
            if (iconName.IndexOf("Archer", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Archer";
            if (iconName.IndexOf("Cleric", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Cleric";
            return null;
        }

        void TryBindLvIndicator()
        {
            if (lvIndicator != null)
                return;

            var t = transform.Find("LvIndicator");
            if (t != null)
                lvIndicator = t.gameObject;
        }

        void Refresh()
        {
            if (lvIndicator == null)
                return;

            var show = hero != null && hero.AvailableSkillPoints > 0;
            if (lvIndicator.activeSelf != show)
                lvIndicator.SetActive(show);
        }
    }
}
