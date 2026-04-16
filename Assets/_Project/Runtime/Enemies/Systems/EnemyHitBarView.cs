using UnityEngine;
using UnityEngine.UI;

namespace TheLastTowerDefence.Enemies.Systems
{
    /// <summary>
    /// World-space полоска HP над врагом: зелёная — текущее HP, белая — догоняет (как у башни).
    /// Стоит по центру сверху (по bounds коллайдера/рендера), поворот в мире без наклона вместе с врагом.
    /// По умолчанию скрыта (<see cref="CanvasGroup.alpha"/> = 0) и плавно показывается после урона на короткое время.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyHitBarView : MonoBehaviour
    {
        [SerializeField] EnemyHealth health;
        [SerializeField] Image greenBar;
        [SerializeField] Image whiteBar;
        [SerializeField] float whiteCatchUpSpeed = 8f;
        [Tooltip("Полоска невидима, пока враг не получит урон; затем на время visibleAfterDamageSeconds.")]
        [SerializeField] bool showOnlyAfterDamage = true;
        [Tooltip("Сколько секунд держать полоску после последнего урона (таймер обновляется при каждом попадании).")]
        [SerializeField] float visibleAfterDamageSeconds = 2.2f;
        [Tooltip("Скорость нарастания альфы CanvasGroup при появлении.")]
        [SerializeField] float fadeInSpeed = 4f;
        [Tooltip("Скорость спада альфы после таймера.")]
        [SerializeField] float fadeOutSpeed = 2.2f;
        [Tooltip("Каждый кадр ставить полоску над центром по X и чуть выше верхней границы тела врага.")]
        [SerializeField] bool positionOverTopCenter = true;
        [Tooltip("Зазор в мировых единицах над верхом bounds (Collider2D или Renderer).")]
        [SerializeField] float verticalGapWorld = 0.06f;
        [Tooltip("Сглаживание якоря полоски (bounds спрайта дёргаются на анимации). 0 — без сглаживания, как раньше.")]
        [SerializeField] float positionSmoothTime = 0.12f;
        [SerializeField] bool keepWorldRotationFlat = true;
        [SerializeField] bool hideOnDeath = true;

        CanvasGroup _canvasGroup;
        float _lastHealthForReveal;
        bool _revealHealthInitialized;
        float _visibleUntilTime;

        Vector3 _smoothedPosition;
        Vector3 _positionSmoothVelocity;

        void Awake()
        {
            if (health == null)
                health = GetComponentInParent<EnemyHealth>();
            TryAutoWireImages();
            if (showOnlyAfterDamage)
                EnsureCanvasGroup();
        }

        void OnEnable()
        {
            if (health == null)
                return;

            health.HealthChanged += OnHealthChanged;
            health.Died += OnEnemyDied;
            _revealHealthInitialized = false;
            if (showOnlyAfterDamage && _canvasGroup != null)
                _canvasGroup.alpha = 0f;

            OnHealthChanged(health.CurrentHealth, health.MaxHealth);
            if (whiteBar != null && greenBar != null)
                whiteBar.fillAmount = greenBar.fillAmount;

            if (positionOverTopCenter)
            {
                _smoothedPosition = GetTopCenterWorldPosition(health, verticalGapWorld);
                _positionSmoothVelocity = Vector3.zero;
            }
        }

        void OnDisable()
        {
            if (health == null)
                return;

            health.HealthChanged -= OnHealthChanged;
            health.Died -= OnEnemyDied;
        }

        void Update()
        {
            UpdateVisibilityAlpha();

            if (whiteBar == null || greenBar == null)
                return;

            var target = greenBar.fillAmount;
            var t = 1f - Mathf.Exp(-whiteCatchUpSpeed * Time.deltaTime);
            var next = Mathf.Lerp(whiteBar.fillAmount, target, t);
            if (Mathf.Abs(next - target) < 0.0005f)
                next = target;
            whiteBar.fillAmount = next;
        }

        void EnsureCanvasGroup()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        void UpdateVisibilityAlpha()
        {
            if (!showOnlyAfterDamage || _canvasGroup == null)
                return;

            var wantOpaque = Time.time < _visibleUntilTime;
            var targetAlpha = wantOpaque ? 1f : 0f;
            var speed = wantOpaque ? fadeInSpeed : fadeOutSpeed;
            _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, targetAlpha, speed * Time.deltaTime);
        }

        void BumpDamageVisibility()
        {
            _visibleUntilTime = Time.time + Mathf.Max(0.1f, visibleAfterDamageSeconds);
        }

        void LateUpdate()
        {
            if (health != null && positionOverTopCenter)
            {
                var target = GetTopCenterWorldPosition(health, verticalGapWorld);
                if (positionSmoothTime <= 0f)
                    transform.position = target;
                else
                {
                    _smoothedPosition = Vector3.SmoothDamp(
                        _smoothedPosition,
                        target,
                        ref _positionSmoothVelocity,
                        positionSmoothTime,
                        Mathf.Infinity,
                        Time.deltaTime);
                    transform.position = _smoothedPosition;
                }
            }

            if (keepWorldRotationFlat)
                transform.rotation = Quaternion.identity;
        }

        static Vector3 GetTopCenterWorldPosition(EnemyHealth enemy, float yGap)
        {
            if (enemy == null)
                return default;

            // Сначала видимый спрайт — центр по X и «крыша» по Y совпадают с картинкой.
            var rend = enemy.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                var b = rend.bounds;
                return new Vector3(b.center.x, b.max.y + yGap, b.center.z);
            }

            var col = enemy.GetComponentInChildren<Collider2D>();
            if (col != null)
            {
                var b = col.bounds;
                return new Vector3(b.center.x, b.max.y + yGap, b.center.z);
            }

            return enemy.transform.position + Vector3.up * (0.5f + yGap);
        }

        void OnHealthChanged(float current, float max)
        {
            if (showOnlyAfterDamage)
            {
                if (_revealHealthInitialized && current + 1e-4f < _lastHealthForReveal)
                    BumpDamageVisibility();
                _revealHealthInitialized = true;
                _lastHealthForReveal = current;
            }

            var maxSafe = Mathf.Max(1f, max);
            var norm = Mathf.Clamp01(current / maxSafe);

            if (greenBar != null)
                greenBar.fillAmount = norm;
        }

        void OnEnemyDied(EnemyHealth _)
        {
            if (!hideOnDeath)
                return;

            gameObject.SetActive(false);
        }

        void TryAutoWireImages()
        {
            if (greenBar == null)
                greenBar = transform.Find("BlackBack/GreenLine")?.GetComponent<Image>();
            if (whiteBar == null)
                whiteBar = transform.Find("BlackBack/WhiteBack")?.GetComponent<Image>();
        }
    }
}
