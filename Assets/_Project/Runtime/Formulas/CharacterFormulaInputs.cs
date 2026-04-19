using System;
using UnityEngine;
using TheLastTowerDefence.Heroes.Domain;

namespace TheLastTowerDefence.Formulas
{
    /// <summary>
    /// Базовые характеристики и уровень, уже с учётом постоянных бонусов (предметы, таланты и т.д.),
    /// если вызывающий код сначала применил <see cref="CharacterStatModifiers"/>.
    /// Источник правил: <c>Assets/Documentation/Character_stats.xml</c>, лист «Характеристики».
    /// </summary>
    public readonly struct CharacterCoreStats : IEquatable<CharacterCoreStats>
    {
        public int Level { get; }
        public int Strength { get; }
        public int Dexterity { get; }
        public int Stamina { get; }
        public int Intelligence { get; }
        public int Willpower { get; }
        public int Luck { get; }

        public CharacterCoreStats(
            int level,
            int strength,
            int dexterity,
            int stamina,
            int intelligence,
            int willpower,
            int luck)
        {
            Level = Mathf.Max(1, level);
            Strength = Mathf.Max(0, strength);
            Dexterity = Mathf.Max(0, dexterity);
            Stamina = Mathf.Max(0, stamina);
            Intelligence = Mathf.Max(0, intelligence);
            Willpower = Mathf.Max(0, willpower);
            Luck = Mathf.Max(0, luck);
        }

        /// <summary>Собирает статы из <see cref="HeroStatsConfig"/> (там только база и уровень).</summary>
        public static CharacterCoreStats FromHeroConfig(HeroStatsConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            return new CharacterCoreStats(
                config.level,
                config.strength,
                config.dexterity,
                config.stamina,
                config.intelligence,
                config.willpower,
                config.luck);
        }

        /// <summary>
        /// Суммирует базу и аддитивные модификаторы (баффы, предметы, скиллы).
        /// Уровень не ниже 1, остальные не ниже 0.
        /// </summary>
        public static CharacterCoreStats ApplyModifiers(in CharacterCoreStats baseStats, in CharacterStatModifiers modifiers)
        {
            return new CharacterCoreStats(
                baseStats.Level + modifiers.LevelBonus,
                baseStats.Strength + modifiers.StrengthBonus,
                baseStats.Dexterity + modifiers.DexterityBonus,
                baseStats.Stamina + modifiers.StaminaBonus,
                baseStats.Intelligence + modifiers.IntelligenceBonus,
                baseStats.Willpower + modifiers.WillpowerBonus,
                baseStats.Luck + modifiers.LuckBonus);
        }

        public bool Equals(CharacterCoreStats other) =>
            Level == other.Level &&
            Strength == other.Strength &&
            Dexterity == other.Dexterity &&
            Stamina == other.Stamina &&
            Intelligence == other.Intelligence &&
            Willpower == other.Willpower &&
            Luck == other.Luck;

        public override bool Equals(object obj) => obj is CharacterCoreStats other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(Level, Strength, Dexterity, Stamina, Intelligence, Willpower, Luck);
    }

    /// <summary>
    /// Аддитивные бонусы к базовым статам. Пустая структура = нет модификаторов.
    /// В будущем сюда же можно добавить отдельные флаги или мультипликаторы — пока только суммы.
    /// </summary>
    [Serializable]
    public struct CharacterStatModifiers
    {
        public int LevelBonus;
        public int StrengthBonus;
        public int DexterityBonus;
        public int StaminaBonus;
        public int IntelligenceBonus;
        public int WillpowerBonus;
        public int LuckBonus;

        public static CharacterStatModifiers None => default;
    }

    /// <summary>
    /// Параметры базового оружия из таблицы (блок «Базовое оружие»): мин/макс урон, APS, модификатор крита.
    /// </summary>
    [Serializable]
    public struct CharacterWeaponStats
    {
        [Tooltip("Мин. урон оружия (как «Мин. Урон» в Character_stats).")]
        public float weaponMinDamage;

        [Tooltip("Макс. урон оружия.")]
        public float weaponMaxDamage;

        [Tooltip("Базовая скорость атаки оружия (APS0), до ловкости и капа 3.")]
        public float weaponAttacksPerSecond;

        [Tooltip("Модификатор крита базового оружия (в формуле крита суммируется внутри INT).")]
        public float weaponCritModifier;

        /// <summary>Значения как в примерной строке Character_stats (6 / 8 / 0.9 / 1).</summary>
        public static CharacterWeaponStats TableExampleDefaults => new CharacterWeaponStats
        {
            weaponMinDamage = 6f,
            weaponMaxDamage = 8f,
            weaponAttacksPerSecond = 0.9f,
            weaponCritModifier = 1f,
        };
    }
}
