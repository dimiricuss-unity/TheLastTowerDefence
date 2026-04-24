using UnityEngine;
using TheLastTowerDefence.Formulas;
using TheLastTowerDefence.Inventory.Domain;

namespace TheLastTowerDefence.Inventory.Systems
{
    /// <summary>
    /// Суммирует ненулевые поля <see cref="InventoryItemConfig"/> со всех слотов экипировки владельца (в т.ч. на неактивных окнах).
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
            if (c.bonusStrength != 0)
            {
                m.StrengthBonus += c.bonusStrength;
            }

            if (c.bonusDexterity != 0)
            {
                m.DexterityBonus += c.bonusDexterity;
            }

            if (c.bonusStamina != 0)
            {
                m.StaminaBonus += c.bonusStamina;
            }

            if (c.bonusIntelligence != 0)
            {
                m.IntelligenceBonus += c.bonusIntelligence;
            }

            if (c.bonusWillpower != 0)
            {
                m.WillpowerBonus += c.bonusWillpower;
            }

            if (c.bonusLuck != 0)
            {
                m.LuckBonus += c.bonusLuck;
            }

            if (c.weaponArmorRating != 0)
            {
                m.ArmorRatingBonus += c.weaponArmorRating;
            }

            if (Mathf.Abs(c.bonusMaxHp) > Epsilon)
            {
                m.MaxHpBonus += c.bonusMaxHp;
            }

            if (Mathf.Abs(c.bonusMaxMana) > Epsilon)
            {
                m.MaxManaBonus += c.bonusMaxMana;
            }

            if (Mathf.Abs(c.bonusManaRegenPerSecond) > Epsilon)
            {
                m.ManaRegenPerSecondBonus += c.bonusManaRegenPerSecond;
            }

            if (Mathf.Abs(c.bonusCriticalDamage) > Epsilon)
            {
                m.CriticalDamageBonus += c.bonusCriticalDamage;
            }

            if (Mathf.Abs(c.weaponBaseMinDamage) > Epsilon)
            {
                w.weaponMinDamage += c.weaponBaseMinDamage;
            }

            if (Mathf.Abs(c.weaponBaseMaxDamage) > Epsilon)
            {
                w.weaponMaxDamage += c.weaponBaseMaxDamage;
            }

            if (Mathf.Abs(c.weaponBaseAttacksPerSecond) > Epsilon)
            {
                w.weaponAttacksPerSecond += c.weaponBaseAttacksPerSecond;
            }

            if (Mathf.Abs(c.weaponBaseCritModifier) > Epsilon)
            {
                w.weaponCritModifier += c.weaponBaseCritModifier;
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
