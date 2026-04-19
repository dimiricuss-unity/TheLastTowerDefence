using UnityEngine;
using TheLastTowerDefence.Formulas;

namespace TheLastTowerDefence.Heroes.Domain
{
    /// <summary>
    /// Базовое оружие героя — те же поля, что блок «Базовое оружие» в <c>Character_stats.xml</c>
    /// (мин/макс урон, APS, модификатор крита). Ссылка хранится в <see cref="HeroStatsConfig.weapon"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "HeroWeapon", menuName = "TLTD/Heroes/Hero Weapon Config")]
    public sealed class HeroWeaponConfig : ScriptableObject
    {
        [Header("Как в Character_stats")]
        [Tooltip("Название оружия (колонка «Название» в таблице).")]
        public string weaponDisplayName = "";

        [Tooltip("Тип оружия (для геймплея/UI; расширяйте enum по мере появления классов).")]
        public HeroWeaponKind weaponType = HeroWeaponKind.Melee;

        [Tooltip("Мин. урон оружия.")]
        [Min(0f)] public float minDamage = 6f;

        [Tooltip("Макс. урон оружия.")]
        [Min(0f)] public float maxDamage = 8f;

        [Tooltip("Скорость атаки оружия (базовый APS до ловкости и капа в формулах).")]
        [Min(0.01f)] public float attacksPerSecond = 0.9f;

        [Tooltip("Модификатор крита базового оружия (слагаемое в INT внутри формулы крита).")]
        public float critModifier = 1f;

        /// <summary>
        /// Преобразует поля ассета в структуру для <see cref="CharacterStatFormulas"/>.
        /// </summary>
        public CharacterWeaponStats ToFormulaWeaponStats()
        {
            return new CharacterWeaponStats
            {
                weaponMinDamage = Mathf.Max(0f, minDamage),
                weaponMaxDamage = Mathf.Max(0f, maxDamage),
                weaponAttacksPerSecond = Mathf.Max(0.01f, attacksPerSecond),
                weaponCritModifier = critModifier,
            };
        }
    }
}
