using UnityEngine;
using UnityEngine.UI;
using TheLastTowerDefence.Enemies.Systems;

namespace TheLastTowerDefence.UI
{
    /// <summary>
    /// UI SpawnButton: запускает следующую волну через <see cref="EnemyWaves"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpawnEnemiesButton : MonoBehaviour
    {
        [Tooltip("Если не задан, ищется первый EnemyWaves на сцене.")]
        [SerializeField] EnemyWaves enemyWaves;

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

        /// <summary>Обработчик кнопки (и для Inspector OnClick).</summary>
        public void SpawnEnemies()
        {
            var waves = enemyWaves != null ? enemyWaves : FindFirstObjectByType<EnemyWaves>();
            if (waves == null)
            {
                Debug.LogWarning(
                    $"[{nameof(SpawnEnemiesButton)}] На сцене нет {nameof(EnemyWaves)} — назначь ссылку или добавь объект EnemyWavesCatalog.",
                    this);
                return;
            }

            waves.StartNextWave();
        }
    }
}
