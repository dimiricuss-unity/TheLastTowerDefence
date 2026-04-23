using UnityEngine;

namespace TheLastTowerDefence.Inventory.Domain
{
    [CreateAssetMenu(
        fileName = "InventoryItemConfig",
        menuName = "TheLastTowerDefence/Inventory/Item Config")]
    public sealed class InventoryItemConfig : ScriptableObject
    {
        [Header("Visual")]
        public Sprite itemIcon;

        [Header("Equip")]
        public EquipmentSlotType slotType = EquipmentSlotType.Weapon;
        public EquipableCharacterType equipableCharacter = EquipableCharacterType.All;

        [Header("Grid Size (shared inventory)")]
        [Min(1)] public int sizeInCellsX = 1;
        [Min(1)] public int sizeInCellsY = 1;
    }
}
