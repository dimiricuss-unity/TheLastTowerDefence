using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheLastTowerDefence.Enemies.Domain;
using TheLastTowerDefence.Enemies.Spawning;

namespace TheLastTowerDefence.Enemies.Systems
{
    /// <summary>
    /// Каталог волн: по кнопке запускает следующую <see cref="EnemiesWaveConfig"/>, спавн по группам и интервалам из SO.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyWaves : MonoBehaviour
    {
        [SerializeField] List<EnemiesWaveConfig> enemyWavesList = new List<EnemiesWaveConfig>();

        [Tooltip("Родитель с дочерними точками EnemySpawnPoint (случайная точка на каждого врага).")]
        [SerializeField] Transform spawnPoints;

        int _nextWaveIndex;
        Coroutine _waveRoutine;

        /// <summary>Старт следующей волны из списка (если индекс за пределами — снова последняя волна).</summary>
        public void StartNextWave()
        {
            if (enemyWavesList == null || enemyWavesList.Count == 0)
            {
                Debug.LogWarning($"[{nameof(EnemyWaves)}] Список волн пуст на '{name}'.", this);
                return;
            }

            if (_waveRoutine != null)
                return;

            var lastIndex = enemyWavesList.Count - 1;
            var index = Mathf.Min(_nextWaveIndex, lastIndex);
            var config = enemyWavesList[index];
            if (config == null)
            {
                Debug.LogWarning($"[{nameof(EnemyWaves)}] Волна с индексом {index} не назначена (null).", this);
                return;
            }

            if (_nextWaveIndex < lastIndex)
                _nextWaveIndex++;

            _waveRoutine = StartCoroutine(RunWaveRoutine(config));
        }

        IEnumerator RunWaveRoutine(EnemiesWaveConfig wave)
        {
            var points = CollectSpawnPoints();
            if (points.Count == 0)
            {
                Debug.LogWarning(
                    $"[{nameof(EnemyWaves)}] Нет активных {nameof(EnemySpawnPoint)} под '{spawnPoints?.name ?? "null"}'.",
                    this);
                _waveRoutine = null;
                yield break;
            }

            var groups = wave.EnemiesGroups;
            if (groups == null || groups.Count == 0)
            {
                _waveRoutine = null;
                yield break;
            }

            for (var g = 0; g < groups.Count; g++)
            {
                var group = groups[g];
                if (group == null)
                    continue;

                var count = Mathf.Max(0, group.NumberOfEnemies);
                for (var i = 0; i < count; i++)
                {
                    var point = points[Random.Range(0, points.Count)];
                    TrySpawnOneEnemy(wave, point);

                    if (i < count - 1 && group.SpawnInterval > 0f)
                        yield return new WaitForSeconds(group.SpawnInterval);
                }

                if (g < groups.Count - 1 && wave.GroupInterval > 0f)
                    yield return new WaitForSeconds(wave.GroupInterval);
            }

            _waveRoutine = null;
        }

        List<EnemySpawnPoint> CollectSpawnPoints()
        {
            var list = new List<EnemySpawnPoint>();
            if (spawnPoints == null)
                return list;

            var found = spawnPoints.GetComponentsInChildren<EnemySpawnPoint>(true);
            for (var i = 0; i < found.Length; i++)
            {
                var sp = found[i];
                if (sp != null && sp.isActiveAndEnabled && sp.gameObject.activeInHierarchy)
                    list.Add(sp);
            }

            return list;
        }

        static void TrySpawnOneEnemy(EnemiesWaveConfig wave, EnemySpawnPoint point)
        {
            var prefab = PickRandomPrefab(wave);
            if (prefab == null)
            {
                Debug.LogWarning($"[{nameof(EnemyWaves)}] Нет валидных префабов в EnemyPrefabs.", point);
                return;
            }

            var stats = PickRandomStats(wave);
            if (stats == null)
            {
                Debug.LogWarning($"[{nameof(EnemyWaves)}] Нет EnemyStatsConfig в волне.", point);
                return;
            }

            var loot = PickRandomLootMatchingLevel(wave, stats.level);
            point.SpawnWaveEnemy(prefab, stats, loot);
        }

        static GameObject PickRandomPrefab(EnemiesWaveConfig wave)
        {
            var list = wave.EnemyPrefabs;
            if (list == null || list.Count == 0)
                return null;

            var buffer = new List<GameObject>();
            for (var i = 0; i < list.Count; i++)
            {
                var p = list[i];
                if (p != null)
                    buffer.Add(p);
            }

            if (buffer.Count == 0)
                return null;
            return buffer[Random.Range(0, buffer.Count)];
        }

        static EnemyStatsConfig PickRandomStats(EnemiesWaveConfig wave)
        {
            var list = wave.EnemyConfigs;
            if (list == null || list.Count == 0)
                return null;

            var buffer = new List<EnemyStatsConfig>();
            for (var i = 0; i < list.Count; i++)
            {
                var c = list[i];
                if (c != null)
                    buffer.Add(c);
            }

            if (buffer.Count == 0)
                return null;
            return buffer[Random.Range(0, buffer.Count)];
        }

        static LootDropConfig PickRandomLootMatchingLevel(EnemiesWaveConfig wave, int statsLevel)
        {
            var list = wave.EnemyDrops;
            if (list == null || list.Count == 0)
                return null;

            var buffer = new List<LootDropConfig>();
            for (var i = 0; i < list.Count; i++)
            {
                var d = list[i];
                if (d != null && d.level == statsLevel)
                    buffer.Add(d);
            }

            if (buffer.Count == 0)
                return null;
            return buffer[Random.Range(0, buffer.Count)];
        }
    }
}
