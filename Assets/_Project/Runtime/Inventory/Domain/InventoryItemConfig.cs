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

        [Header("Weapon / combat base")]
        [Tooltip("Минимальный урон базы оружия (логика применения — позже).")]
        public float weaponBaseMinDamage;

        [Tooltip("Максимальный урон базы оружия.")]
        public float weaponBaseMaxDamage;

        [Tooltip("Базовая скорость атаки (APS0).")]
        public float weaponBaseAttacksPerSecond;

        [Tooltip("Модификатор крита базы оружия (как weaponCritModifier в формулах).")]
        public float weaponBaseCritModifier;

        [Tooltip("Рейтинг брони, который даёт предмет (логика стэка — позже).")]
        public int weaponArmorRating;

        [Header("Attribute bonuses")]
        public int bonusStrength;
        public int bonusDexterity;
        public int bonusStamina;
        public int bonusIntelligence;
        public int bonusWillpower;
        public int bonusLuck;

        [Header("Combat parameter bonuses")]
        [Tooltip("Бонус к максимуму HP.")]
        public float bonusMaxHp;

        [Tooltip("Бонус к максимуму маны.")]
        public float bonusMaxMana;

        [Tooltip("Бонус к восстановлению маны в секунду.")]
        public float bonusManaRegenPerSecond;

        [Tooltip("Бонус к критическому урону (интерпретация при применении — позже).")]
        public float bonusCriticalDamage;

        [Header("Economy")]
        [Min(0)]
        [Tooltip("Цена предмета.")]
        public int price;

        [Header("Grid Size (shared inventory)")]
        [Min(1)] public int sizeInCellsX = 1;
        [Min(1)] public int sizeInCellsY = 1;
    }
}
