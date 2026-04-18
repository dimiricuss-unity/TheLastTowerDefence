using UnityEngine;

namespace TheLastTowerDefence.Heroes.Domain
{
    [CreateAssetMenu(fileName = "HeroStats", menuName = "TLTD/Heroes/Hero Stats Config")]
    public sealed class HeroStatsConfig : ScriptableObject
    {
        [Min(1f)] public float maxHealth = 100f;
        [Min(0f)] public float damage = 10f;
        [Min(0.01f)] public float attacksPerSecond = 1f;

        [Tooltip("Мана героя (максимум или базовый пул — по логике геймплея).")]
        [Min(0f)] public float mana = 100f;

        [Tooltip("Восстановление маны в секунду реального времени, пока текущая мана ниже максимума.")]
        [Min(0f)] public float manaRegenPerSecond = 2f;
    }
}
