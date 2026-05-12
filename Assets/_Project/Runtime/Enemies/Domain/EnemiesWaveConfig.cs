using System.Collections.Generic;
using UnityEngine;

namespace TheLastTowerDefence.Enemies.Domain
{
    /// <summary>
    /// Одна группа внутри волны: сколько врагов и пауза между появлениями внутри группы.
    /// </summary>
    [System.Serializable]
    public sealed class EnemiesGroup
    {
        [Min(0)]
        [Tooltip("Количество врагов в этой группе.")]
        public int NumberOfEnemies;

        [Min(0f)]
        [Tooltip("Интервал между появлением врагов внутри группы (секунды).")]
        public float SpawnInterval;
    }

    /// <summary>
    /// Конфигурация одной волны: группы врагов с паузой <see cref="GroupInterval"/> между группами,
    /// плюс списки префабов и SO статов/лута.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemiesWave", menuName = "TLTD/Enemies/Enemies Wave Config")]
    public sealed class EnemiesWaveConfig : ScriptableObject
    {
        [Min(0f)]
        [Tooltip("Интервал между группами врагов (секунды): после завершения одной группы до начала следующей).")]
        public float GroupInterval;

        [Tooltip("Группы врагов по порядку волны. Внутри группы враги появляются с интервалом SpawnInterval.")]
        public List<EnemiesGroup> EnemiesGroups = new List<EnemiesGroup>();

        [Tooltip("Префабы врагов (Enemy).")]
        public List<GameObject> EnemyPrefabs = new List<GameObject>();

        [Tooltip("Конфиги статов врагов (EnemyStatsConfig).")]
        public List<EnemyStatsConfig> EnemyConfigs = new List<EnemyStatsConfig>();

        [Tooltip("Конфиги дропа (LootDropConfig).")]
        public List<LootDropConfig> EnemyDrops = new List<LootDropConfig>();
    }
}
