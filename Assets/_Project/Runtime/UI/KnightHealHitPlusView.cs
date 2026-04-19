using System.Collections;
using TMPro;
using UnityEngine;

namespace TheLastTowerDefence.UI
{
    /// <summary>
    /// Показ +N HP на <c>Knight_icon</c> (поле <see cref="hitPlus"/> — объект HitPlus с TMP).
    /// Движение вверх и затухание без DOTween (чтобы не тянуть сборку плагина в asmdef).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KnightHealHitPlusView : MonoBehaviour
    {
        [Tooltip("Текст HitPlus (обычно дочерний TMP на Knight_icon). Пусто — ищется Transform с именем HitPlus.")]
        [SerializeField] TMP_Text hitPlus;

        [SerializeField, Min(1f)] float moveUpPixels = 48f;

        [SerializeField, Min(0.05f)] float duration = 0.85f;

        [SerializeField] Color healColor = new Color(0.45f, 1f, 0.55f, 1f);

        RectTransform _hitPlusRect;
        Vector2 _baseAnchoredPosition;
        Coroutine _routine;

        void Awake()
        {
            if (hitPlus == null)
            {
                var t = transform.Find("HitPlus");
                if (t == null)
                {
                    foreach (var c in GetComponentsInChildren<Transform>(true))
                    {
                        if (c.name == "HitPlus")
                        {
                            t = c;
                            break;
                        }
                    }
                }

                if (t != null)
                    hitPlus = t.GetComponent<TMP_Text>();
            }

            if (hitPlus != null)
            {
                _hitPlusRect = hitPlus.rectTransform;
                _baseAnchoredPosition = _hitPlusRect.anchoredPosition;
                hitPlus.gameObject.SetActive(false);
            }
        }

        /// <summary>Запускает показ числа восстановленного HP.</summary>
        public void ShowHealAmount(float amount)
        {
            if (hitPlus == null || amount <= 0f)
                return;

            if (_routine != null)
                StopCoroutine(_routine);

            _routine = StartCoroutine(PlayRoutine(amount));
        }

        IEnumerator PlayRoutine(float amount)
        {
            var rt = _hitPlusRect;
            var c = healColor;
            c.a = 1f;
            hitPlus.color = c;
            hitPlus.text = $"+{Mathf.RoundToInt(amount)}";
            rt.anchoredPosition = _baseAnchoredPosition;
            hitPlus.gameObject.SetActive(true);

            var endPos = _baseAnchoredPosition + new Vector2(0f, moveUpPixels);
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var k = Mathf.Clamp01(elapsed / duration);
                var ease = 1f - (1f - k) * (1f - k);
                rt.anchoredPosition = Vector2.Lerp(_baseAnchoredPosition, endPos, ease);
                var ca = healColor;
                ca.a = 1f - k;
                hitPlus.color = ca;
                yield return null;
            }

            rt.anchoredPosition = endPos;
            var endC = healColor;
            endC.a = 0f;
            hitPlus.color = endC;
            hitPlus.gameObject.SetActive(false);
            rt.anchoredPosition = _baseAnchoredPosition;
            _routine = null;
        }
    }
}
