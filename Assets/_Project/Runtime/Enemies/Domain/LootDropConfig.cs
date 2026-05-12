using UnityEngine;

namespace TheLastTowerDefence.Enemies.Domain
{
    public enum LootRarity
    {
        Base = 0,
        Rare = 1,
        Magic = 2,
        Legendary = 3,
        Epic = 4,
        Relict = 5,
    }

    [System.Serializable]
    public sealed class LootDropSection
    {
        [Tooltip("Тип лута для этой секции.")]
        public LootRarity rarity = LootRarity.Base;

        [Range(0f, 100f)]
        [Tooltip("Шанс выпадения лута этого типа в процентах.")]
        public float lootDropChancePercent;

        [Tooltip("Если выключено, секция не участвует в розыгрыше.")]
        public bool isEnabledInRoll = true;
    }

    [CreateAssetMenu(fileName = "LootDrop", menuName = "TLTD/Enemies/Loot Drop Config")]
    public sealed class LootDropConfig : ScriptableObject
    {
        [Min(0)]
        [Tooltip("Уровень / ранг таблицы лута (для дизайна и подбора контента).")]
        public int level;

        [Tooltip("Секции розыгрыша лута. Каждая секция задаёт редкость, шанс и участие в розыгрыше.")]
        public LootDropSection[] lootDropSections =
        {
            new() { rarity = LootRarity.Base, isEnabledInRoll = true },
            new() { rarity = LootRarity.Rare, isEnabledInRoll = true },
            new() { rarity = LootRarity.Magic, isEnabledInRoll = true },
            new() { rarity = LootRarity.Legendary, isEnabledInRoll = true },
            new() { rarity = LootRarity.Epic, isEnabledInRoll = true },
            new() { rarity = LootRarity.Relict, isEnabledInRoll = true },
        };
    }
}
