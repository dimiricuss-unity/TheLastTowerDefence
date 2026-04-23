using UnityEngine;
using UnityEngine.UI;
using TheLastTowerDefence.Inventory.Domain;

namespace TheLastTowerDefence.Inventory.Systems
{
    [DisallowMultipleComponent]
    public sealed class InventoryItemView : MonoBehaviour
    {
        [SerializeField] private InventoryItemConfig config;
        [Header("Grid Metrics")]
        [SerializeField] private bool useGridMetricsFromScene = true;
        [Min(1f)] [SerializeField] private float cellSize = 40f;
        [Min(0f)] [SerializeField] private float cellSpacing = 2f;

        [Header("Optional Overrides")]
        [SerializeField] private RectTransform targetRect;
        [SerializeField] private Image iconImage;
        [SerializeField] private LayoutElement layoutElement;
        [SerializeField] private bool isEquippedInSlot;

        public InventoryItemConfig Config => config;
        public int SizeInCellsX => config != null ? Mathf.Max(1, config.sizeInCellsX) : 1;
        public int SizeInCellsY => config != null ? Mathf.Max(1, config.sizeInCellsY) : 1;

        private void Reset()
        {
            BindMissingReferences();
            ApplyConfigToVisuals();
        }

        private void Awake()
        {
            BindMissingReferences();
            ApplyConfigToVisuals();
        }

        private void OnEnable()
        {
            BindMissingReferences();
            ApplyConfigToVisuals();
        }

        private void OnValidate()
        {
            BindMissingReferences();
        }

        [ContextMenu("Apply Config To Visuals")]
        private void ApplyConfigFromContextMenu()
        {
            BindMissingReferences();
            ApplyConfigToVisuals();
        }

        public void ReapplyConfigToVisuals()
        {
            BindMissingReferences();
            ApplyConfigToVisuals();
        }

        public void SetEquippedInSlot(bool equipped)
        {
            isEquippedInSlot = equipped;
        }

        private void BindMissingReferences()
        {
            if (targetRect == null)
            {
                targetRect = transform as RectTransform;
            }

            if (layoutElement == null)
            {
                layoutElement = GetComponent<LayoutElement>();
            }

            if (iconImage == null)
            {
                var icon = transform.Find("Icon");
                if (icon != null)
                {
                    iconImage = icon.GetComponent<Image>();
                }
            }
        }

        private void ApplyConfigToVisuals()
        {
            if (config == null)
            {
                return;
            }

            ResolveGridMetricsFromScene();

            var sizeX = Mathf.Max(1, config.sizeInCellsX);
            var sizeY = Mathf.Max(1, config.sizeInCellsY);

            var width = ComputeSizeInPixels(sizeX);
            var height = ComputeSizeInPixels(sizeY);

            if (!isEquippedInSlot && targetRect != null)
            {
                targetRect.sizeDelta = new Vector2(width, height);
            }

            if (layoutElement != null)
            {
                layoutElement.ignoreLayout = isEquippedInSlot;
                layoutElement.minWidth = isEquippedInSlot ? -1f : width;
                layoutElement.minHeight = isEquippedInSlot ? -1f : height;
                layoutElement.preferredWidth = isEquippedInSlot ? -1f : width;
                layoutElement.preferredHeight = isEquippedInSlot ? -1f : height;
                layoutElement.flexibleWidth = 0f;
                layoutElement.flexibleHeight = 0f;
            }

            if (iconImage != null)
            {
                iconImage.sprite = config.itemIcon;
                iconImage.color = config.itemIcon != null
                    ? new Color(1f, 1f, 1f, 1f)
                    : new Color(1f, 1f, 1f, 0f);
                iconImage.preserveAspect = true;
            }
        }

        private float ComputeSizeInPixels(int cells)
        {
            return cells * cellSize + Mathf.Max(0, cells - 1) * cellSpacing;
        }

        private void ResolveGridMetricsFromScene()
        {
            if (!useGridMetricsFromScene)
            {
                return;
            }

            var grid = FindGridLayoutGroupForItem();
            if (grid == null)
            {
                return;
            }

            cellSize = Mathf.Max(1f, grid.cellSize.x);
            cellSpacing = Mathf.Max(0f, grid.spacing.x);
        }

        private GridLayoutGroup FindGridLayoutGroupForItem()
        {
            // Expected hierarchy:
            // GridContent
            //   - GridCellsLayer (GridLayoutGroup)
            //   - ItemsLayer (this item is here)
            if (transform.parent == null)
            {
                return null;
            }

            var gridContent = transform.parent.parent;
            if (gridContent == null)
            {
                return null;
            }

            var gridCellsLayer = gridContent.Find("GridCellsLayer");
            if (gridCellsLayer == null)
            {
                return null;
            }

            return gridCellsLayer.GetComponent<GridLayoutGroup>();
        }

    }
}
