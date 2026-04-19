using UnityEngine;
using TheLastTowerDefence.Heroes.Domain;
using TheLastTowerDefence.UI;

namespace TheLastTowerDefence.Heroes.Systems
{
    /// <summary>
    /// Активное заклинание клирика (лечение рыцаря). Решение «лечить или стрелять» использует <see cref="CharacterHeroRangeAttack"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterClericMagic : MonoBehaviour
    {
        const float FullHpEpsilon = 1e-3f;

        [SerializeField] ClericHealSpellConfig activeHealSpell;
        [SerializeField] CharacterHeroStats knight;

        [Tooltip("Плюс HP на Knight_icon. Пусто — ищется первый KnightHealHitPlusView на сцене.")]
        [SerializeField] KnightHealHitPlusView knightHealHitPlus;

        CharacterHeroStats _cleric;
        KnightHealHitPlusView _resolvedHealPlus;

        void Awake()
        {
            _cleric = GetComponentInParent<CharacterHeroStats>();
        }

        /// <summary>
        /// Нужно ли вместо дальней атаки по врагам тратить тики APS на лечение рыцаря.
        /// </summary>
        public bool ShouldPrioritizeHealing()
        {
            if (activeHealSpell == null || knight == null || !knight.IsAlive)
                return false;
            if (_cleric == null || !_cleric.IsAlive)
                return false;
            if (knight.CurrentHealth >= knight.MaxHealth - FullHpEpsilon)
                return false;
            if (_cleric.CurrentMana + 1e-6f < activeHealSpell.manaCost)
                return false;
            return true;
        }

        /// <summary>
        /// Списывает ману и восстанавливает HP рыцарю (не больше недостающего до макс). Без поворота клирика.
        /// </summary>
        public bool TryPerformHeal()
        {
            if (activeHealSpell == null || knight == null || !knight.IsAlive)
                return false;
            if (_cleric == null || !_cleric.IsAlive)
                return false;
            if (knight.CurrentHealth >= knight.MaxHealth - FullHpEpsilon)
                return false;
            if (!_cleric.TryConsumeMana(activeHealSpell.manaCost))
                return false;

            var applied = knight.RestoreHealth(activeHealSpell.healAmount);
            if (applied > 0f)
                ResolveKnightHealHitPlus()?.ShowHealAmount(applied);

            return true;
        }

        KnightHealHitPlusView ResolveKnightHealHitPlus()
        {
            if (knightHealHitPlus != null)
                return knightHealHitPlus;
            if (_resolvedHealPlus != null)
                return _resolvedHealPlus;

            var all = Object.FindObjectsByType<KnightHealHitPlusView>(FindObjectsSortMode.None);
            _resolvedHealPlus = all != null && all.Length > 0 ? all[0] : null;
            return _resolvedHealPlus;
        }
    }
}
