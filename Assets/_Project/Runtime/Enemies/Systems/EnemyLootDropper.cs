using UnityEngine;
using TheLastTowerDefence.Enemies.Domain;
using TheLastTowerDefence.Inventory.Domain;
using TheLastTowerDefence.Inventory.Systems;

namespace TheLastTowerDefence.Enemies.Systems
{
    /// <summary>
    /// Разыгрывает дроп сундука в момент смерти врага на основе настроек из <see cref="EnemyStatsConfig"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class EnemyLootDropper : MonoBehaviour
    {
        [Header("Chest Prefabs by rarity")]
        [SerializeField] GameObject baseChestPrefab;
        [SerializeField] GameObject rareChestPrefab;
        [SerializeField] GameObject magicChestPrefab;
        [SerializeField] GameObject legendaryChestPrefab;
        [SerializeField] GameObject epicChestPrefab;
        [SerializeField] GameObject relictChestPrefab;

        EnemyHealth _health;
        EnemyStatsConfig _config;
        AmmunitionCatalog _ammunitionCatalog;

        void Awake()
        {
            _health = GetComponent<EnemyHealth>();
            _ammunitionCatalog = FindFirstObjectByType<AmmunitionCatalog>();
        }

        void OnEnable()
        {
            if (_health != null)
            {
                _health.Died += OnEnemyDied;
            }
        }

        void OnDisable()
        {
            if (_health != null)
            {
                _health.Died -= OnEnemyDied;
            }
        }

        public void Configure(EnemyStatsConfig config)
        {
            _config = config;
        }

        void OnEnemyDied(EnemyHealth _)
        {
            if (_config == null)
            {
                return;
            }

            if (!PassesGlobalLootRoll(_config.totalLootDropChancePercent))
            {
                return;
            }

            if (!TryPickChestPrefab(_config, out var selectedChestPrefab, out var selectedRarity) || selectedChestPrefab == null)
            {
                return;
            }

            var chestInstance = Instantiate(selectedChestPrefab, transform.position, Quaternion.identity);
            PutConstructedItemIntoChest(chestInstance, selectedRarity);
        }

        static bool PassesGlobalLootRoll(float chancePercent)
        {
            var clampedChance = Mathf.Clamp(chancePercent, 0f, 100f);
            return Random.value * 100f <= clampedChance;
        }

        void PutConstructedItemIntoChest(GameObject chestInstance, LootRarity selectedRarity)
        {
            if (chestInstance == null)
            {
                return;
            }

            if (_ammunitionCatalog == null)
            {
                _ammunitionCatalog = FindFirstObjectByType<AmmunitionCatalog>();
            }

            if (_ammunitionCatalog == null)
            {
                return;
            }

            var inventoryRarity = ToInventoryRarity(selectedRarity);
            if (!_ammunitionCatalog.TryBuildItemForRarity(inventoryRarity, out var builtItem) || builtItem == null)
            {
                return;
            }

            var container = chestInstance.GetComponent<EnemyLootChestContainer>();
            if (container == null)
            {
                container = chestInstance.AddComponent<EnemyLootChestContainer>();
            }

            container.SetStoredItem(builtItem);
        }

        bool TryPickChestPrefab(
            EnemyStatsConfig config,
            out GameObject selectedChestPrefab,
            out LootRarity selectedRarity)
        {
            selectedChestPrefab = null;
            selectedRarity = LootRarity.Base;
            var sections = config.lootDropSections;
            if (sections == null || sections.Length == 0)
            {
                return false;
            }

            var totalWeight = 0f;
            for (var i = 0; i < sections.Length; i++)
            {
                var section = sections[i];
                if (section == null || !section.isEnabledInRoll)
                {
                    continue;
                }

                var prefab = GetChestPrefab(section.rarity);
                if (prefab == null)
                {
                    continue;
                }

                totalWeight += Mathf.Max(0f, section.lootDropChancePercent);
            }

            if (totalWeight <= 0f)
            {
                return false;
            }

            var roll = Random.value * totalWeight;
            var cumulative = 0f;
            for (var i = 0; i < sections.Length; i++)
            {
                var section = sections[i];
                if (section == null || !section.isEnabledInRoll)
                {
                    continue;
                }

                var prefab = GetChestPrefab(section.rarity);
                if (prefab == null)
                {
                    continue;
                }

                var weight = Mathf.Max(0f, section.lootDropChancePercent);
                if (weight <= 0f)
                {
                    continue;
                }

                cumulative += weight;
                if (roll <= cumulative)
                {
                    selectedChestPrefab = prefab;
                    selectedRarity = section.rarity;
                    return true;
                }
            }

            return false;
        }

        static InventoryItemRarity ToInventoryRarity(LootRarity rarity)
        {
            return rarity switch
            {
                LootRarity.Base => InventoryItemRarity.Base,
                LootRarity.Rare => InventoryItemRarity.Rare,
                LootRarity.Magic => InventoryItemRarity.Magic,
                LootRarity.Legendary => InventoryItemRarity.Legendary,
                LootRarity.Epic => InventoryItemRarity.Epic,
                LootRarity.Relict => InventoryItemRarity.Relict,
                _ => InventoryItemRarity.Base,
            };
        }

        GameObject GetChestPrefab(LootRarity rarity)
        {
            return rarity switch
            {
                LootRarity.Base => baseChestPrefab,
                LootRarity.Rare => rareChestPrefab,
                LootRarity.Magic => magicChestPrefab,
                LootRarity.Legendary => legendaryChestPrefab,
                LootRarity.Epic => epicChestPrefab,
                LootRarity.Relict => relictChestPrefab,
                _ => null,
            };
        }
    }
}
