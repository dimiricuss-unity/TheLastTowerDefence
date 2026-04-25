using UnityEngine;
using UnityEngine.UI;
using TheLastTowerDefence.Enemies.Systems;
using TheLastTowerDefence.Inventory.Systems;

namespace TheLastTowerDefence.UI
{
    /// <summary>
    /// Собирает предметы из всех сундуков на сцене и переносит их в инвентарь.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class CollectLootButton : MonoBehaviour
    {
        [SerializeField] InventoryGridView inventoryGrid;

        Button _button;

        void Awake()
        {
            _button = GetComponent<Button>();
            if (_button != null)
            {
                _button.onClick.AddListener(CollectAllLootToInventory);
            }
        }

        void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(CollectAllLootToInventory);
            }
        }

        public void CollectAllLootToInventory()
        {
            if (!ResolveInventoryGrid())
            {
                Debug.LogWarning($"[{nameof(CollectLootButton)}] Не найден {nameof(InventoryGridView)}.", this);
                return;
            }

            var chests = FindObjectsByType<EnemyLootChestContainer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (var i = 0; i < chests.Length; i++)
            {
                var chest = chests[i];
                if (chest == null || !chest.TryTakeStoredItem(out var item) || item == null)
                {
                    continue;
                }

                if (!inventoryGrid.TryPlaceItemAtFirstFreeCell(item))
                {
                    chest.SetStoredItem(item);
                    continue;
                }

                Destroy(chest.gameObject);
            }

            CollectLooseWorldItems();
        }

        bool ResolveInventoryGrid()
        {
            if (inventoryGrid != null)
            {
                return true;
            }

            inventoryGrid = FindFirstObjectByType<InventoryGridView>(FindObjectsInactive.Include);
            return inventoryGrid != null;
        }

        void CollectLooseWorldItems()
        {
            var items = FindObjectsByType<InventoryItemView>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            var itemsLayer = inventoryGrid.ItemsLayer;
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item == null || item.Config == null)
                {
                    continue;
                }

                if (item.IsEquippedInSlot)
                {
                    continue;
                }

                var itemTransform = item.transform;
                if (itemsLayer != null && itemTransform.IsChildOf(itemsLayer))
                {
                    continue;
                }

                if (itemTransform.GetComponentInParent<EquipmentSlotCellView>() != null)
                {
                    continue;
                }

                if (itemTransform.GetComponentInParent<EnemyLootChestContainer>() != null)
                {
                    continue;
                }

                inventoryGrid.TryPlaceItemAtFirstFreeCell(item);
            }
        }
    }
}
