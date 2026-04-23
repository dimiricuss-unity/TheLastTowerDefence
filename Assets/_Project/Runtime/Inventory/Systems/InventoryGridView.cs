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

        private void Awake()
        {
            BindMissingReferences();
        }

        private void OnValidate()
        {
            BindMissingReferences();
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
