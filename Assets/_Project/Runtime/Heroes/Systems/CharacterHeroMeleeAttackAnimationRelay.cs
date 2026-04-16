using UnityEngine;

namespace TheLastTowerDefence.Heroes.Systems
{
    /// <summary>
    /// Вешается на тот же GameObject, что и <see cref="Animator"/> (например корень Knight из PSB).
    /// Animation Event в клипе Attack должен вызывать <see cref="OnAttackDamage"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterHeroMeleeAttackAnimationRelay : MonoBehaviour
    {
        CharacterHeroMeleeAttack _melee;

        void Awake()
        {
            _melee = transform.root.GetComponentInChildren<CharacterHeroMeleeAttack>(true);
        }

        /// <summary>Имя функции в Animation Event клипа Attack (рекомендуемое).</summary>
        public void OnAttackDamage()
        {
            if (_melee != null)
                _melee.OnAttackDamageAnimationEvent();
        }

        /// <summary>Альтернативное имя события (как у EnemyAnimationEventRelay на врагах).</summary>
        public void OnAttackAnimationHit()
        {
            OnAttackDamage();
        }
    }
}
