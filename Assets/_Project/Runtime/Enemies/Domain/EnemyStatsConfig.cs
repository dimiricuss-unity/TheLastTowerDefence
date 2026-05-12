using UnityEngine;

namespace TheLastTowerDefence.Enemies.Domain
{
    [CreateAssetMenu(fileName = "EnemyStats", menuName = "TLTD/Enemies/Enemy Stats Config")]
    public sealed class EnemyStatsConfig : ScriptableObject
    {
        [Min(0)]
        [Tooltip("Уровень врага (для дизайна и подбора контента).")]
        public int level;

        [Min(1f)] public float maxHealth = 50f;
        [Min(0f)] public float damage = 5f;
        [Min(0.01f)] public float attacksPerSecond = 1f;

        [Tooltip("Опыт за убийство этого врага (сумма делится на трёх героев, вниз до целого).")]
        [Min(0)] public int experience = 0;

        [Tooltip("Ближний враг: контактный урон по героям с тегом RangeHero не наносится. Снимите для дальнобойного врага.")]
        public bool isMeleeAttacker = true;

        [Header("Loot")]
        [Range(0f, 100f)]
        [Tooltip("Общий шанс выпадения лута с этого врага в процентах (секции — в LootDropConfig).")]
        public float totalLootDropChancePercent;
    }
}
