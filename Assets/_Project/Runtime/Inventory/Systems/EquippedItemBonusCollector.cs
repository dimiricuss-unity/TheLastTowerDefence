using UnityEngine;
using TheLastTowerDefence.Formulas;
using TheLastTowerDefence.Inventory.Domain;

namespace TheLastTowerDefence.Inventory.Systems
{
    /// <summary>
    /// Суммирует ненулевые поля <see cref="InventoryItemStatsConfig"/> со всех слотов экипировки владельца (в т.ч. на неактивных окнах).
    /// </summary>
    public static class EquippedItemBonusCollector
    {
        const float Epsilon = 1e-5f;

        public static void CollectForOwner(
            PlayableCharacterClass owner,
            out CharacterStatModifiers statAndFlatBonuses,
            out CharacterWeaponStats weaponStatDelta)
        {
            statAndFlatBonuses = CharacterStatModifiers.None;
            weaponStatDelta = default;

            var slots = Object.FindObjectsByType<EquipmentSlotCellView>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (var i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.OwnerClass != owner)
                {
                    continue;
                }

                var view = slot.EquippedItem;
                if (view == null || view.Config == null)
                {
                    continue;
                }

                Accumulate(view.Config, ref statAndFlatBonuses, ref weaponStatDelta);
            }
        }

        static void Accumulate(
            InventoryItemConfig c,
            ref CharacterStatModifiers m,
            ref CharacterWeaponStats w)
        {
            var stats = c.statsConfig;
            if (stats == null)
            {
                return;
            }

            if (stats.bonusStrength != 0)
            {
                m.StrengthBonus += stats.bonusStrength;
            }

            if (stats.bonusDexterity != 0)
            {
                m.DexterityBonus += stats.bonusDexterity;
            }

            if (stats.bonusStamina != 0)
            {
                m.StaminaBonus += stats.bonusStamina;
            }

            if (stats.bonusIntelligence != 0)
            {
                m.IntelligenceBonus += stats.bonusIntelligence;
            }

            if (stats.bonusWillpower != 0)
            {
                m.WillpowerBonus += stats.bonusWillpower;
            }

            if (stats.bonusLuck != 0)
            {
                m.LuckBonus += stats.bonusLuck;
            }

            if (stats.weaponArmorRating != 0)
            {
                m.ArmorRatingBonus += stats.weaponArmorRating;
            }

            if (Mathf.Abs(stats.bonusMaxHp) > Epsilon)
            {
                m.MaxHpBonus += stats.bonusMaxHp;
            }

            if (Mathf.Abs(stats.bonusMaxMana) > Epsilon)
            {
                m.MaxManaBonus += stats.bonusMaxMana;
            }

            if (Mathf.Abs(stats.bonusManaRegenPerSecond) > Epsilon)
            {
                m.ManaRegenPerSecondBonus += stats.bonusManaRegenPerSecond;
            }

            if (Mathf.Abs(stats.bonusCriticalDamage) > Epsilon)
            {
                m.CriticalDamageBonus += stats.bonusCriticalDamage;
            }

            if (Mathf.Abs(stats.weaponBaseMinDamage) > Epsilon)
            {
                w.weaponMinDamage += stats.weaponBaseMinDamage;
            }

            if (Mathf.Abs(stats.weaponBaseMaxDamage) > Epsilon)
            {
                w.weaponMaxDamage += stats.weaponBaseMaxDamage;
            }

            if (Mathf.Abs(stats.weaponBaseAttacksPerSecond) > Epsilon)
            {
                w.weaponAttacksPerSecond += stats.weaponBaseAttacksPerSecond;
            }

            if (Mathf.Abs(stats.weaponBaseCritModifier) > Epsilon)
            {
                w.weaponCritModifier += stats.weaponBaseCritModifier;
            }
        }

        /// <summary>
        /// Базовое оружие героя + суммарные ненулевые поля оружия с предметов.
        /// </summary>
        public static CharacterWeaponStats MergeWeaponWithEquipmentDelta(
            CharacterWeaponStats baseWeapon,
            CharacterWeaponStats delta)
        {
            var m = baseWeapon;
            if (Mathf.Abs(delta.weaponMinDamage) > Epsilon)
            {
                m.weaponMinDamage += delta.weaponMinDamage;
            }

            if (Mathf.Abs(delta.weaponMaxDamage) > Epsilon)
            {
                m.weaponMaxDamage += delta.weaponMaxDamage;
            }

            if (Mathf.Abs(delta.weaponAttacksPerSecond) > Epsilon)
            {
                m.weaponAttacksPerSecond += delta.weaponAttacksPerSecond;
            }

            if (Mathf.Abs(delta.weaponCritModifier) > Epsilon)
            {
                m.weaponCritModifier += delta.weaponCritModifier;
            }

            m.weaponMinDamage = Mathf.Max(0f, m.weaponMinDamage);
            m.weaponMaxDamage = Mathf.Max(0f, m.weaponMaxDamage);
            m.weaponAttacksPerSecond = Mathf.Max(0.01f, m.weaponAttacksPerSecond);
            return m;
        }
    }
}
