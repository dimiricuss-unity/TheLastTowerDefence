using System;
using UnityEngine;
using TheLastTowerDefence.Common.Combat;
using TheLastTowerDefence.Heroes.Domain;

namespace TheLastTowerDefence.Heroes.Systems
{
    /// <summary>
    /// Applies <see cref="HeroStatsConfig"/>; HP via <see cref="IDamageable"/>; мана и реген из конфига.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterHeroStats : MonoBehaviour, IDamageable
    {
        [SerializeField] HeroStatsConfig config;

        float _maxHealth;
        float _currentHealth;
        float _damage;
        float _attacksPerSecond;
        float _maxMana;
        float _currentMana;
        float _manaRegenPerSecond;
        bool _configured;

        public HeroStatsConfig Config => config;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
        public float Damage => _damage;
        public float AttacksPerSecond => _attacksPerSecond;
        public float CurrentMana => _currentMana;
        public float MaxMana => _maxMana;
        public bool IsAlive => _configured && _currentHealth > 0f;

        public event Action<float, float> HealthChanged;
        public event Action<float, float> ManaChanged;
        public event Action<CharacterHeroStats> Died;

        void Awake()
        {
            ApplyConfig();
        }

        void Update()
        {
            TickManaRegen();
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

            _maxMana = Mathf.Max(0f, config.mana);
            _currentMana = _maxMana;
            _manaRegenPerSecond = Mathf.Max(0f, config.manaRegenPerSecond);

            _configured = true;
            HealthChanged?.Invoke(_currentHealth, _maxHealth);
            ManaChanged?.Invoke(_currentMana, _maxMana);
        }

        void TickManaRegen()
        {
            if (!_configured || !IsAlive || _maxMana <= 0f || _manaRegenPerSecond <= 0f)
                return;
            if (_currentMana >= _maxMana - 1e-6f)
                return;

            var before = _currentMana;
            _currentMana = Mathf.Min(_maxMana, _currentMana + _manaRegenPerSecond * Time.deltaTime);
            if (!Mathf.Approximately(before, _currentMana))
                ManaChanged?.Invoke(_currentMana, _maxMana);
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

        /// <summary>
        /// Восстанавливает HP, не выше максимума (не больше недостающего до макс от <paramref name="amount"/>).
        /// </summary>
        public void RestoreHealth(float amount)
        {
            if (!_configured || !IsAlive || amount <= 0f)
                return;

            var missing = _maxHealth - _currentHealth;
            if (missing <= 0f)
                return;

            var applied = Mathf.Min(amount, missing);
            _currentHealth += applied;
            HealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        /// <summary>
        /// Списывает ману целиком; при нехватке мана не меняется.
        /// </summary>
        public bool TryConsumeMana(float amount)
        {
            if (!_configured || !IsAlive || amount <= 0f)
                return true;
            if (_currentMana + 1e-6f < amount)
                return false;

            _currentMana -= amount;
            ManaChanged?.Invoke(_currentMana, _maxMana);
            return true;
        }

        void Die()
        {
            Died?.Invoke(this);
            gameObject.SetActive(false);
        }
    }
}
