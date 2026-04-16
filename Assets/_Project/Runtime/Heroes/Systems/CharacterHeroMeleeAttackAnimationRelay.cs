using UnityEngine;

namespace TheLastTowerDefence.Heroes.Systems
{
    /// <summary>
    /// �������� �� ��� �� GameObject, ��� � <see cref="Animator"/> (�������� ������ Knight �� PSB).
    /// Animation Event � ����� Attack ������ �������� <see cref="OnAttackDamage"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterHeroMeleeAttackAnimationRelay : MonoBehaviour
    {
        CharacterHeroMeleeAttack _melee;

        void Awake()
        {
            _melee = transform.root.GetComponentInChildren<CharacterHeroMeleeAttack>(true);
        }

        /// <summary>��� ������� � Animation Event ����� Attack (�������������).</summary>
        public void OnAttackDamage()
        {
            if (_melee != null)
                _melee.OnAttackDamageAnimationEvent();
        }

        /// <summary>�������������� ��� ������� (��� � EnemyAnimationEventRelay �� ������).</summary>
        public void OnAttackAnimationHit()
        {
            OnAttackDamage();
        }
    }
}
