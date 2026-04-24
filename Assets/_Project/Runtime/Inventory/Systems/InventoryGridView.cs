using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TheLastTowerDefence.Inventory.Systems
{
    [DisallowMultipleComponent]
    public sealed class InventoryGridView : MonoBehaviour
    {
        [SerializeField] private GridLayoutGroup gridCellsLayer;
        [SerializeField] private RectTransform itemsLayer;
        [SerializeField] private RectTransform viewport;

        [Header("Scroll")]
        [SerializeField] private bool enableVerticalScroll = true;
        [SerializeField] private float scrollWheelSensitivity = 40f;
        [Tooltip("Сколько строк сетки за один клик ScrollUp / ScrollDown.")]
        [SerializeField] private int scrollStepInRows = 5;
        [Tooltip("Длительность плавной прокрутки по кнопкам (unscaled).")]
        [SerializeField] private float scrollButtonAnimDuration = 0.16f;

        [Header("Toolbar (родитель — обычно Inventory; кнопки рядом с GridViewport)")]
        [SerializeField] private Button scrollUpButton;
        [SerializeField] private Button scrollDownButton;
        [SerializeField] private Button sortInventoryButton;

        [Header("Drag placement highlight")]
        [SerializeField] private Color validPlacementCellTint = new Color(0.25f, 0.85f, 0.4f, 1f);
        [SerializeField] private Color invalidPlacementCellTint = new Color(0.95f, 0.35f, 0.3f, 1f);
        [SerializeField] private bool showInvalidPlacementTint;
        [SerializeField] [Range(0f, 1f)] private float highlightBlend = 0.55f;

        public RectTransform ItemsLayer => itemsLayer;

        /// <summary>
        /// Попадает ли экранная точка во вьюпорт сетки (как для подсветки / размещения по указателю).
        /// </summary>
        public bool ContainsScreenPoint(Vector2 screenPosition, Camera eventCamera)
        {
            BindMissingReferences();
            var rect = viewport != null ? viewport : itemsLayer;
            return rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, eventCamera);
        }

        private Image[] _cellBackgroundImages;
        private Color[] _cellOriginalColors;
        private bool _cellColorsCached;
        private ScrollRect _scrollRect;
        private Transform _sortHoldRoot;
        private bool _toolbarListenersBound;
        private Coroutine _scrollButtonRoutine;

        private void Awake()
        {
            BindMissingReferences();
            SetupVerticalScrollIfNeeded();
            TryAutoAssignToolbarButtons();
            BindToolbarListeners();
        }

        private void OnDestroy()
        {
            if (_scrollButtonRoutine != null)
            {
                StopCoroutine(_scrollButtonRoutine);
                _scrollButtonRoutine = null;
            }

            UnbindToolbarListeners();
        }

        private void OnEnable()
        {
            RefreshScrollableContentDimensions();
        }

        private void OnRectTransformDimensionsChange()
        {
            RefreshScrollableContentDimensions();
        }

        private void OnValidate()
        {
            BindMissingReferences();
            TryAutoAssignToolbarButtons();
            if (Application.isPlaying)
            {
                UnbindToolbarListeners();
                BindToolbarListeners();
            }
        }

        /// <summary>
        /// Прокрутка вверх (к началу списка) примерно на <see cref="scrollStepInRows"/> ячеек по высоте.
        /// </summary>
        public void ScrollUpByStep()
        {
            ScrollByRowDelta(scrollStepInRows);
        }

        /// <summary>
        /// Прокрутка вниз (к концу списка).
        /// </summary>
        public void ScrollDownByStep()
        {
            ScrollByRowDelta(-scrollStepInRows);
        }

        /// <summary>
        /// Упорядочивает предметы по имени конфига и уплотняет слева направо, сверху вниз.
        /// </summary>
        public void SortAndPackInventoryItems()
        {
            BindMissingReferences();
            if (itemsLayer == null || gridCellsLayer == null)
            {
                return;
            }

            var buffer = new List<InventoryItemView>(itemsLayer.childCount);
            for (var i = 0; i < itemsLayer.childCount; i++)
            {
                var iv = itemsLayer.GetChild(i).GetComponent<InventoryItemView>();
                if (iv != null && iv.Config != null)
                {
                    buffer.Add(iv);
                }
            }

            if (buffer.Count == 0)
            {
                return;
            }

            buffer.Sort(CompareItemsByConfigName);

            var hold = GetOrCreateSortHoldRoot();
            for (var i = 0; i < buffer.Count; i++)
            {
                buffer[i].transform.SetParent(hold, true);
            }

            for (var i = 0; i < buffer.Count; i++)
            {
                if (!TryPlaceItemAtFirstFreeCell(buffer[i]))
                {
                    buffer[i].transform.SetParent(itemsLayer, false);
                }
            }

            for (var i = 0; i < buffer.Count; i++)
            {
                if (buffer[i] != null && buffer[i].transform.parent == hold)
                {
                    buffer[i].transform.SetParent(itemsLayer, false);
                }
            }

            if (_scrollRect != null && enableVerticalScroll)
            {
                _scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private static int CompareItemsByConfigName(InventoryItemView a, InventoryItemView b)
        {
            if (a == null || a.Config == null)
            {
                return b == null || b.Config == null ? 0 : 1;
            }

            if (b == null || b.Config == null)
            {
                return -1;
            }

            return string.CompareOrdinal(a.Config.name, b.Config.name);
        }

        private Transform GetOrCreateSortHoldRoot()
        {
            if (_sortHoldRoot != null)
            {
                return _sortHoldRoot;
            }

            var parent = itemsLayer != null ? itemsLayer.parent : transform;
            var existing = parent != null ? parent.Find("_InventorySortHold") : null;
            if (existing != null)
            {
                _sortHoldRoot = existing;
                return _sortHoldRoot;
            }

            var go = new GameObject("_InventorySortHold", typeof(RectTransform));
            _sortHoldRoot = go.transform;
            _sortHoldRoot.SetParent(parent, false);
            var rt = (RectTransform)_sortHoldRoot;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = new Vector2(8000f, 8000f);
            return _sortHoldRoot;
        }

        private void ScrollByRowDelta(int signedRows)
        {
            if (!enableVerticalScroll || _scrollRect == null || gridCellsLayer == null)
            {
                return;
            }

            var content = _scrollRect.content;
            var vp = _scrollRect.viewport;
            if (content == null || vp == null)
            {
                return;
            }

            var scrollable = content.rect.height - vp.rect.height;
            if (scrollable <= 1f)
            {
                return;
            }

            var deltaNorm = (signedRows * GetPitchY()) / scrollable;
            var from = _scrollRect.verticalNormalizedPosition;
            var to = Mathf.Clamp01(from + deltaNorm);
            if (Mathf.Approximately(from, to))
            {
                return;
            }

            if (_scrollButtonRoutine != null)
            {
                StopCoroutine(_scrollButtonRoutine);
            }

            _scrollButtonRoutine = StartCoroutine(SmoothScrollVerticalRoutine(from, to));
        }

        private IEnumerator SmoothScrollVerticalRoutine(float from, float to)
        {
            var dur = Mathf.Max(0.02f, scrollButtonAnimDuration);
            var elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                var u = Mathf.Clamp01(elapsed / dur);
                u = u * u * (3f - 2f * u);
                _scrollRect.verticalNormalizedPosition = Mathf.Lerp(from, to, u);
                yield return null;
            }

            _scrollRect.verticalNormalizedPosition = to;
            _scrollButtonRoutine = null;
        }

        private void TryAutoAssignToolbarButtons()
        {
            var root = transform.parent;
            if (root == null)
            {
                return;
            }

            if (scrollUpButton == null)
            {
                scrollUpButton = root.Find("ScrollUp")?.GetComponent<Button>();
            }

            if (scrollDownButton == null)
            {
                scrollDownButton = root.Find("ScrollDown")?.GetComponent<Button>();
            }

            if (sortInventoryButton == null)
            {
                sortInventoryButton = root.Find("Sorting")?.GetComponent<Button>();
            }
        }

        private void BindToolbarListeners()
        {
            UnbindToolbarListeners();

            if (scrollUpButton != null)
            {
                scrollUpButton.onClick.AddListener(ScrollUpByStep);
                _toolbarListenersBound = true;
            }

            if (scrollDownButton != null)
            {
                scrollDownButton.onClick.AddListener(ScrollDownByStep);
                _toolbarListenersBound = true;
            }

            if (sortInventoryButton != null)
            {
                sortInventoryButton.onClick.AddListener(SortAndPackInventoryItems);
                _toolbarListenersBound = true;
            }
        }

        private void UnbindToolbarListeners()
        {
            if (scrollUpButton != null)
            {
                scrollUpButton.onClick.RemoveListener(ScrollUpByStep);
            }

            if (scrollDownButton != null)
            {
                scrollDownButton.onClick.RemoveListener(ScrollDownByStep);
            }

            if (sortInventoryButton != null)
            {
                sortInventoryButton.onClick.RemoveListener(SortAndPackInventoryItems);
            }

            _toolbarListenersBound = false;
        }

        private void SetupVerticalScrollIfNeeded()
        {
            if (!enableVerticalScroll)
            {
                return;
            }

            BindMissingReferences();
            if (itemsLayer == null || gridCellsLayer == null)
            {
                return;
            }

            var content = itemsLayer.parent as RectTransform;
            if (content == null)
            {
                return;
            }

            _scrollRect = GetComponent<ScrollRect>();
            if (_scrollRect == null)
            {
                _scrollRect = gameObject.AddComponent<ScrollRect>();
            }

            var vp = viewport != null ? viewport : transform as RectTransform;
            _scrollRect.viewport = vp;
            _scrollRect.content = content;
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _scrollRect.scrollSensitivity = scrollWheelSensitivity;
            _scrollRect.inertia = true;

            ApplyScrollContentAnchors(content);
            RefreshScrollableContentDimensions();
        }

        private static void ApplyScrollContentAnchors(RectTransform content)
        {
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
        }

        private void RefreshScrollableContentDimensions()
        {
            if (!enableVerticalScroll || _scrollRect == null || gridCellsLayer == null || _scrollRect.content == null)
            {
                return;
            }

            var columns = Mathf.Max(1, gridCellsLayer.constraintCount);
            var cellCount = gridCellsLayer.transform.childCount;
            var rows = Mathf.Max(1, Mathf.CeilToInt(cellCount / (float)columns));
            var pad = gridCellsLayer.padding;
            var height = pad.top + pad.bottom + (rows * gridCellsLayer.cellSize.y) + (Mathf.Max(0, rows - 1) * gridCellsLayer.spacing.y);

            var content = _scrollRect.content;
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

            var vp = _scrollRect.viewport;
            if (vp != null)
            {
                content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, vp.rect.width);
            }
        }

        /// <summary>
        /// Подсветка области под курсором: по умолчанию только если предмет можно положить (зелёный оттенок).
        /// Включите <see cref="showInvalidPlacementTint"/>, чтобы подсвечивать красным занятые/недопустимые позиции.
        /// </summary>
        public void UpdatePlacementHighlight(
            InventoryItemView itemView,
            Vector2 screenPosition,
            Camera eventCamera,
            int grabbedCellOffsetX,
            int grabbedCellOffsetY)
        {
            ClearPlacementHighlight();
            if (itemView == null || itemView.Config == null)
            {
                return;
            }

            BindMissingReferences();
            if (gridCellsLayer == null || itemsLayer == null)
            {
                return;
            }

            var rect = viewport != null ? viewport : itemsLayer;
            if (rect == null || !RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, eventCamera))
            {
                return;
            }

            if (!ScreenToCell(screenPosition, eventCamera, out var pointerCellX, out var pointerCellY))
            {
                return;
            }

            var anchorX = pointerCellX - grabbedCellOffsetX;
            var anchorY = pointerCellY - grabbedCellOffsetY;
            if (!TryBuildClampedPlacementRect(itemView, anchorX, anchorY, out var placement))
            {
                return;
            }

            var isValid = IsAreaFree(placement, itemView);
            if (!isValid && !showInvalidPlacementTint)
            {
                return;
            }

            var tint = isValid ? validPlacementCellTint : invalidPlacementCellTint;
            ApplyHighlightToRect(placement, tint);
        }

        public void ClearPlacementHighlight()
        {
            if (!_cellColorsCached || _cellBackgroundImages == null || _cellOriginalColors == null)
            {
                return;
            }

            for (var i = 0; i < _cellBackgroundImages.Length; i++)
            {
                if (_cellBackgroundImages[i] != null)
                {
                    _cellBackgroundImages[i].color = _cellOriginalColors[i];
                }
            }
        }

        private void EnsureCellImageCache()
        {
            if (_cellColorsCached || gridCellsLayer == null)
            {
                return;
            }

            var t = gridCellsLayer.transform;
            var n = t.childCount;
            _cellBackgroundImages = new Image[n];
            _cellOriginalColors = new Color[n];
            for (var i = 0; i < n; i++)
            {
                var img = t.GetChild(i).GetComponent<Image>();
                _cellBackgroundImages[i] = img;
                _cellOriginalColors[i] = img != null ? img.color : Color.white;
            }

            _cellColorsCached = true;
        }

        private void ApplyHighlightToRect(RectInt placement, Color tint)
        {
            EnsureCellImageCache();
            if (_cellBackgroundImages == null || _cellBackgroundImages.Length == 0)
            {
                return;
            }

            var columns = Mathf.Max(1, gridCellsLayer.constraintCount);
            var blend = Mathf.Clamp01(highlightBlend);
            for (var y = placement.yMin; y < placement.yMax; y++)
            {
                for (var x = placement.xMin; x < placement.xMax; x++)
                {
                    var idx = y * columns + x;
                    if (idx < 0 || idx >= _cellBackgroundImages.Length)
                    {
                        continue;
                    }

                    var img = _cellBackgroundImages[idx];
                    if (img == null)
                    {
                        continue;
                    }

                    img.color = Color.Lerp(_cellOriginalColors[idx], tint, blend);
                }
            }
        }

        public bool TryPlaceItemByPointer(
            InventoryItemView itemView,
            PointerEventData eventData,
            int grabbedCellOffsetX = 0,
            int grabbedCellOffsetY = 0)
        {
            if (itemView == null || eventData == null)
            {
                return false;
            }

            BindMissingReferences();
            if (gridCellsLayer == null || itemsLayer == null)
            {
                return false;
            }

            if (!ScreenToCell(eventData.position, eventData.pressEventCamera, out var cellX, out var cellY))
            {
                return false;
            }

            return TryPlaceItemAtCell(itemView, cellX - grabbedCellOffsetX, cellY - grabbedCellOffsetY);
        }

        /// <summary>
        /// Ищет первую свободную позицию на сетке и кладёт туда предмет (например, вытеснённый из слота экипировки).
        /// </summary>
        public bool TryPlaceItemAtFirstFreeCell(InventoryItemView itemView)
        {
            if (itemView == null || itemView.Config == null)
            {
                return false;
            }

            BindMissingReferences();
            if (gridCellsLayer == null || itemsLayer == null)
            {
                return false;
            }

            var columns = Mathf.Max(1, gridCellsLayer.constraintCount);
            var rows = Mathf.Max(1, gridCellsLayer.transform.childCount / columns);
            var w = Mathf.Max(1, itemView.Config.sizeInCellsX);
            var h = Mathf.Max(1, itemView.Config.sizeInCellsY);
            var maxX = Mathf.Max(0, columns - w);
            var maxY = Mathf.Max(0, rows - h);

            for (var y = 0; y <= maxY; y++)
            {
                for (var x = 0; x <= maxX; x++)
                {
                    if (TryPlaceItemAtCell(itemView, x, y))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool TryPlaceItemAtCell(InventoryItemView itemView, int cellX, int cellY)
        {
            if (itemView == null || itemView.Config == null)
            {
                return false;
            }

            BindMissingReferences();
            if (gridCellsLayer == null || itemsLayer == null)
            {
                return false;
            }

            if (!TryBuildClampedPlacementRect(itemView, cellX, cellY, out var candidate))
            {
                return false;
            }

            if (!IsAreaFree(candidate, itemView))
            {
                return false;
            }

            var itemRect = itemView.transform as RectTransform;
            if (itemRect == null)
            {
                return false;
            }

            itemRect.SetParent(itemsLayer, false);
            itemRect.anchorMin = new Vector2(0f, 1f);
            itemRect.anchorMax = new Vector2(0f, 1f);
            itemRect.pivot = new Vector2(0f, 1f);
            itemView.SetEquippedInSlot(false);
            itemView.ReapplyConfigToVisuals();
            itemRect.anchoredPosition = new Vector2(
                candidate.xMin * GetPitchX(),
                -candidate.yMin * GetPitchY());
            itemRect.localScale = Vector3.one;
            itemRect.SetAsLastSibling();
            itemRect.gameObject.SetActive(true);
            return true;
        }

        private bool TryBuildClampedPlacementRect(InventoryItemView itemView, int anchorCellX, int anchorCellY, out RectInt rect)
        {
            rect = default;
            if (itemView == null || itemView.Config == null || gridCellsLayer == null)
            {
                return false;
            }

            var columns = Mathf.Max(1, gridCellsLayer.constraintCount);
            var rows = Mathf.Max(1, gridCellsLayer.transform.childCount / columns);

            var widthCells = Mathf.Max(1, itemView.Config.sizeInCellsX);
            var heightCells = Mathf.Max(1, itemView.Config.sizeInCellsY);

            var maxX = Mathf.Max(0, columns - widthCells);
            var maxY = Mathf.Max(0, rows - heightCells);
            var clampedX = Mathf.Clamp(anchorCellX, 0, maxX);
            var clampedY = Mathf.Clamp(anchorCellY, 0, maxY);
            rect = new RectInt(clampedX, clampedY, widthCells, heightCells);
            return true;
        }

        private bool ScreenToCell(Vector2 screenPosition, Camera eventCamera, out int cellX, out int cellY)
        {
            cellX = 0;
            cellY = 0;

            var rect = viewport != null ? viewport : itemsLayer;
            if (rect == null)
            {
                return false;
            }

            if (!RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, eventCamera))
            {
                return false;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(itemsLayer, screenPosition, eventCamera, out var local))
            {
                return false;
            }

            var x = Mathf.Max(0f, local.x);
            var y = Mathf.Max(0f, -local.y);
            cellX = Mathf.FloorToInt(x / GetPitchX());
            cellY = Mathf.FloorToInt(y / GetPitchY());
            return true;
        }

        private bool IsAreaFree(RectInt candidate, InventoryItemView ignored)
        {
            if (itemsLayer == null)
            {
                return true;
            }

            for (var i = 0; i < itemsLayer.childCount; i++)
            {
                var child = itemsLayer.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                var otherItem = child.GetComponent<InventoryItemView>();
                if (otherItem == null || otherItem == ignored || otherItem.Config == null)
                {
                    continue;
                }

                if (!TryBuildRectForItem(otherItem, out var occupied))
                {
                    continue;
                }

                if (candidate.Overlaps(occupied))
                {
                    return false;
                }
            }

            return true;
        }

        private bool TryBuildRectForItem(InventoryItemView item, out RectInt rect)
        {
            rect = default;
            if (item == null || item.Config == null)
            {
                return false;
            }

            var rectTransform = item.transform as RectTransform;
            if (rectTransform == null)
            {
                return false;
            }

            var x = Mathf.RoundToInt(rectTransform.anchoredPosition.x / GetPitchX());
            var y = Mathf.RoundToInt(-rectTransform.anchoredPosition.y / GetPitchY());
            var w = Mathf.Max(1, item.Config.sizeInCellsX);
            var h = Mathf.Max(1, item.Config.sizeInCellsY);
            rect = new RectInt(x, y, w, h);
            return true;
        }

        private float GetPitchX() => gridCellsLayer.cellSize.x + gridCellsLayer.spacing.x;
        private float GetPitchY() => gridCellsLayer.cellSize.y + gridCellsLayer.spacing.y;

        private void BindMissingReferences()
        {
            if (gridCellsLayer == null)
            {
                var gridCells = transform.Find("GridContent/GridCellsLayer");
                if (gridCells != null)
                {
                    gridCellsLayer = gridCells.GetComponent<GridLayoutGroup>();
                }
            }

            if (itemsLayer == null)
            {
                var items = transform.Find("GridContent/ItemsLayer");
                if (items != null)
                {
                    itemsLayer = items as RectTransform;
                }
            }

            if (viewport == null)
            {
                viewport = transform as RectTransform;
            }
        }
    }
}
