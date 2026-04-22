using NUnit.Framework;
using TheLastTowerDefence.Formulas;
using TheLastTowerDefence.Heroes.Domain;

namespace TheLastTowerDefence.Tests
{
    public sealed class CharacterStatFormulasTests
    {
        static CharacterWeaponStats ApsOne =>
            new CharacterWeaponStats
            {
                weaponMinDamage = 0f,
                weaponMaxDamage = 0f,
                weaponAttacksPerSecond = 1f,
                weaponCritModifier = 0f,
            };

        [Test]
        public void ComputeAttacksPerSecond_uses_dexterity_by_default()
        {
            var highDex = new CharacterCoreStats(0, 0, 20, 0, 0, 0, 0);
            var highWill = new CharacterCoreStats(0, 0, 0, 0, 0, 20, 0);

            var apsDex = CharacterStatFormulas.ComputeAttacksPerSecond(highDex, ApsOne, AttackSpeedScalingAttribute.Dexterity);
            var apsWillIgnored = CharacterStatFormulas.ComputeAttacksPerSecond(highWill, ApsOne, AttackSpeedScalingAttribute.Dexterity);

            Assert.Greater(apsDex, apsWillIgnored);
        }

        [Test]
        public void ComputeAttacksPerSecond_uses_willpower_when_scaling_will()
        {
            var highDexLowWill = new CharacterCoreStats(0, 0, 20, 0, 0, 0, 0);
            var lowDexHighWill = new CharacterCoreStats(0, 0, 0, 0, 0, 20, 0);

            var apsDexScaling = CharacterStatFormulas.ComputeAttacksPerSecond(
                highDexLowWill,
                ApsOne,
                AttackSpeedScalingAttribute.Dexterity);

            var apsWillScaling = CharacterStatFormulas.ComputeAttacksPerSecond(
                lowDexHighWill,
                ApsOne,
                AttackSpeedScalingAttribute.Willpower);

            Assert.Greater(apsDexScaling, apsWillScaling);
        }

        [Test]
        public void Compute_physical_damage_uses_intelligence_when_primary_int()
        {
            var weapon = new CharacterWeaponStats
            {
                weaponMinDamage = 10f,
                weaponMaxDamage = 20f,
                weaponAttacksPerSecond = 1f,
                weaponCritModifier = 0f,
            };

            var highStrLowInt = new CharacterCoreStats(0, 20, 0, 0, 0, 0, 0);
            var lowStrHighInt = new CharacterCoreStats(0, 0, 0, 0, 20, 0, 0);

            var minStr = CharacterStatFormulas.ComputeMinPhysicalDamage(
                highStrLowInt,
                weapon,
                PhysicalDamagePrimaryAttribute.Strength);
            var minInt = CharacterStatFormulas.ComputeMinPhysicalDamage(
                lowStrHighInt,
                weapon,
                PhysicalDamagePrimaryAttribute.Intelligence);

            Assert.Greater(minStr, minInt);

            var maxStr = CharacterStatFormulas.ComputeMaxPhysicalDamage(
                highStrLowInt,
                weapon,
                PhysicalDamagePrimaryAttribute.Strength);
            var maxInt = CharacterStatFormulas.ComputeMaxPhysicalDamage(
                lowStrHighInt,
                weapon,
                PhysicalDamagePrimaryAttribute.Intelligence);

            Assert.Greater(maxStr, maxInt);
        }

        [Test]
        public void Compute_physical_damage_uses_dexterity_when_primary_dex()
        {
            var weapon = new CharacterWeaponStats
            {
                weaponMinDamage = 10f,
                weaponMaxDamage = 20f,
                weaponAttacksPerSecond = 1f,
                weaponCritModifier = 0f,
            };

            var highStrNoDex = new CharacterCoreStats(0, 20, 0, 0, 0, 0, 0);
            var noStrHighDex = new CharacterCoreStats(0, 0, 20, 0, 0, 0, 0);

            var minDexBuild = CharacterStatFormulas.ComputeMinPhysicalDamage(
                noStrHighDex,
                weapon,
                PhysicalDamagePrimaryAttribute.Dexterity);
            var minStrBuildWithDexPrimary = CharacterStatFormulas.ComputeMinPhysicalDamage(
                highStrNoDex,
                weapon,
                PhysicalDamagePrimaryAttribute.Dexterity);

            Assert.Greater(minDexBuild, minStrBuildWithDexPrimary);

            var maxDexBuild = CharacterStatFormulas.ComputeMaxPhysicalDamage(
                noStrHighDex,
                weapon,
                PhysicalDamagePrimaryAttribute.Dexterity);
            var maxStrBuildWithDexPrimary = CharacterStatFormulas.ComputeMaxPhysicalDamage(
                highStrNoDex,
                weapon,
                PhysicalDamagePrimaryAttribute.Dexterity);

            Assert.Greater(maxDexBuild, maxStrBuildWithDexPrimary);
        }
    }
}
