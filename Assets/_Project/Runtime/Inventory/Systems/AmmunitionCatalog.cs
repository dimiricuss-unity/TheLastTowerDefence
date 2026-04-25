using System.Collections.Generic;
using UnityEngine;
using TheLastTowerDefence.Inventory.Domain;

namespace TheLastTowerDefence.Inventory.Systems
{
    [System.Serializable]
    public sealed class ItemStatsRangesByRarity
    {
        [Header("Base")]
        [SerializeField] List<InventoryItemStatsRangeConfig> baseItems = new();

        [Header("Rare")]
        [SerializeField] List<InventoryItemStatsRangeConfig> rareItems = new();

        [Header("Magic")]
        [SerializeField] List<InventoryItemStatsRangeConfig> magicItems = new();

        [Header("Legendary")]
        [SerializeField] List<InventoryItemStatsRangeConfig> legendaryItems = new();

        [Header("Epic")]
        [SerializeField] List<InventoryItemStatsRangeConfig> epicItems = new();

        [Header("Relict")]
        [SerializeField] List<InventoryItemStatsRangeConfig> relictItems = new();

        public IReadOnlyList<InventoryItemStatsRangeConfig> BaseItems => baseItems;
        public IReadOnlyList<InventoryItemStatsRangeConfig> RareItems => rareItems;
        public IReadOnlyList<InventoryItemStatsRangeConfig> MagicItems => magicItems;
        public IReadOnlyList<InventoryItemStatsRangeConfig> LegendaryItems => legendaryItems;
        public IReadOnlyList<InventoryItemStatsRangeConfig> EpicItems => epicItems;
        public IReadOnlyList<InventoryItemStatsRangeConfig> RelictItems => relictItems;

        public IReadOnlyList<InventoryItemStatsRangeConfig> GetByRarity(InventoryItemRarity rarity)
        {
            return rarity switch
            {
                InventoryItemRarity.Base => baseItems,
                InventoryItemRarity.Rare => rareItems,
                InventoryItemRarity.Magic => magicItems,
                InventoryItemRarity.Legendary => legendaryItems,
                InventoryItemRarity.Epic => epicItems,
                InventoryItemRarity.Relict => relictItems,
                _ => baseItems,
            };
        }
    }

    [DisallowMultipleComponent]
    public sealed class AmmunitionCatalog : MonoBehaviour
    {
        [Header("Item Configs")]
        [SerializeField] List<InventoryItemConfig> itemConfigs = new();

        [Header("Item Stats Ranges")]
        [SerializeField] ItemStatsRangesByRarity itemStatsRangesByRarity = new();

        [Header("Prefab")]
        [SerializeField] InventoryItemView inventoryItemPrefab;

        public IReadOnlyList<InventoryItemConfig> ItemConfigs => itemConfigs;
        public ItemStatsRangesByRarity ItemStatsRangesByRarity => itemStatsRangesByRarity;
        public InventoryItemView InventoryItemPrefab => inventoryItemPrefab;

        public bool TryBuildItemForRarity(InventoryItemRarity rarity, out InventoryItemView builtItemView)
        {
            builtItemView = null;
            if (inventoryItemPrefab == null)
            {
                return false;
            }

            var rangesPool = itemStatsRangesByRarity.GetByRarity(rarity);
            if (!TryPickRandomNonNull(rangesPool, out var selectedRange))
            {
                return false;
            }

            if (!TryPickTemplateForRange(selectedRange, out var selectedTemplate))
            {
                return false;
            }

            var rolledStats = BuildRolledStatsConfig(selectedRange, rarity);
            var runtimeItemConfig = BuildRuntimeItemConfig(selectedTemplate, rolledStats);
            if (runtimeItemConfig == null)
            {
                return false;
            }

            var itemInstance = Instantiate(inventoryItemPrefab);
            itemInstance.SetRuntimeConfig(runtimeItemConfig);
            itemInstance.SetRuntimeIconTint(selectedRange.iconColor);
            builtItemView = itemInstance;
            return true;
        }

