using UnityEngine;

namespace TheLastTowerDefence.Heroes.Domain
{
    /// <summary>
    /// Стартовые базовые характеристики героя и ссылка на базовое оружие.
    /// Боевые параметры считаются в <see cref="TheLastTowerDefence.Formulas.CharacterStatFormulas"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "HeroStats", menuName = "TLTD/Heroes/Hero Stats Config")]
    public sealed class HeroStatsConfig : ScriptableObject
    {
        [Header("Базовое оружие (Character_stats)")]
        [Tooltip("SO оружия: название, тип, мин/макс урон, APS, модификатор крита. Если пусто — в бою подставятся TableExampleDefaults в CharacterHeroStats.")]
        public HeroWeaponConfig weapon;

        [Header("Базовые характеристики")]
        [Tooltip("Уровень персонажа (стартовое значение из дизайн-таблицы).")]
        [Min(1)] public int level = 1;

        [Tooltip("Сила.")]
        [Min(0)] public int strength = 9;

        [Tooltip("Ловкость.")]
        [Min(0)] public int dexterity = 6;

        [Tooltip("Выносливость.")]
        [Min(0)] public int stamina = 8;

        [Tooltip("Интеллект.")]
        [Min(0)] public int intelligence = 5;

        [Tooltip("Воля.")]
        [Min(0)] public int willpower = 5;

        [Tooltip("Удача.")]
        [Min(0)] public int luck = 7;
    }
}
