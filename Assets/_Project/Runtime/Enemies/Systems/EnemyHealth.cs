using System;
using UnityEngine;
using TheLastTowerDefence.Enemies.Domain;
using TheLastTowerDefence.UI;

namespace TheLastTowerDefence.Enemies.Systems
{
    /// <summary>
    /// Текущее HP, приём урона; при смерти отключает атаку/движение, шлёт <see cref="Died"/>, затем отключает объект врага.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyHealth : MonoBehaviour
    {
        EnemyMovement _movement;
        EnemyAttack _attack;

        float _max;
        float _current;
        bool _configured;

        public float CurrentHealth => _current;
        public float MaxHealth => _max;
        public bool IsAlive => _configured && _current > 0f;

        public event Action<float, float> HealthChanged;
        public event Action<EnemyHealth> Died;

        void Awake()
        {
            _movement = GetComponent<EnemyMovement>();
            _attack = GetComponent<EnemyAttack>();
        }

        public void Configure(EnemyStatsConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            _max = Mathf.Max(1f, config.maxHealth);
            _current = _max;
            _configured = true;
            HealthChanged?.Invoke(_current, _max);
        }

        public void ApplyDamage(float amount)
        {
            if (!_configured || !IsAlive || amount <= 0f)
                return;

            _current = Mathf.Max(0f, _current - amount);
            HealthChanged?.Invoke(_current, _max);
            DamageFloaterRoot.ShowAtEnemy(this, amount);

            if (_current <= 0f)
                Die();
        }

        void Die()
        {
            if (_movement != null)
                _movement.enabled = false;
            if (_attack != null)
                _attack.enabled = false;

            Died?.Invoke(this);
            gameObject.SetActive(false);
        }
    }
}
