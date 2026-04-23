using UnityEngine;
using UnityEngine.UI;
using TheLastTowerDefence.Inventory.Domain;

namespace TheLastTowerDefence.Inventory.Systems
{
    [DisallowMultipleComponent]
    public sealed class EquipmentSlotCellView : MonoBehaviour
    {
        [SerializeField] private EquipmentSlotType slotType = EquipmentSlotType.Weapon;
        [SerializeField] private PlayableCharacterClass ownerClass = PlayableCharacterClass.Warrior;
        [SerializeField] private RectTransform itemAnchor;
        [SerializeField] private InventoryItemView equippedItem;

        [Header("Drag equip feedback")]
        [Tooltip("Оверлей поверх слота (raycast off). Если пусто — ищется дочерний объект Selection.")]
        [SerializeField] private Image dragHighlightImage;
        [SerializeField] private Color validDragSlotColor = new Color(0.25f, 0.82f, 0.38f, 0.55f);
        [SerializeField] private Color invalidDragSlotColor = new Color(0.92f, 0.32f, 0.28f, 0.55f);

        public EquipmentSlotType SlotType => slotType;
        public PlayableCharacterClass OwnerClass => ownerClass;
        public InventoryItemView EquippedItem => equippedItem;

        public bool CanEquip(InventoryItemConfig itemConfig)
        {
            if (itemConfig == null)
            {
                return false;
            }

            if (itemConfig.slotType != slotType)
            {
                return false;
            }

            if (itemConfig.equipableCharacter == EquipableCharacterType.All)
            {
                return true;
            }

            return itemConfig.equipableCharacter == ToEquipableCharacter(ownerClass);
        }

        public bool TryEquipItem(InventoryItemView itemView)
        {
            if (itemView == null || !CanEquip(itemView.Config))
            {
                return false;
            }

            if (equippedItem == itemView)
            {
                return true;
            }

            if (equippedItem != null && equippedItem != itemView)
            {
                var displaced = equippedItem;
                equippedItem = null;
                displaced.SetEquippedInSlot(false);

                var stashGrid = FindFirstObjectByType<InventoryGridView>();
                if (stashGrid == null || !stashGrid.TryPlaceItemAtFirstFreeCell(displaced))
                {
                    equippedItem = displaced;
                    displaced.SetEquippedInSlot(true);
                    displaced.ReapplyConfigToVisuals();
                    return false;
                }
            }

            BindMissingReferences();

            var targetParent = itemAnchor != null ? itemAnchor : transform as RectTransform;
            var itemTransform = itemView.transform as RectTransform;
            if (targetParent == null || itemTransform == null)
            {
                return false;
            }

            itemTransform.SetParent(targetParent, false);
            itemTransform.anchorMin = Vector2.zero;
            itemTransform.anchorMax = Vector2.one;
            itemTransform.pivot = new Vector2(0.5f, 0.5f);
            itemTransform.offsetMin = new Vector2(6f, 6f);
            itemTransform.offsetMax = new Vector2(-6f, -6f);
            itemTransform.localScale = Vector3.one;
            itemView.SetEquippedInSlot(true);
            itemView.ReapplyConfigToVisuals();

            equippedItem = itemView;
            return true;
        }

        public void UnequipIfCurrent(InventoryItemView itemView)
        {
            if (equippedItem == itemView)
            {
                equippedItem = null;
                itemView.SetEquippedInSlot(false);
            }
        }

        /// <summary>
        /// Снимает предмет со слота и кладёт в инвентарь (первая свободная ячейка). При полном инвентаре откат.
        /// </summary>
        public bool TryUnequipToInventory()
        {
            if (equippedItem == null)
            {
                return false;
            }

            var item = equippedItem;
            UnequipIfCurrent(item);

            var stashGrid = FindFirstObjectByType<InventoryGridView>();
            if (stashGrid == null || !stashGrid.TryPlaceItemAtFirstFreeCell(item))
            {
                TryEquipItem(item);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Подсветка при перетаскивании предмета над слотом. <paramref name="draggedConfig"/> == null — скрыть.
        /// </summary>
        public void SetDragEquipHighlight(InventoryItemConfig draggedConfig)
        {
            BindDragHighlightImage();
            if (dragHighlightImage == null)
            {
                return;
            }

            if (draggedConfig == null)
            {
                dragHighlightImage.gameObject.SetActive(false);
                return;
            }

            dragHighlightImage.color = CanEquip(draggedConfig) ? validDragSlotColor : invalidDragSlotColor;
            dragHighlightImage.gameObject.SetActive(true);
        }

        private void Awake()
        {
            BindMissingReferences();
        }

        private void OnValidate()
        {
            BindMissingReferences();
        }

        private void BindMissingReferences()
        {
            if (itemAnchor == null)
            {
                var asRect = transform as RectTransform;
                if (asRect != null)
                {
                    itemAnchor = asRect;
                }
            }

            BindDragHighlightImage();
        }

        private void BindDragHighlightImage()
        {
            if (dragHighlightImage != null)
            {
                return;
            }

            var selection = transform.Find("Selection");
            if (selection != null)
            {
                dragHighlightImage = selection.GetComponent<Image>();
            }
        }

        private static EquipableCharacterType ToEquipableCharacter(PlayableCharacterClass characterClass)
        {
            switch (characterClass)
            {
                case PlayableCharacterClass.Warrior:
                    return EquipableCharacterType.Warrior;
                case PlayableCharacterClass.Cleric:
                    return EquipableCharacterType.Cleric;
                case PlayableCharacterClass.Archer:
                    return EquipableCharacterType.Archer;
                default:
                    return EquipableCharacterType.All;
            }
        }
    }
}