        bool TryPickTemplateForRange(
            InventoryItemStatsRangeConfig selectedRange,
            out InventoryItemConfig selectedTemplate)
        {
            selectedTemplate = null;
            var candidates = new List<InventoryItemConfig>();
            for (var i = 0; i < itemConfigs.Count; i++)
            {
                var cfg = itemConfigs[i];
                if (cfg == null)
                {
                    continue;
                }

                if (cfg.slotType != selectedRange.slotType)
                {
                    continue;
                }

                if (cfg.equipableCharacter != selectedRange.equipableCharacter)
                {
                    continue;
                }

                candidates.Add(cfg);
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            selectedTemplate = candidates[Random.Range(0, candidates.Count)];
            return selectedTemplate != null;
        }

        static bool TryPickRandomNonNull(
            IReadOnlyList<InventoryItemStatsRangeConfig> pool,
            out InventoryItemStatsRangeConfig picked)
        {
            picked = null;
            if (pool == null || pool.Count == 0)
            {
                return false;
            }

            var candidates = new List<InventoryItemStatsRangeConfig>();
            for (var i = 0; i < pool.Count; i++)
            {
                if (pool[i] != null)
                {
                    candidates.Add(pool[i]);
                }
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            picked = candidates[Random.Range(0, candidates.Count)];
            return picked != null;
        }

        static InventoryItemConfig BuildRuntimeItemConfig(
            InventoryItemConfig template,
            InventoryItemStatsConfig rolledStats)
        {
            if (template == null || rolledStats == null)
            {
                return null;
            }

            var runtime = ScriptableObject.CreateInstance<InventoryItemConfig>();
            runtime.itemIcon = template.itemIcon;
            runtime.displayName = template.displayName;
            runtime.description = template.description;
            runtime.slotType = template.slotType;
            runtime.equipableCharacter = template.equipableCharacter;
            runtime.sizeInCellsX = Mathf.Max(1, template.sizeInCellsX);
            runtime.sizeInCellsY = Mathf.Max(1, template.sizeInCellsY);
            runtime.statsConfig = rolledStats;
            return runtime;
        }

        static InventoryItemStatsConfig BuildRolledStatsConfig(
            InventoryItemStatsRangeConfig range,
            InventoryItemRarity rarity)
        {
            var rolled = ScriptableObject.CreateInstance<InventoryItemStatsConfig>();
            rolled.slotType = range.slotType;
            rolled.equipableCharacter = range.equipableCharacter;
            rolled.rarity = rarity;

            rolled.weaponBaseMinDamage = RollFloat(range.weaponBaseMinDamageMin, range.weaponBaseMinDamageMax);
            rolled.weaponBaseMaxDamage = RollFloat(range.weaponBaseMaxDamageMin, range.weaponBaseMaxDamageMax);
            rolled.weaponBaseAttacksPerSecond = RollFloat(range.weaponBaseAttacksPerSecondMin, range.weaponBaseAttacksPerSecondMax);
            rolled.weaponBaseCritModifier = RollFloat(range.weaponBaseCritModifierMin, range.weaponBaseCritModifierMax);
            rolled.weaponArmorRating = RollInt(range.weaponArmorRatingMin, range.weaponArmorRatingMax);

            rolled.bonusStrength = RollInt(range.bonusStrengthMin, range.bonusStrengthMax);
            rolled.bonusDexterity = RollInt(range.bonusDexterityMin, range.bonusDexterityMax);
            rolled.bonusStamina = RollInt(range.bonusStaminaMin, range.bonusStaminaMax);
            rolled.bonusIntelligence = RollInt(range.bonusIntelligenceMin, range.bonusIntelligenceMax);
            rolled.bonusWillpower = RollInt(range.bonusWillpowerMin, range.bonusWillpowerMax);
            rolled.bonusLuck = RollInt(range.bonusLuckMin, range.bonusLuckMax);

            rolled.bonusMaxHp = RollFloat(range.bonusMaxHpMin, range.bonusMaxHpMax);
            rolled.bonusMaxMana = RollFloat(range.bonusMaxManaMin, range.bonusMaxManaMax);
            rolled.bonusManaRegenPerSecond = RollFloat(range.bonusManaRegenPerSecondMin, range.bonusManaRegenPerSecondMax);
            rolled.bonusCriticalDamage = RollFloat(range.bonusCriticalDamageMin, range.bonusCriticalDamageMax);
            rolled.price = Mathf.Max(0, RollInt(range.priceMin, range.priceMax));
            return rolled;
        }

        static float RollFloat(float min, float max)
        {
            if (Mathf.Approximately(min, 0f) && Mathf.Approximately(max, 0f))
            {
                return 0f;
            }

            if (min > max)
            {
                (min, max) = (max, min);
            }

            return Mathf.Approximately(min, max) ? min : Random.Range(min, max);
        }

        static int RollInt(int min, int max)
        {
            if (min == 0 && max == 0)
            {
                return 0;
            }

            if (min > max)
            {
                (min, max) = (max, min);
            }

            if (min == max)
            {
                return min;
            }

            return Random.Range(min, max + 1);
        }
    }
}
