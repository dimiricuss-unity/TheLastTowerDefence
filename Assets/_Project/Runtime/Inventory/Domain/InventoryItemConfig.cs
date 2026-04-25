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

        [Header("Text")]
        [Tooltip("Отображаемое название предмета (в т.ч. оружия).")]
        public string displayName;

        [Tooltip("Описание предмета для UI.")]
        [TextArea(4, 14)]
        public string description;

        [Header("Equip")]
        public EquipmentSlotType slotType = EquipmentSlotType.Weapon;
        public EquipableCharacterType equipableCharacter = EquipableCharacterType.All;
        
        [Header("Stats")]
        [Tooltip("Ссылка на конфиг боевых параметров и экономики предмета.")]
        public InventoryItemStatsConfig statsConfig;

        [Header("Grid Size (shared inventory)")]
        [Min(1)] public int sizeInCellsX = 1;
        [Min(1)] public int sizeInCellsY = 1;
    }
}
