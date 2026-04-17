using UnityEngine;
using UnityEngine.UI;
using TheLastTowerDefence.Enemies.Spawning;

namespace TheLastTowerDefence.UI
{
    /// <summary>
    /// UI-кнопка: по клику вызывает <see cref="EnemySpawnPoint.Spawn"/> на всех точках спавна в сцене
    /// (аналогично тому, что делает <c>spawnOnStart</c> при старте).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpawnEnemiesButton : MonoBehaviour
    {
        [Tooltip("Если задано — только эти точки; иначе все EnemySpawnPoint на сцене.")]
        [SerializeField] EnemySpawnPoint[] spawnPoints;

        Button _button;

        void Awake()
        {
            _button = GetComponent<Button>();
            if (_button != null)
                _button.onClick.AddListener(SpawnEnemies);
        }

        void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(SpawnEnemies);
        }

        /// <summary>Можно повесить и из Inspector OnClick без дублирования с Awake — тогда уберите компонент с кнопки и вызывайте с другого объекта.</summary>
        public void SpawnEnemies()
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                foreach (var p in spawnPoints)
                {
                    if (p != null)
                        p.Spawn();
                }

                return;
            }

            var all = FindObjectsByType<EnemySpawnPoint>(FindObjectsSortMode.None);
            if (all.Length == 0)
            {
                Debug.LogWarning($"[{nameof(SpawnEnemiesButton)}] На сцене нет {nameof(EnemySpawnPoint)}.", this);
                return;
            }

            foreach (var p in all)
                p.Spawn();
        }
    }
}
