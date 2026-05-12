using TheLastTowerDefence.Enemies.Domain;
using TheLastTowerDefence.Enemies.Systems;
using UnityEngine;

namespace TheLastTowerDefence.Enemies.Spawning
{
    /// <summary>
    /// Точка спавна: позиция и опционально <see cref="initialChaseTarget"/> для первого подхода врага.
    /// Волны спавнят префаб через <see cref="SpawnWaveEnemy"/>; устаревший <see cref="enemyPrefab"/> + Start больше не используются для волн.
    /// </summary>
    public sealed class EnemySpawnPoint : MonoBehaviour
    {
        [SerializeField] GameObject enemyPrefab;
        [SerializeField] bool spawnOnStart;
        [Tooltip("Если задано, заспавненный враг сначала идёт к этой точке (world), затем — к башне / герою как обычно. Пусто — сразу к башне.")]
        [SerializeField] Transform initialChaseTarget;

        void Start()
        {
            if (spawnOnStart && enemyPrefab != null)
                Spawn();
        }

        /// <summary>
        /// Создаёт врага на позиции точки (legacy: только если задан <see cref="enemyPrefab"/>).
        /// </summary>
        public void Spawn()
        {
            if (enemyPrefab == null)
                return;

            var instance = Instantiate(enemyPrefab, transform.position, Quaternion.identity);
            ApplyInitialChaseTarget(instance);
        }

        /// <summary>
        /// Спавн из волны: префаб и SO задаются снаружи; контекст читает <see cref="EnemyStatsBootstrap"/> в Awake.
        /// </summary>
        /// <returns>Инстанс или null при ошибке.</returns>
        public GameObject SpawnWaveEnemy(GameObject prefab, EnemyStatsConfig stats, LootDropConfig loot)
        {
            if (prefab == null)
            {
                Debug.LogWarning($"[{nameof(EnemySpawnPoint)}] Prefab is null on '{name}'.", this);
                return null;
            }

            if (stats == null)
            {
                Debug.LogWarning($"[{nameof(EnemySpawnPoint)}] EnemyStatsConfig is null on '{name}'.", this);
                return null;
            }

            EnemyWaveSpawnContext.SetPending(stats, loot);
            GameObject instance;
            try
            {
                instance = Instantiate(prefab, transform.position, Quaternion.identity);
            }
            catch
            {
                EnemyWaveSpawnContext.ClearPending();
                throw;
            }

            if (EnemyWaveSpawnContext.HasPending)
            {
                EnemyWaveSpawnContext.ClearPending();
                Debug.LogError(
                    $"[{nameof(EnemySpawnPoint)}] На префабе нет {nameof(EnemyStatsBootstrap)} — pending-конфиг волны потерян ('{name}').",
                    this);
                Destroy(instance);
                return null;
            }

            ApplyInitialChaseTarget(instance);
            return instance;
        }

        void ApplyInitialChaseTarget(GameObject instance)
        {
            if (initialChaseTarget == null)
                return;

            var movement = instance.GetComponent<EnemyMovement>();
            if (movement == null)
                movement = instance.GetComponentInChildren<EnemyMovement>(true);
            if (movement != null)
                movement.SetInitialApproachTarget(initialChaseTarget);
            else
                Debug.LogWarning(
                    $"[{nameof(EnemySpawnPoint)}] Задан {nameof(initialChaseTarget)}, но на префабе нет {nameof(EnemyMovement)} ('{name}').",
                    this);
        }
    }
}
