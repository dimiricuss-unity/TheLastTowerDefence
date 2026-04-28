using UnityEngine;
using UnityEngine.EventSystems;
using TheLastTowerDefence.UI;

namespace TheLastTowerDefence.Inventory.Systems
{
    /// <summary>
    /// ПКМ по предмету в инвентаре — надеть в подходящий слот активного окна персонажа (с заменой в инвентарь).
    /// ПКМ по предмету в слоте — снять в инвентарь. Требуется <see cref="Graphic"/> с raycast на корне предмета.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InventoryItemView))]
    public sealed class InventoryItemRightClickHandler : MonoBehaviour, IPointerClickHandler
    {
        private InventoryItemView _itemView;
        private InventoryItemPopupController _popupController;

        private void Awake()
        {
            _itemView = GetComponent<InventoryItemView>();
            _popupController = FindFirstObjectByType<InventoryItemPopupController>(FindObjectsInactive.Include);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_itemView == null || _itemView.Config == null)
            {
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                TryOpenInfoPopup();
                return;
            }

            if (eventData.button != PointerEventData.InputButton.Right)
            {
                return;
            }

            var slot = GetComponentInParent<EquipmentSlotCellView>();
            if (slot != null && slot.EquippedItem == _itemView)
            {
                slot.TryUnequipToInventory();
                return;
            }

            var grid = FindFirstObjectByType<InventoryGridView>();
            if (grid == null || grid.ItemsLayer == null || transform.parent != grid.ItemsLayer)
            {
                return;
            }

            var windows = FindFirstObjectByType<CharacterWindowsController>();
            if (windows == null)
            {
                return;
            }

            var activeRoot = windows.GetActiveCharacterWindow();
            if (activeRoot == null || !activeRoot.activeInHierarchy)
            {
                return;
            }

            var slots = activeRoot.GetComponentsInChildren<EquipmentSlotCellView>(true);

            // Сначала только свободные подходящие слоты — иначе первый по иерархии Ring
            // забирает любое новое кольцо и вытесняет уже надетое.
            for (var i = 0; i < slots.Length; i++)
            {
                var s = slots[i];
                if (s == null || !s.CanEquip(_itemView.Config) || s.EquippedItem != null)
                {
                    continue;
                }

                if (s.TryEquipItem(_itemView))
                {
                    return;
                }
            }

            for (var i = 0; i < slots.Length; i++)
            {
                var s = slots[i];
                if (s != null && s.CanEquip(_itemView.Config) && s.TryEquipItem(_itemView))
                {
                    return;
                }
            }
        }

        private void TryOpenInfoPopup()
        {
            var isInGrid = IsItemInInventoryGrid();
            var slot = GetComponentInParent<EquipmentSlotCellView>();
            var isInEquippedSlot = slot != null && slot.EquippedItem == _itemView;
            if (!isInGrid && !isInEquippedSlot)
            {
                return;
            }

            if (_popupController == null)
            {
                _popupController = FindFirstObjectByType<InventoryItemPopupController>(FindObjectsInactive.Include);
            }

            if (_popupController != null)
            {
                _popupController.ShowForItem(_itemView);
            }
        }

        private bool IsItemInInventoryGrid()
        {
            var grid = FindFirstObjectByType<InventoryGridView>();
            return grid != null && grid.ItemsLayer != null && transform.parent == grid.ItemsLayer;
        }
    }
}
