using UnityEngine;
using TheLastTowerDefence.Enemies.Domain;
using TheLastTowerDefence.Enemies.Spawning;

namespace TheLastTowerDefence.Enemies.Systems
{
    /// <summary>
    /// Ранняя инициализация: один раз передаёт данные из SO в остальные модули врага.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class EnemyStatsBootstrap : MonoBehaviour
    {
        [SerializeField] EnemyStatsConfig enemyStatsConfig;
        [SerializeField] LootDropConfig lootDropConfig;

        [Tooltip("Цель движения и атаки (корень башни с IDamageable). На ассете префаба сцену сюда не перетащить — оставь пустым: ищется объект с тегом towerTag.")]
        [SerializeField] Transform combatTarget;

        [Tooltip("Если combatTarget пуст, берётся GameObject.FindGameObjectWithTag (тег должен существовать в Project Settings → Tags).")]
        [SerializeField] string towerTag = "Tower";

        [SerializeField] EnemyHealth health;
        [SerializeField] EnemyMovement movement;
        [SerializeField] EnemyAttack attack;
        [SerializeField] EnemyHeroFocus heroFocus;
        [SerializeField] EnemyLootDropper lootDropper;

        void Reset()
        {
            health = GetComponent<EnemyHealth>();
            movement = GetComponent<EnemyMovement>();
            attack = GetComponent<EnemyAttack>();
            heroFocus = GetComponent<EnemyHeroFocus>();
            lootDropper = GetComponent<EnemyLootDropper>();
        }

        void Awake()
        {
            EnsureReferences();

            if (EnemyWaveSpawnContext.TryConsume(out var injectedStats, out var injectedLoot))
            {
                enemyStatsConfig = injectedStats;
                lootDropConfig = injectedLoot;
            }

            if (enemyStatsConfig == null)
            {
                Debug.LogError($"[{nameof(EnemyStatsBootstrap)}] Не назначен EnemyStatsConfig на '{name}'.", this);
                enabled = false;
                return;
            }

            if (combatTarget == null)
            {
                if (string.IsNullOrEmpty(towerTag))
                {
                    Debug.LogWarning($"[{nameof(EnemyStatsBootstrap)}] Combat Target пуст и towerTag не задан.", this);
                }
                else
                {
                    try
                    {
                        var towerGo = GameObject.FindGameObjectWithTag(towerTag);
                        if (towerGo != null)
                            combatTarget = towerGo.transform;
                        else
                            Debug.LogWarning($"[{nameof(EnemyStatsBootstrap)}] Нет объекта с тегом '{towerTag}' — назначь тег на башню или combatTarget вручную на инстансе.", this);
                    }
                    catch (UnityException)
                    {
                        Debug.LogError($"[{nameof(EnemyStatsBootstrap)}] Тег '{towerTag}' не объявлен в проекте (Tags & Layers).", this);
                    }
                }
            }

            health.Configure(enemyStatsConfig);
            movement.Configure(enemyStatsConfig, combatTarget);
            attack.Configure(enemyStatsConfig, combatTarget);
            if (heroFocus != null)
                heroFocus.Initialize(combatTarget, enemyStatsConfig.isMeleeAttacker);
            if (lootDropper != null)
                lootDropper.Configure(enemyStatsConfig, lootDropConfig);
        }

        void EnsureReferences()
        {
            if (health == null)
                health = GetComponent<EnemyHealth>();
            if (movement == null)
                movement = GetComponent<EnemyMovement>();
            if (attack == null)
                attack = GetComponent<EnemyAttack>();
            if (heroFocus == null)
                heroFocus = GetComponent<EnemyHeroFocus>();
            if (lootDropper == null)
                lootDropper = GetComponent<EnemyLootDropper>();
        }
    }
}
