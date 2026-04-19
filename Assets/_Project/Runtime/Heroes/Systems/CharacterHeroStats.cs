using System;
using UnityEngine;
using TheLastTowerDefence.Common.Combat;
using TheLastTowerDefence.Formulas;
using TheLastTowerDefence.Heroes.Domain;

namespace TheLastTowerDefence.Heroes.Systems
{
    /// <summary>
    /// Применяет <see cref="HeroStatsConfig"/> (в т.ч. <see cref="HeroStatsConfig.weapon"/>); боевые числа из <see cref="CharacterStatFormulas"/>.
    /// Аддитивные модификаторы статов (<see cref="CharacterStatModifiers"/>) в будущем можно менять из инвентаря/баффов и вызывать пересчёт.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterHeroStats : MonoBehaviour, IDamageable
    {
        [SerializeField] HeroStatsConfig config;

        [Header("Модификаторы базовых статов (баффы / предметы / таланты)")]
        [SerializeField] CharacterStatModifiers statModifiers;

        float _maxHealth;
        float _currentHealth;
        float _minDamage;
        float _maxDamage;
        float _attacksPerSecond;
        float _critChancePercent;
        float _criticalDamageMin;
        float _criticalDamageMax;
        float _maxMana;
        float _currentMana;
        float _manaRegenPerSecond;
        bool _configured;

        public HeroStatsConfig Config => config;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
        public float MinDamage => _minDamage;
        public float MaxDamage => _maxDamage;
        public float AttacksPerSecond => _attacksPerSecond;
        public float CritChancePercent => _critChancePercent;
        public float CriticalDamageMin => _criticalDamageMin;
        public float CriticalDamageMax => _criticalDamageMax;
        public float CurrentMana => _currentMana;
        public float MaxMana => _maxMana;
        public bool IsAlive => _configured && _currentHealth > 0f;

        public event Action<float, float> HealthChanged;
        public event Action<float, float> ManaChanged;
        public event Action<CharacterHeroStats> Died;

        void Awake()
        {
            ApplyDerivedStats();
        }

        void Update()
        {
            TickManaRegen();
        }

        /// <summary>
        /// Пересчитывает производные параметры из конфига, оружия и текущих <see cref="statModifiers"/>.
        /// Вызывайте после смены экипировки или баффов (когда появится тот слой).
        /// </summary>
        public void ApplyDerivedStats()
        {
            if (config == null)
            {
                _configured = false;
                Debug.LogError($"[{nameof(CharacterHeroStats)}] HeroStatsConfig is not assigned on '{name}'.", this);
                return;
            }

            var coreBase = CharacterCoreStats.FromHeroConfig(config);
            var core = CharacterCoreStats.ApplyModifiers(coreBase, statModifiers);

            var weapon = ResolveWeaponStatsFromConfig();

            _maxHealth = CharacterStatFormulas.ComputeMaxHitPoints(core);
            _maxMana = CharacterStatFormulas.ComputeMaxMana(core);
            _manaRegenPerSecond = CharacterStatFormulas.ComputeManaRegenPerSecond(core);
            _attacksPerSecond = CharacterStatFormulas.ComputeAttacksPerSecond(core, weapon);
            _critChancePercent = CharacterStatFormulas.ComputeCritChancePercent(core, weapon);

            _minDamage = CharacterStatFormulas.ComputeMinPhysicalDamage(core, weapon);
            _maxDamage = CharacterStatFormulas.ComputeMaxPhysicalDamage(core, weapon);
            if (_minDamage > _maxDamage)
                (_minDamage, _maxDamage) = (_maxDamage, _minDamage);

            _criticalDamageMin = CharacterStatFormulas.ComputeCriticalDamageMin(_minDamage);
            _criticalDamageMax = CharacterStatFormulas.ComputeCriticalDamageMax(_maxDamage);
            if (_criticalDamageMin > _criticalDamageMax)
                (_criticalDamageMin, _criticalDamageMax) = (_criticalDamageMax, _criticalDamageMin);

            var wasConfigured = _configured;
            _maxHealth = Mathf.Max(1f, _maxHealth);
            _attacksPerSecond = Mathf.Max(0.01f, _attacksPerSecond);

            if (!wasConfigured)
            {
                _currentHealth = _maxHealth;
                _currentMana = _maxMana;
            }
            else
            {
                var hpRatio = _maxHealth > 1e-6f ? _currentHealth / _maxHealth : 0f;
                var manaRatio = _maxMana > 1e-6f ? _currentMana / _maxMana : 0f;
                _currentHealth = Mathf.Clamp(_maxHealth * hpRatio, 0f, _maxHealth);
                _currentMana = Mathf.Clamp(_maxMana * manaRatio, 0f, _maxMana);
            }

            _configured = true;
            HealthChanged?.Invoke(_currentHealth, _maxHealth);
            ManaChanged?.Invoke(_currentMana, _maxMana);
        }

        /// <summary>
        /// Оружие из <see cref="HeroStatsConfig.weapon"/>; при отсутствии или невалидном APS — как в примере таблицы (6/8/0.9/1).
        /// </summary>
        CharacterWeaponStats ResolveWeaponStatsFromConfig()
        {
            if (config.weapon != null)
            {
                var w = config.weapon.ToFormulaWeaponStats();
                if (w.weaponAttacksPerSecond >= 0.01f)
                    return w;
            }

            return CharacterWeaponStats.TableExampleDefaults;
        }

        /// <summary>
        /// Случайный урон удара: сначала бросок крита по <see cref="CritChancePercent"/>,
        /// затем либо диапазон крит-урона, либо обычный min–max (границы включительны, как в таблице).
        /// </summary>
        public float SampleStrikeDamage(out bool isCritical)
        {
            isCritical = false;
            if (!_configured)
                return 0f;

            if (_critChancePercent > 0f &&
                UnityEngine.Random.Range(0f, 100f) < _critChancePercent)
            {
                isCritical = true;
                return UnityEngine.Random.Range(_criticalDamageMin, _criticalDamageMax);
            }

            return UnityEngine.Random.Range(_minDamage, _maxDamage);
        }

        /// <summary>Перегрузка без флага крита (например, для отладки).</summary>
        public float SampleStrikeDamage() => SampleStrikeDamage(out _);

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
        /// <returns>Сколько HP реально добавлено (0, если нечего восстанавливать).</returns>
        public float RestoreHealth(float amount)
        {
            if (!_configured || !IsAlive || amount <= 0f)
                return 0f;

            var missing = _maxHealth - _currentHealth;
            if (missing <= 0f)
                return 0f;

            var applied = Mathf.Min(amount, missing);
            _currentHealth += applied;
            HealthChanged?.Invoke(_currentHealth, _maxHealth);
            return applied;
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
