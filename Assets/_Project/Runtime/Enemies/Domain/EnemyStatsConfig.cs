using UnityEngine;

namespace TheLastTowerDefence.Enemies.Domain
{
    [CreateAssetMenu(fileName = "EnemyStats", menuName = "TLTD/Enemies/Enemy Stats Config")]
    public sealed class EnemyStatsConfig : ScriptableObject
    {
        [Min(1f)] public float maxHealth = 50f;
        [Min(0f)] public float damage = 5f;
        [Min(0.01f)] public float attacksPerSecond = 1f;
    }
}
