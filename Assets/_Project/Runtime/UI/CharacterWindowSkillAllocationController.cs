using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TheLastTowerDefence.Formulas;
using TheLastTowerDefence.Heroes.Systems;

namespace TheLastTowerDefence.UI
{
    /// <summary>
    /// Вкладка «Характеристики»: пул очков <c>AvailableContent</c>, кнопки Plus/Minus по строкам статов.
    /// Логика очков и сохранение — в <see cref="CharacterHeroStats"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterWindowSkillAllocationController : MonoBehaviour
    {
        [SerializeField] CharacterHeroStats hero;

        [SerializeField] TMP_Text availableContent;

        readonly (string rowName, HeroStatKind stat)[] _rows =
        {
            ("Strength", HeroStatKind.Strength),
            ("Dexterity", HeroStatKind.Dexterity),
            ("Stamina", HeroStatKind.Stamina),
            ("Intelligence", HeroStatKind.Intelligence),
            ("Willpower", HeroStatKind.Willpower),
            ("Luck", HeroStatKind.Luck),
        };

        Button[] _plusButtons;
        Button[] _minusButtons;
        bool _wired;

        void Awake()
        {
            ResolveHeroIfMissing();
            BindAvailableContentIfMissing();
            AllocateButtonArrays();
            WireRowButtons();
            _wired = true;
        }

        void OnEnable()
        {
            ResolveHeroIfMissing();
            if (hero != null)
            {
                hero.DerivedStatsChanged += OnDerivedStatsChanged;
            }

            RefreshUi();
        }

        void OnDisable()
        {
            if (hero != null)
            {
                hero.DerivedStatsChanged -= OnDerivedStatsChanged;
            }
        }

        void OnDerivedStatsChanged() => RefreshUi();

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

        void BindAvailableContentIfMissing()
        {
            if (availableContent != null)
            {
                return;
            }

            var texts = GetComponentsInChildren<TMP_Text>(true);
            for (var i = 0; i < texts.Length; i++)
            {
                if (texts[i].name == "AvailableContent")
                {
                    availableContent = texts[i];
                    return;
                }
            }
        }

        void AllocateButtonArrays()
        {
            _plusButtons = new Button[_rows.Length];
            _minusButtons = new Button[_rows.Length];
        }

        void WireRowButtons()
        {
            for (var i = 0; i < _rows.Length; i++)
            {
                var row = transform.Find(_rows[i].rowName);
                if (row == null)
                {
                    continue;
                }

                var plusT = row.Find("Plus");
                var minusT = row.Find("Minus");
                if (plusT != null)
                {
                    _plusButtons[i] = plusT.GetComponent<Button>();
                }

                if (minusT != null)
                {
                    _minusButtons[i] = minusT.GetComponent<Button>();
                }

                var capturedStat = _rows[i].stat;
                if (_plusButtons[i] != null)
                {
                    _plusButtons[i].onClick.AddListener(() => OnPlusClicked(capturedStat));
                }

                if (_minusButtons[i] != null)
                {
                    _minusButtons[i].onClick.AddListener(() => OnMinusClicked(capturedStat));
                }
            }
        }

        void OnDestroy()
        {
            if (!_wired)
            {
                return;
            }

            for (var i = 0; i < _rows.Length; i++)
            {
                if (_plusButtons[i] != null)
                {
                    _plusButtons[i].onClick.RemoveAllListeners();
                }

                if (_minusButtons[i] != null)
                {
                    _minusButtons[i].onClick.RemoveAllListeners();
                }
            }
        }

        void OnPlusClicked(HeroStatKind stat)
        {
            if (hero == null)
            {
                return;
            }

            if (hero.TrySpendSkillPoint(stat))
            {
                RefreshUi();
            }
        }

        void OnMinusClicked(HeroStatKind stat)
        {
            if (hero == null)
            {
                return;
            }

            if (hero.TryRefundSessionSkillPoint(stat))
            {
                RefreshUi();
            }
        }

        void RefreshUi()
        {
            if (availableContent != null)
            {
                if (hero == null || hero.Config == null)
                {
                    availableContent.text = "--";
                }
                else
                {
                    availableContent.text = hero.AvailableSkillPoints.ToString(CultureInfo.InvariantCulture);
                }
            }

            var canPlus = hero != null && hero.Config != null && hero.AvailableSkillPoints > 0;
            for (var i = 0; i < _rows.Length; i++)
            {
                if (_plusButtons != null && _plusButtons[i] != null)
                {
                    _plusButtons[i].interactable = canPlus;
                }

                if (_minusButtons != null && _minusButtons[i] != null && hero != null && hero.Config != null)
                {
                    _minusButtons[i].interactable = hero.CanRefundSessionSkillPoint(_rows[i].stat);
                }
                else if (_minusButtons != null && _minusButtons[i] != null)
                {
                    _minusButtons[i].interactable = false;
                }
            }
        }
    }
}
