using UnityEngine;
using TheLastTowerDefence.Heroes.Domain;

namespace TheLastTowerDefence.Formulas
{
    /// <summary>
    /// Формулы производных боевых параметров из таблицы Character_stats.
    /// Документ-источник: <c>Assets/Documentation/Character_stats.xml</c>, первая строка (легенда) и ячейки со ss:Formula (R1C1).
    /// Все методы без побочных эффектов; подходят для юнит-тестов и для последующего слоя баффов/инвентаря
    /// (входящие статы уже должны включать нужные бонусы, либо использоваться через <see cref="CharacterCoreStats.ApplyModifiers"/>).
    /// </summary>
    public static class CharacterStatFormulas
    {
        /// <summary>
        /// Excel <c>INT(x)</c> для неотрицательных значений совпадает с отбрасыванием дробной части вниз.
        /// </summary>
        public static int ExcelIntDown(float value) => Mathf.FloorToInt(value);

        /// <summary>
        /// Количество HP: <c>INT(20 + Выносливость*2 + Уровень*10)</c> (строка «Количество HP», легенда в XML).
        /// </summary>
        public static float ComputeMaxHitPoints(in CharacterCoreStats core)
        {
            var v = 20f + core.Stamina * 2f + core.Level * 10f;
            return Mathf.Max(1f, ExcelIntDown(v));
        }

        /// <summary>
        /// Количество маны: <c>INT(20 + Уровень*10 + Интеллект*5 + Воля*2)</c>.
        /// </summary>
        public static float ComputeMaxMana(in CharacterCoreStats core)
        {
            var v = 20f + core.Level * 10f + core.Intelligence * 5f + core.Willpower * 2f;
            return Mathf.Max(0f, ExcelIntDown(v));
        }

        /// <summary>
        /// Восполнение маны (в единицах маны за секунду реального времени): <c>INT(Воля/2) + Уровень</c>.
        /// </summary>
        public static float ComputeManaRegenPerSecond(in CharacterCoreStats core)
        {
            return Mathf.Max(0f, ExcelIntDown(core.Willpower / 2f) + core.Level);
        }

        /// <summary>
        /// Вероятность крита в процентах: <c>MIN(75, INT(5 + Удача*0.5 + Ловкость*0.25 + модификатор крита оружия))</c>.
        /// </summary>
        public static float ComputeCritChancePercent(in CharacterCoreStats core, in CharacterWeaponStats weapon)
        {
            var raw = 5f + core.Luck * 0.5f + core.Dexterity * 0.25f + weapon.weaponCritModifier;
            var v = ExcelIntDown(raw);
            return Mathf.Clamp(v, 0f, 75f);
        }

        /// <summary>
        /// Скорость атаки (APS): <c>MIN(3, INT(APS0*(1+0.03*Стат)*100)/100)</c>, APS0 из оружия.
        /// Стат — ловкость или воля по <paramref name="attackSpeedScaling"/> (задаётся в <see cref="HeroStatsConfig"/>).
        /// </summary>
        public static float ComputeAttacksPerSecond(
            in CharacterCoreStats core,
            in CharacterWeaponStats weapon,
            AttackSpeedScalingAttribute attackSpeedScaling = AttackSpeedScalingAttribute.Dexterity)
        {
            var aps0 = Mathf.Max(0.01f, weapon.weaponAttacksPerSecond);
            var stat = attackSpeedScaling == AttackSpeedScalingAttribute.Willpower
                ? core.Willpower
                : core.Dexterity;
            var scaled = aps0 * (1f + 0.03f * stat);
            var truncated = ExcelIntDown(scaled * 100f) / 100f;
            return Mathf.Clamp(truncated, 0.01f, 3f);
        }

        /// <summary>
        /// Минимальный урон: <c>MAX(1, МинУронОружия + ОсновнойСтат + INT(Ловкость/2))</c>.
        /// Основной стат — сила, интеллект или ловкость по <paramref name="physicalDamagePrimary"/> (<see cref="HeroStatsConfig"/>).
        /// </summary>
        public static float ComputeMinPhysicalDamage(
            in CharacterCoreStats core,
            in CharacterWeaponStats weapon,
            PhysicalDamagePrimaryAttribute physicalDamagePrimary = PhysicalDamagePrimaryAttribute.Strength)
        {
            var w = Mathf.Max(0f, weapon.weaponMinDamage);
            var primary = physicalDamagePrimary switch
            {
                PhysicalDamagePrimaryAttribute.Intelligence => core.Intelligence,
                PhysicalDamagePrimaryAttribute.Dexterity => core.Dexterity,
                _ => core.Strength,
            };
            var v = w + primary + ExcelIntDown(core.Dexterity / 2f);
            return Mathf.Max(1f, v);
        }

        /// <summary>
        /// Максимальный урон: <c>MAX(1, МаксУронОружия + ОсновнойСтат + INT(Ловкость/2))</c>.
        /// </summary>
        public static float ComputeMaxPhysicalDamage(
            in CharacterCoreStats core,
            in CharacterWeaponStats weapon,
            PhysicalDamagePrimaryAttribute physicalDamagePrimary = PhysicalDamagePrimaryAttribute.Strength)
        {
            var w = Mathf.Max(0f, weapon.weaponMaxDamage);
            var primary = physicalDamagePrimary switch
            {
                PhysicalDamagePrimaryAttribute.Intelligence => core.Intelligence,
                PhysicalDamagePrimaryAttribute.Dexterity => core.Dexterity,
                _ => core.Strength,
            };
            var v = w + primary + ExcelIntDown(core.Dexterity / 2f);
            return Mathf.Max(1f, v);
        }

        /// <summary>
        /// Нижняя граница критического урона: <c>INT(Минимальный урон * 1.5)</c> (как в ячейке крита от R20C5 в таблице).
        /// </summary>
        public static float ComputeCriticalDamageMin(float minPhysicalDamageAfterOtherBonuses)
        {
            return Mathf.Max(0f, ExcelIntDown(minPhysicalDamageAfterOtherBonuses * 1.5f));
        }

        /// <summary>
        /// Верхняя граница критического урона: <c>INT(Максимальный урон * 1.5)</c>.
        /// </summary>
        public static float ComputeCriticalDamageMax(float maxPhysicalDamageAfterOtherBonuses)
        {
            return Mathf.Max(0f, ExcelIntDown(maxPhysicalDamageAfterOtherBonuses * 1.5f));
        }
    }
}
