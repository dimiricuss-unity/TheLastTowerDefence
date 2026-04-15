using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheLastTowerDefence.CoreLoop.Battle
{
    /// <summary>
    /// UI полоски башни: зелёная — фактический HP сразу, белая — догоняет плавно, текст — текущее / макс.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TowerHitBarView : MonoBehaviour
    {
        [SerializeField] TowerDamageable tower;
        [SerializeField] Image greenBar;
        [SerializeField] Image whiteBar;
        [SerializeField] TMP_Text hitText;
        [SerializeField] float whiteCatchUpSpeed = 8f;

        void Awake()
        {
            if (tower == null)
                tower = FindObjectOfType<TowerDamageable>();
            TryAutoWireImages();
        }

        void OnEnable()
        {
            if (tower != null)
            {
                tower.HealthChanged += OnHealthChanged;
                OnHealthChanged(tower.CurrentHealth, tower.MaxHealth);
                if (whiteBar != null && greenBar != null)
                    whiteBar.fillAmount = greenBar.fillAmount;
            }
        }

        void OnDisable()
        {
            if (tower != null)
                tower.HealthChanged -= OnHealthChanged;
        }

        void Update()
        {
            if (whiteBar == null || greenBar == null)
                return;

            var target = greenBar.fillAmount;
            var t = 1f - Mathf.Exp(-whiteCatchUpSpeed * Time.deltaTime);
            var next = Mathf.Lerp(whiteBar.fillAmount, target, t);
            if (Mathf.Abs(next - target) < 0.0005f)
                next = target;
            whiteBar.fillAmount = next;
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
                greenBar = transform.Find("HitBar/BlackBack/GreenBar")?.GetComponent<Image>();
            if (whiteBar == null)
                whiteBar = transform.Find("HitBar/BlackBack/WhiteBar")?.GetComponent<Image>();
            if (hitText == null)
                hitText = transform.Find("HitBar/BlackBack/HitText")?.GetComponent<TMP_Text>();
        }
    }
}
