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
    /// Уровень и XP в сессии с нуля; в формулы уходит <c>_runtimeLevel</c>, очки за уровень — в тот же пул, что и стартовые очки характеристик.
    /// Очки характеристик: только в рамках текущего Play Mode; сессия до Next, закрепление в <see cref="CommitSkillAllocationSession"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterHeroStats : MonoBehaviour, IDamageable
    {
        [SerializeField] HeroStatsConfig config;

        [Header("Модификаторы базовых статов (баффы / предметы / таланты)")]
        [SerializeField] CharacterStatModifiers statModifiers;

        CharacterStatModifiers _committedSkillBonuses;
        CharacterStatModifiers _sessionSkillBonuses;
        int _availableSkillPoints;
        int _runtimeLevel;
        int _xpIntoCurrentLevel;

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
        CharacterCoreStats _coreStats;

        public HeroStatsConfig Config => config;

        /// <summary>Уровень и базовые характеристики с учётом <see cref="statModifiers"/> (как в <see cref="ApplyDerivedStats"/>).</summary>
        public CharacterCoreStats CoreStats => _coreStats;
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
        public float ManaRegenPerSecond => _manaRegenPerSecond;
        public bool IsAlive => _configured && _currentHealth > 0f;

        public int AvailableSkillPoints => _availableSkillPoints;

        /// <summary>Текущий уровень героя в сессии (с 0).</summary>
        public int DisplayLevel => _runtimeLevel;

        public int CurrentXpIntoLevel => _xpIntoCurrentLevel;

        public int XpThresholdForNextLevel =>
            config != null && config.experienceSteps != null
                ? config.experienceSteps.GetXpToNextLevel(_runtimeLevel)
                : 0;

        /// <summary>Накопительный опыт для UI «Exp»: веха начала текущего уровня + XP в сегменте (см. <see cref="Heroes.Domain.ExperienceSteps"/>).</summary>
        public int CumulativeXpDisplay =>
            config?.experienceSteps != null
                ? config.experienceSteps.SumXpThresholdsFromLevelZeroUpToExclusive(_runtimeLevel) + _xpIntoCurrentLevel
                : _xpIntoCurrentLevel;

        /// <summary>Накопительная веха конца текущего этапа для UI «Exp» (правая часть дроби).</summary>
        public int CumulativeXpNextBoundary
        {
            get
            {
                var steps = config?.experienceSteps;
                if (steps == null)
                    return 0;
                if (steps.HasNextLevel(_runtimeLevel))
                    return steps.SumXpThresholdsFromLevelZeroThroughInclusive(_runtimeLevel);
                var total = steps.SumAllXpThresholds();
                return Mathf.Max(total, CumulativeXpDisplay);
            }
        }

        /// <summary>Заполнение полоски уровня только внутри текущего сегмента (после апa снова от 0).</summary>
        public float LevelProgressFill01
        {
            get
            {
                var t = XpThresholdForNextLevel;
                return t > 0 ? Mathf.Clamp01((float)_xpIntoCurrentLevel / t) : 0f;
            }
        }

        public event Action<float, float> HealthChanged;
        public event Action<float, float> ManaChanged;
        public event Action DerivedStatsChanged;
        public event Action ProgressionChanged;
        public event Action<CharacterHeroStats> Died;

        void Awake()
        {
            ResetProgressionForPlaySession();
            ResetSkillPointsForPlaySession();
            ApplyDerivedStats();
            ProgressionChanged?.Invoke();
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
                _coreStats = default;
                Debug.LogError($"[{nameof(CharacterHeroStats)}] HeroStatsConfig is not assigned on '{name}'.", this);
                return;
            }

            var coreBase = CharacterCoreStats.FromHeroConfig(config, _runtimeLevel);
            var skillMods = CharacterStatModifiers.Combine(_committedSkillBonuses, _sessionSkillBonuses);
            var allMods = CharacterStatModifiers.Combine(statModifiers, skillMods);
            var core = CharacterCoreStats.ApplyModifiers(coreBase, allMods);
            _coreStats = core;

            var weapon = ResolveWeaponStatsFromConfig();

            _maxHealth = CharacterStatFormulas.ComputeMaxHitPoints(core);
            _maxMana = CharacterStatFormulas.ComputeMaxMana(core);
            _manaRegenPerSecond = CharacterStatFormulas.ComputeManaRegenPerSecond(core);
            _attacksPerSecond = CharacterStatFormulas.ComputeAttacksPerSecond(core, weapon, config.attackSpeedScaling);
            _critChancePercent = CharacterStatFormulas.ComputeCritChancePercent(core, weapon);

            _minDamage = CharacterStatFormulas.ComputeMinPhysicalDamage(core, weapon, config.physicalDamagePrimary);
            _maxDamage = CharacterStatFormulas.ComputeMaxPhysicalDamage(core, weapon, config.physicalDamagePrimary);
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
            DerivedStatsChanged?.Invoke();
        }

        void ResetProgressionForPlaySession()
        {
            _runtimeLevel = 0;
            _xpIntoCurrentLevel = 0;
        }

        void ResetSkillPointsForPlaySession()
        {
            _availableSkillPoints = 0;
            _committedSkillBonuses = CharacterStatModifiers.None;
            _sessionSkillBonuses = CharacterStatModifiers.None;
        }

        /// <summary>Добавить опыт; за один вызов возможен не более чем один переход на следующий уровень.</summary>
        public void AddExperience(int amount)
        {
            if (amount <= 0 || config == null)
                return;

            var steps = config.experienceSteps;
            if (steps == null || !steps.HasNextLevel(_runtimeLevel))
                return;

            _xpIntoCurrentLevel += amount;
            var threshold = steps.GetXpToNextLevel(_runtimeLevel);
            var leveled = false;
            if (threshold > 0 && _xpIntoCurrentLevel >= threshold)
            {
                _xpIntoCurrentLevel -= threshold;
                _runtimeLevel++;
                _availableSkillPoints += Mathf.Max(0, steps.attributePointsGrantedPerLevelUp);
                leveled = true;
            }

            if (leveled)
            {
                ApplyDerivedStats();
                RestoreHpAndManaToFullAndNotify();
            }

            ProgressionChanged?.Invoke();
        }

        static int GetStat(in CharacterStatModifiers m, HeroStatKind stat) =>
            stat switch
            {
                HeroStatKind.Strength => m.StrengthBonus,
                HeroStatKind.Dexterity => m.DexterityBonus,
                HeroStatKind.Stamina => m.StaminaBonus,
                HeroStatKind.Intelligence => m.IntelligenceBonus,
                HeroStatKind.Willpower => m.WillpowerBonus,
                HeroStatKind.Luck => m.LuckBonus,
                _ => 0,
            };

        static void AddStat(ref CharacterStatModifiers m, HeroStatKind stat, int delta)
        {
            switch (stat)
            {
                case HeroStatKind.Strength:
                    m.StrengthBonus = Mathf.Max(0, m.StrengthBonus + delta);
                    break;
                case HeroStatKind.Dexterity:
                    m.DexterityBonus = Mathf.Max(0, m.DexterityBonus + delta);
                    break;
                case HeroStatKind.Stamina:
                    m.StaminaBonus = Mathf.Max(0, m.StaminaBonus + delta);
                    break;
                case HeroStatKind.Intelligence:
                    m.IntelligenceBonus = Mathf.Max(0, m.IntelligenceBonus + delta);
                    break;
                case HeroStatKind.Willpower:
                    m.WillpowerBonus = Mathf.Max(0, m.WillpowerBonus + delta);
                    break;
                case HeroStatKind.Luck:
                    m.LuckBonus = Mathf.Max(0, m.LuckBonus + delta);
                    break;
            }
        }

        public int GetSessionSkillBonus(HeroStatKind stat) => GetStat(in _sessionSkillBonuses, stat);

        public bool CanRefundSessionSkillPoint(HeroStatKind stat) =>
            config != null && GetSessionSkillBonus(stat) > 0;

        public bool TrySpendSkillPoint(HeroStatKind stat)
        {
            if (config == null || _availableSkillPoints <= 0)
                return false;

            _availableSkillPoints--;
            AddStat(ref _sessionSkillBonuses, stat, 1);
            ApplyDerivedStats();
            return true;
        }

        public bool TryRefundSessionSkillPoint(HeroStatKind stat)
        {
            if (config == null)
                return false;
            if (GetStat(in _sessionSkillBonuses, stat) <= 0)
                return false;

            AddStat(ref _sessionSkillBonuses, stat, -1);
            _availableSkillPoints++;
            ApplyDerivedStats();
            return true;
        }

        /// <summary>Закрепить сессионные вложения в характеристики (закрытие окна персонажей через Next).</summary>
        public void CommitSkillAllocationSession()
        {
            if (config == null)
                return;

            var hadConfig = _configured;
            var prevMaxHp = _maxHealth;
            var prevMaxMana = _maxMana;

            _committedSkillBonuses = CharacterStatModifiers.Combine(_committedSkillBonuses, _sessionSkillBonuses);
            _sessionSkillBonuses = CharacterStatModifiers.None;
            ApplyDerivedStats();

            if (!hadConfig)
                return;

            const float eps = 1e-3f;
            var hpMaxUp = _maxHealth > prevMaxHp + eps;
            var manaMaxUp = _maxMana > prevMaxMana + eps;
            if (!hpMaxUp && !manaMaxUp)
                return;

            if (hpMaxUp)
                _currentHealth = _maxHealth;
            if (manaMaxUp)
                _currentMana = _maxMana;

            if (hpMaxUp)
                HealthChanged?.Invoke(_currentHealth, _maxHealth);
            if (manaMaxUp)
                ManaChanged?.Invoke(_currentMana, _maxMana);
        }

        void RestoreHpAndManaToFullAndNotify()
        {
            if (!_configured)
                return;

            _currentHealth = _maxHealth;
            _currentMana = _maxMana;
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
