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

    [CreateAssetMenu(fileName = "EnemyStats", menuName = "TLTD/Enemies/Enemy Stats Config")]
    public sealed class EnemyStatsConfig : ScriptableObject
    {
        [Min(1f)] public float maxHealth = 50f;
        [Min(0f)] public float damage = 5f;
        [Min(0.01f)] public float attacksPerSecond = 1f;

        [Tooltip("Опыт за убийство этого врага (сумма делится на трёх героев, вниз до целого).")]
        [Min(0)] public int experience = 0;

        [Tooltip("Ближний враг: контактный урон по героям с тегом RangeHero не наносится. Снимите для дальнобойного врага.")]
        public bool isMeleeAttacker = true;

        [Header("Loot")]
        [Range(0f, 100f)]
        [Tooltip("Общий шанс выпадения лута с этого врага в процентах.")]
        public float totalLootDropChancePercent;

        [Header("Loot Sections")]
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
