using UnityEngine;

namespace TheLastTowerDefence.Heroes.Systems
{
    /// <summary>
    /// Вешается на объект с <see cref="Animator"/>. Animation Event в клипе Attack вызывает <see cref="OnAttackDamage"/>.
    /// Поддерживает <see cref="CharacterHeroMeleeAttack"/> (рыцарь) и <see cref="CharacterHeroRangeAttack"/> (лучник), не оба сразу.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterHeroMeleeAttackAnimationRelay : MonoBehaviour
    {
        CharacterHeroMeleeAttack _melee;
        CharacterHeroRangeAttack _range;

        void Awake()
        {
            var root = transform.root;
            _melee = root.GetComponentInChildren<CharacterHeroMeleeAttack>(true);
            _range = root.GetComponentInChildren<CharacterHeroRangeAttack>(true);
        }

        /// <summary>Имя функции в Animation Event клипа Attack (произвольное).</summary>
        public void OnAttackDamage()
        {
            if (_melee != null)
                _melee.OnAttackDamageAnimationEvent();
            else if (_range != null)
                _range.OnAttackDamageAnimationEvent();
        }

        /// <summary>Алиас для события (как у EnemyAnimationEventRelay на врагах).</summary>
        public void OnAttackAnimationHit()
        {
            OnAttackDamage();
        }
    }
}
