using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TheLastTowerDefence.Enemies.Spawning;

namespace TheLastTowerDefence.UI
{
    /// <summary>
    /// UI SpawnButton: по одному клику — один враг из активных <see cref="EnemySpawnPoint"/>,
    /// перечисленных в порядке детей объекта <see cref="spawnPointsRoot"/> (в сцене обычно «SpawnPoints»).
    /// Точки обходятся по кругу: 1-й клик — первая, 2-й — вторая, после последней снова первая.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpawnEnemiesButton : MonoBehaviour
    {
        [Tooltip("Родитель спавн-точек (дочерние в порядке иерархии). На каждом дочернем объекте должен быть EnemySpawnPoint.")]
        [SerializeField] Transform spawnPointsRoot;

        [Tooltip("Если spawnPointsRoot не задан в инспекторе, ищется активный объект с этим именем.")]
        [SerializeField] string spawnPointsRootName = "SpawnPoints";

        Button _button;
        int _nextSpawnIndex;

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
            var root = ResolveSpawnPointsRoot();
            if (root == null)
            {
                Debug.LogWarning(
                    $"[{nameof(SpawnEnemiesButton)}] Не задан родитель спавн-точек и не найден объект '{spawnPointsRootName}'.",
                    this);
                return;
            }

            var points = CollectActiveSpawnPointsInChildOrder(root);
            if (points.Count == 0)
            {
                Debug.LogWarning(
                    $"[{nameof(SpawnEnemiesButton)}] Под '{root.name}' нет активных {nameof(EnemySpawnPoint)}.",
                    this);
                return;
            }

            var idx = _nextSpawnIndex % points.Count;
            points[idx].Spawn();
            _nextSpawnIndex = (_nextSpawnIndex + 1) % points.Count;
        }

        Transform ResolveSpawnPointsRoot()
        {
            if (spawnPointsRoot != null)
                return spawnPointsRoot;

            if (string.IsNullOrEmpty(spawnPointsRootName))
                return null;

            var go = GameObject.Find(spawnPointsRootName);
            return go != null ? go.transform : null;
        }

        /// <summary>
        /// Прямые дочерние <paramref name="root"/> в порядке sibling index; только активные в иерархии,
        /// с активным <see cref="EnemySpawnPoint"/> на том же GameObject или среди детей (первый найденный).
        /// </summary>
        static List<EnemySpawnPoint> CollectActiveSpawnPointsInChildOrder(Transform root)
        {
            var list = new List<EnemySpawnPoint>(root.childCount);
            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (!child.gameObject.activeInHierarchy)
                    continue;

                var sp = child.GetComponent<EnemySpawnPoint>();
                if (sp == null)
                    sp = child.GetComponentInChildren<EnemySpawnPoint>(false);
                if (sp == null || !sp.enabled || !sp.gameObject.activeInHierarchy)
                    continue;

                list.Add(sp);
            }

            return list;
        }
    }
}
