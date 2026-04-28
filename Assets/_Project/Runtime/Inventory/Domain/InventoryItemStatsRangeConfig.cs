using UnityEngine;

namespace TheLastTowerDefence.Inventory.Domain
{
    [CreateAssetMenu(
        fileName = "InventoryItemStatsRangeConfig",
        menuName = "TheLastTowerDefence/Inventory/Item Stats Range Config")]
    public sealed class InventoryItemStatsRangeConfig : ScriptableObject
    {
        [Header("Equip")]
        public EquipmentSlotType slotType = EquipmentSlotType.Weapon;
        public EquipableCharacterType equipableCharacter = EquipableCharacterType.All;
        public InventoryItemRarity rarity = InventoryItemRarity.Base;

        [Header("Visual")]
        [Tooltip("Цвет иконки предмета после генерации.")]
        public Color iconColor = Color.white;

        [Header("Weapon / combat base ranges")]
        [Tooltip("Минимально возможный минимальный урон базы оружия.")]
        public float weaponBaseMinDamageMin;
        [Tooltip("Максимально возможный минимальный урон базы оружия.")]
        public float weaponBaseMinDamageMax;

        [Tooltip("Минимально возможный максимальный урон базы оружия.")]
        public float weaponBaseMaxDamageMin;
        [Tooltip("Максимально возможный максимальный урон базы оружия.")]
        public float weaponBaseMaxDamageMax;

        [Tooltip("Минимально возможная базовая скорость атаки (APS0).")]
        public float weaponBaseAttacksPerSecondMin;
        [Tooltip("Максимально возможная базовая скорость атаки (APS0).")]
        public float weaponBaseAttacksPerSecondMax;

        [Tooltip("Минимально возможный модификатор крита базы оружия.")]
        public float weaponBaseCritModifierMin;
        [Tooltip("Максимально возможный модификатор крита базы оружия.")]
        public float weaponBaseCritModifierMax;

        [Tooltip("Минимально возможный рейтинг брони от предмета.")]
        public int weaponArmorRatingMin;
        [Tooltip("Максимально возможный рейтинг брони от предмета.")]
        public int weaponArmorRatingMax;

        [Header("Attribute bonus ranges")]
        public int bonusStrengthMin;
        public int bonusStrengthMax;

        public int bonusDexterityMin;
        public int bonusDexterityMax;

        public int bonusStaminaMin;
        public int bonusStaminaMax;

        public int bonusIntelligenceMin;
        public int bonusIntelligenceMax;

        public int bonusWillpowerMin;
        public int bonusWillpowerMax;

        public int bonusLuckMin;
        public int bonusLuckMax;

        [Header("Combat parameter bonus ranges")]
        [Tooltip("Диапазон бонуса к максимуму HP.")]
        public float bonusMaxHpMin;
        public float bonusMaxHpMax;

        [Tooltip("Диапазон бонуса к восстановлению HP в секунду.")]
        public float bonusHpRegenPerSecondMin;
        public float bonusHpRegenPerSecondMax;

        [Tooltip("Диапазон бонуса к максимуму маны.")]
        public float bonusMaxManaMin;
        public float bonusMaxManaMax;

        [Tooltip("Диапазон бонуса к восстановлению маны в секунду.")]
        public float bonusManaRegenPerSecondMin;
        public float bonusManaRegenPerSecondMax;

        [Tooltip("Диапазон бонуса к критическому урону.")]
        public float bonusCriticalDamageMin;
        public float bonusCriticalDamageMax;

        [Header("Economy ranges")]
        [Min(0)]
        [Tooltip("Минимально возможная цена предмета.")]
        public int priceMin;

        [Min(0)]
        [Tooltip("Максимально возможная цена предмета.")]
        public int priceMax;

    }
}
