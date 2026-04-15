using UnityEngine;

namespace TheLastTowerDefence.Enemies.Spawning
{
    /// <summary>
    /// Spawns an enemy prefab at this transform's position. Rotation is not inherited from the spawn point
    /// (2D layout objects often have non-zero Z rotation); facing is handled by <c>EnemyMovement</c>.
    /// </summary>
    public sealed class EnemySpawnPoint : MonoBehaviour
    {
        [SerializeField] GameObject enemyPrefab;
        [SerializeField] bool spawnOnStart = true;

        void Start()
        {
            if (spawnOnStart)
                Spawn();
        }

        /// <summary>
        /// Creates an enemy instance at this point's world position with identity rotation.
        /// </summary>
        public void Spawn()
        {
            if (enemyPrefab == null)
            {
                Debug.LogWarning($"[{nameof(EnemySpawnPoint)}] enemyPrefab is not assigned on '{name}'.", this);
                return;
            }

            Instantiate(enemyPrefab, transform.position, Quaternion.identity);
        }
    }
}
