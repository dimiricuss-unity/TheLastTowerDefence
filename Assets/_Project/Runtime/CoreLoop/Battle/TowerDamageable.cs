using System;
using UnityEngine;
using TheLastTowerDefence.Common.Combat;

namespace TheLastTowerDefence.CoreLoop.Battle
{
    /// <summary>
    /// HP башни: приём урона через <see cref="IDamageable"/> (враг бьёт по этому объекту).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TowerDamageable : MonoBehaviour, IDamageable
    {
        [SerializeField] float maxHealth = 1000f;

        float _max;
        float _current;

        public float CurrentHealth => _current;
        public float MaxHealth => _max;

        public event Action<float, float> HealthChanged;

        void Awake()
        {
            _max = Mathf.Max(1f, maxHealth);
            _current = _max;
            HealthChanged?.Invoke(_current, _max);
        }

        public void ApplyDamage(float amount)
        {
            if (amount <= 0f)
                return;

            _current = Mathf.Max(0f, _current - amount);
            HealthChanged?.Invoke(_current, _max);
        }
    }
}
