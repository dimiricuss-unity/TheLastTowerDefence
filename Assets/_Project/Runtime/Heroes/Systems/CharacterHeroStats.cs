using System;
using UnityEngine;
using TheLastTowerDefence.Common.Combat;
using TheLastTowerDefence.Heroes.Domain;

namespace TheLastTowerDefence.Heroes.Systems
{
    /// <summary>
    /// Applies <see cref="HeroStatsConfig"/>; HP via <see cref="IDamageable"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterHeroStats : MonoBehaviour, IDamageable
    {
        [SerializeField] HeroStatsConfig config;

        float _maxHealth;
        float _currentHealth;
        float _damage;
        float _attacksPerSecond;
        bool _configured;

        public HeroStatsConfig Config => config;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
        public float Damage => _damage;
        public float AttacksPerSecond => _attacksPerSecond;
        public bool IsAlive => _configured && _currentHealth > 0f;

        public event Action<float, float> HealthChanged;
        public event Action<CharacterHeroStats> Died;

        void Awake()
        {
            ApplyConfig();
        }

        void ApplyConfig()
        {
            if (config == null)
            {
                _configured = false;
                Debug.LogError($"[{nameof(CharacterHeroStats)}] HeroStatsConfig is not assigned on '{name}'.", this);
                return;
            }

            _maxHealth = Mathf.Max(1f, config.maxHealth);
            _damage = Mathf.Max(0f, config.damage);
            _attacksPerSecond = Mathf.Max(0.01f, config.attacksPerSecond);
            _currentHealth = _maxHealth;
            _configured = true;
            HealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        public void ApplyDamage(float amount)
        {
            if (!_configured || !IsAlive || amount <= 0f)
                return;

            _currentHealth = Mathf.Max(0f, _currentHealth - amount);
            HealthChanged?.Invoke(_currentHealth, _maxHealth);
            Debug.Log($"[Hero HP after hit] {_currentHealth:F1} / {_maxHealth} ('{name}')");

            if (_currentHealth <= 0f)
                Die();
        }

        void Die()
        {
            Died?.Invoke(this);
            gameObject.SetActive(false);
        }
    }
}
