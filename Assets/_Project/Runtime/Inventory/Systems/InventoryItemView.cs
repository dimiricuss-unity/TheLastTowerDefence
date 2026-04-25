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
        [SerializeField] private bool hasRuntimeIconTint;
        [SerializeField] private Color runtimeIconTint = Color.white;

        public InventoryItemConfig Config => config;
        public int SizeInCellsX => config != null ? Mathf.Max(1, config.sizeInCellsX) : 1;
        public int SizeInCellsY => config != null ? Mathf.Max(1, config.sizeInCellsY) : 1;
        public bool IsEquippedInSlot => isEquippedInSlot;

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

        public void SetRuntimeConfig(InventoryItemConfig runtimeConfig)
        {
            config = runtimeConfig;
            isEquippedInSlot = false;
            BindMissingReferences();
            ApplyConfigToVisuals();
        }

        public void SetRuntimeIconTint(Color tint)
        {
            hasRuntimeIconTint = true;
            runtimeIconTint = tint;
            BindMissingReferences();
            ApplyConfigToVisuals();
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
                if (config.itemIcon == null)
                {
                    iconImage.color = new Color(1f, 1f, 1f, 0f);
                }
                else if (hasRuntimeIconTint)
                {
                    runtimeIconTint.a = 1f;
                    iconImage.color = runtimeIconTint;
                }
                else
                {
                    iconImage.color = new Color(1f, 1f, 1f, 1f);
                }
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
