using UnityEngine;
using TheLastTowerDefence.Inventory.Systems;

namespace TheLastTowerDefence.Enemies.Systems
{
    /// <summary>
    /// Хранит собранный предмет внутри сундука до момента выдачи в инвентарь.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyLootChestContainer : MonoBehaviour
    {
        [SerializeField] InventoryItemView storedItem;

        public InventoryItemView StoredItem => storedItem;
        public bool HasStoredItem => storedItem != null;

        public void SetStoredItem(InventoryItemView item)
        {
            storedItem = item;
            if (storedItem == null)
            {
                return;
            }

            var itemTransform = storedItem.transform;
            itemTransform.SetParent(transform, false);
            itemTransform.localPosition = Vector3.zero;
            storedItem.gameObject.SetActive(false);
        }

        public bool TryTakeStoredItem(out InventoryItemView item)
        {
            item = storedItem;
            if (item == null)
            {
                return false;
            }

            storedItem = null;
            item.transform.SetParent(null, true);
            item.gameObject.SetActive(true);
            return true;
        }
    }
}
