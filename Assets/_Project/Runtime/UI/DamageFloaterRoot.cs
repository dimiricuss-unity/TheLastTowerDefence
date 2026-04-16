using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using TheLastTowerDefence.Enemies.Systems;

namespace TheLastTowerDefence.UI
{
    /// <summary>
    /// Общий world-space Canvas: пул всплывающих цифр урона (вверх и вбок).
    /// Точка спавна — центр по X и верх bounds (как у полоски HP).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DamageFloaterRoot : MonoBehaviour
    {
        public static DamageFloaterRoot Instance { get; private set; }

        [SerializeField] int prewarmCount = 12;
        [SerializeField] float floatUpWorld = 0.42f;
        [SerializeField] float floatSideWorld = 0.32f;
        [SerializeField] float floatDuration = 0.72f;
        [SerializeField, Range(0f, 1f)] float fadeStartNormalized = 0.38f;
        [SerializeField] float spawnYOffsetWorld = 0.04f;
        [SerializeField] Color damageTextColor = new Color(1f, 0.92f, 0.35f, 1f);
        [SerializeField, Min(1f)] float damageTextFontSize = 44f;

        readonly Stack<DamageFloaterInstance> _pool = new Stack<DamageFloaterInstance>();
        GameObject _template;

        public float FloatUpWorld => floatUpWorld;
        public float FloatSideWorld => floatSideWorld;
        public float FloatDuration => floatDuration;
        public float FadeStartNormalized => fadeStartNormalized;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            var canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                if (canvas.worldCamera == null)
                    canvas.worldCamera = Camera.main;
            }

            EnsureTemplate();
            for (var i = 0; i < prewarmCount; i++)
            {
                var inst = Instantiate(_template, transform).GetComponent<DamageFloaterInstance>();
                inst.Initialize(this);
                inst.gameObject.SetActive(false);
                _pool.Push(inst);
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>Вызывается из <see cref="EnemyHealth.ApplyDamage"/>.</summary>
        public static void ShowAtEnemy(EnemyHealth health, float damage)
        {
            if (health == null || damage <= 0f || Instance == null)
                return;

            Instance.SpawnInternal(Instance.ComputeSpawnWorldPosition(health), damage);
        }

        void SpawnInternal(Vector3 worldPosition, float amount)
        {
            var inst = _pool.Count > 0 ? _pool.Pop() : CreateFloater();
            var rt = (RectTransform)inst.transform;
            rt.position = worldPosition;
            inst.gameObject.SetActive(true);
            inst.Play(amount);
        }

        internal void ReturnToPool(DamageFloaterInstance inst)
        {
            if (inst == null)
                return;
            inst.StopPlayback();
            inst.gameObject.SetActive(false);
            _pool.Push(inst);
        }

        DamageFloaterInstance CreateFloater()
        {
            var inst = Instantiate(_template, transform).GetComponent<DamageFloaterInstance>();
            inst.Initialize(this);
            return inst;
        }

        void EnsureTemplate()
        {
            if (_template != null)
                return;

            var go = new GameObject("DamageFloaterTemplate", typeof(RectTransform), typeof(CanvasGroup), typeof(TextMeshProUGUI), typeof(DamageFloaterInstance));
            go.transform.SetParent(transform, false);

            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(200f, 72f);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = damageTextFontSize;
            tmp.color = damageTextColor;
            tmp.enableAutoSizing = false;
            tmp.raycastTarget = false;
            tmp.outlineWidth = 0.22f;
            tmp.outlineColor = new Color32(0, 0, 0, 200);
            if (tmp.font == null && TMP_Settings.defaultFontAsset != null)
                tmp.font = TMP_Settings.defaultFontAsset;

            var cg = go.GetComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;

            go.GetComponent<DamageFloaterInstance>().Initialize(this);
            go.SetActive(false);
            _template = go;
        }

        Vector3 ComputeSpawnWorldPosition(EnemyHealth health)
        {
            var yGap = Mathf.Max(0f, spawnYOffsetWorld);
            var rend = health.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                var b = rend.bounds;
                return new Vector3(b.center.x, b.max.y + yGap, b.center.z);
            }

            var col = health.GetComponentInChildren<Collider2D>();
            if (col != null)
            {
                var b = col.bounds;
                return new Vector3(b.center.x, b.max.y + yGap, b.center.z);
            }

            return health.transform.position + Vector3.up * (0.5f + yGap);
        }
    }

    /// <summary>Один экземпляр цифры; создаётся только из <see cref="DamageFloaterRoot"/>.</summary>
    public sealed class DamageFloaterInstance : MonoBehaviour
    {
        DamageFloaterRoot _root;
        RectTransform _rt;
        CanvasGroup _cg;
        TextMeshProUGUI _tmp;
        Coroutine _co;

        public void Initialize(DamageFloaterRoot root)
        {
            _root = root;
            _rt = (RectTransform)transform;
            _cg = GetComponent<CanvasGroup>();
            _tmp = GetComponent<TextMeshProUGUI>();
        }

        public void StopPlayback()
        {
            if (_co != null)
            {
                StopCoroutine(_co);
                _co = null;
            }
        }

        public void Play(float damageAmount)
        {
            StopPlayback();
            _co = StartCoroutine(PlayRoutine(damageAmount));
        }

        IEnumerator PlayRoutine(float damage)
        {
            _tmp.text = Mathf.RoundToInt(damage).ToString();
            _cg.alpha = 1f;

            var start = _rt.position;
            var side = Random.value < 0.5f ? -1f : 1f;
            var end = start + Vector3.up * _root.FloatUpWorld + Vector3.right * side * _root.FloatSideWorld;

            var duration = Mathf.Max(0.05f, _root.FloatDuration);
            var fadeStart = Mathf.Clamp01(_root.FadeStartNormalized) * duration;
            var t = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                var k = Mathf.Clamp01(t / duration);
                var ease = 1f - (1f - k) * (1f - k);
                _rt.position = Vector3.Lerp(start, end, ease);

                if (t >= fadeStart)
                {
                    var fk = Mathf.InverseLerp(fadeStart, duration, t);
                    _cg.alpha = 1f - fk;
                }

                var popT = Mathf.Clamp01(k / 0.14f);
                var pop = Mathf.Lerp(0.45f, 1f, popT);
                _rt.localScale = new Vector3(pop, pop, 1f);

                yield return null;
            }

            _rt.position = end;
            _cg.alpha = 0f;
            _rt.localScale = Vector3.one;
            _co = null;
            gameObject.SetActive(false);
            _root.ReturnToPool(this);
        }
    }
}
