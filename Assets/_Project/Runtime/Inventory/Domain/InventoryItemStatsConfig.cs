using UnityEngine;

namespace TheLastTowerDefence.Inventory.Domain
{
    [CreateAssetMenu(
        fileName = "InventoryItemStatsConfig",
        menuName = "TheLastTowerDefence/Inventory/Item Stats Config")]
    public sealed class InventoryItemStatsConfig : ScriptableObject
    {
        [Header("Equip")]
        public EquipmentSlotType slotType = EquipmentSlotType.Weapon;
        public EquipableCharacterType equipableCharacter = EquipableCharacterType.All;
        public InventoryItemRarity rarity = InventoryItemRarity.Base;

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

        [Tooltip("Бонус к восстановлению HP в секунду.")]
        public float bonusHpRegenPerSecond;

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

        [Header("Drop")]
        [Range(0f, 100f)]
        [Tooltip("Шанс выпадения предмета в процентах.")]
        public float dropChancePercent;
    }
}
