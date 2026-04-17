using System;
using UnityEngine;
using UnityEngine.UI;

namespace TheLastTowerDefence.Heroes.Systems
{
    /// <summary>
    /// UI полоска перезарядки атаки (например <c>Knight_icon/ReloadBar</c>).
    /// <see cref="Image.fillAmount"/>: <b>1</b> — готов к удару; после удара полоска пустеет и <b>заполняется</b> по мере кулдауна APS.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterHeroReloadBarView : MonoBehaviour
    {
        const string RangeHeroTag = "RangeHero";

        [Tooltip("Стат героя в сцене. Если пусто — один герой в сцене; иначе подбор по имени иконки (Knight / Archer).")]
        [SerializeField] CharacterHeroStats hero;

        [Tooltip("Белая полоска (Image, лучше Filled Horizontal). Пусто — авто-поиск под ReloadBar.")]
        [SerializeField] Image reloadFill;

        CharacterHeroMeleeAttack _melee;
        CharacterHeroRangeAttack _range;

        void Awake()
        {
            TryResolveHero();
            TryAutoWireReloadImage();
            CacheAttackComponents();
        }

        void OnEnable()
        {
            RefreshFill();
        }

        void Update()
        {
            RefreshFill();
        }

        void RefreshFill()
        {
            if (reloadFill == null)
                return;

            if (hero == null || !hero.IsAlive)
            {
                reloadFill.fillAmount = 0f;
                return;
            }

            var remaining = GetCooldownRemaining01();
            reloadFill.fillAmount = Mathf.Clamp01(1f - remaining);
        }

        float GetCooldownRemaining01()
        {
            if (_range != null)
                return _range.AttackCooldownRemaining01;
            if (_melee != null)
                return _melee.AttackCooldownRemaining01;
            return 0f;
        }

        void CacheAttackComponents()
        {
            if (hero == null)
                return;
            _melee = hero.GetComponentInChildren<CharacterHeroMeleeAttack>(true);
            _range = hero.GetComponentInChildren<CharacterHeroRangeAttack>(true);
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
                if (icon.IndexOf("Knight", StringComparison.OrdinalIgnoreCase) >= 0 &&
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
            }
        }

        void TryAutoWireReloadImage()
        {
            if (reloadFill != null)
                return;

            reloadFill = transform.Find("ReloadBar/BlackBack/WhiteBar")?.GetComponent<Image>()
                         ?? transform.Find("ReloadBar/WhiteBar")?.GetComponent<Image>()
                         ?? transform.Find("ReloadBar")?.GetComponent<Image>();
        }
    }
}
