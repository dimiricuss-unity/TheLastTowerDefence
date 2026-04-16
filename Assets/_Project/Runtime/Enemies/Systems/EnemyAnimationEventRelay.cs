using UnityEngine;

namespace TheLastTowerDefence.Enemies.Systems
{
    /// <summary>
    /// Relay для Animation Event с дочернего объекта (где стоит Animator) на корневой EnemyAttack.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyAnimationEventRelay : MonoBehaviour
    {
        EnemyAttack _attack;

        void Awake()
        {
            _attack = GetComponentInParent<EnemyAttack>();
        }

        /// <summary>
        /// Назначай эту функцию в Animation Event клипа Attack.
        /// </summary>
        public void OnAttackAnimationHit()
        {
            if (_attack == null)
                return;

            _attack.OnAttackAnimationHit();
        }
    }
}
