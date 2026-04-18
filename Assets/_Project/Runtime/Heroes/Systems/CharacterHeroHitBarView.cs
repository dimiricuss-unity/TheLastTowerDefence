using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TheLastTowerDefence.UI;

namespace TheLastTowerDefence.Heroes.Systems
{
    /// <summary>
    /// UI HP рыцаря: зелёная полоса — текущее HP сразу, белая плавно догоняет (как у башни).
    /// Вешается на объект с дочерним <c>HitBar</c> (например Knight_icon).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterHeroHitBarView : MonoBehaviour
    {
        [SerializeField] CharacterHeroStats hero;
        [SerializeField] Image greenBar;
        [SerializeField] Image whiteBar;
        [SerializeField] TMP_Text hitText;
        [SerializeField] float whiteCatchUpSpeed = 8f;

        void Awake()
        {
            if (hero == null)
                hero = FindObjectOfType<CharacterHeroStats>();
            TryAutoWireImages();
        }

        void OnEnable()
        {
            if (hero != null)
            {
                hero.HealthChanged += OnHealthChanged;
                OnHealthChanged(hero.CurrentHealth, hero.MaxHealth);
                if (whiteBar != null && greenBar != null)
                    whiteBar.fillAmount = greenBar.fillAmount;
            }
        }

        void OnDisable()
        {
            if (hero != null)
                hero.HealthChanged -= OnHealthChanged;
        }

        void Update()
        {
            UiFilledImageCatchUp.Tick(whiteBar, greenBar, whiteCatchUpSpeed);
        }

        void OnHealthChanged(float current, float max)
        {
            var maxSafe = Mathf.Max(1f, max);
            var norm = Mathf.Clamp01(current / maxSafe);

            if (greenBar != null)
                greenBar.fillAmount = norm;

            if (hitText != null)
                hitText.text = $"{Mathf.RoundToInt(current)} / {Mathf.RoundToInt(maxSafe)}";
        }

        void TryAutoWireImages()
        {
            if (greenBar == null)
            {
                greenBar = transform.Find("HitBar/BlackBack/GreenBar")?.GetComponent<Image>()
                           ?? transform.Find("HitBar/GreenBar")?.GetComponent<Image>();
            }

            if (whiteBar == null)
            {
                whiteBar = transform.Find("HitBar/BlackBack/WhiteBar")?.GetComponent<Image>()
                           ?? transform.Find("HitBar/WhiteBar")?.GetComponent<Image>();
            }

            if (hitText == null)
                hitText = transform.Find("HitBar/BlackBack/HitText")?.GetComponent<TMP_Text>();
        }
    }
}
