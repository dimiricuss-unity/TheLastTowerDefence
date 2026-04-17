using UnityEngine;
using TheLastTowerDefence.Common.Combat;
using TheLastTowerDefence.CoreLoop.Battle;
using TheLastTowerDefence.Enemies.Domain;
using TheLastTowerDefence.Heroes.Systems;

namespace TheLastTowerDefence.Enemies.Systems
{
    /// <summary>
    /// Ближний бой: триггер врага пересекается с коллайдером башни и/или героя.
    /// У <c>isMeleeAttacker</c> из SO герой с тегом <c>RangeHero</c> в контакте не считается целью ближнего урона.
    /// Если одновременно башня и герой — урон наносится герою. Движение останавливается в ближнем бою с любой из целей.
    /// При атаке по анимации скорость клипа масштабируется под <c>attacksPerSecond</c> из SO (как у героя).
    /// Интервал между ударами — от момента нанесения урона (Animation Event), а не от старта клипа.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyAttack : MonoBehaviour
    {
        const string RangeHeroTag = "RangeHero";

        [SerializeField] string towerCollisionTag = "Tower";
        [SerializeField] string attackTrigger = "Attack";
        [SerializeField] bool useAttackAnimation;
        [Tooltip("Длительность клипа атаки при Animator.speed = 1. Масштабирование speed под APS из SO: speed = base / cooldown.")]
        [SerializeField] float baseAttackAnimationDurationSeconds = 1f;

        Transform _towerRoot;
        IDamageable _towerDamageable;
        Animator _animator;
        float _damage;
        float _cooldown;
        float _nextAttackTime;
        bool _configured;
        bool _inMeleeWithTower;
        bool _inMeleeWithHero;
        CharacterHeroStats _heroInMelee;
        bool _awaitingAnimationHit;
        bool _isMeleeAttacker;

        float _attackAnimatorOriginalSpeed = 1f;
        bool _attackAnimatorSpeedOverridden;

        /// <summary>
        /// Контакт ближнего боя с башней или с <b>живым</b> героем.
        /// После смерти героя флаг контакта с ним может не сброситься через Exit-триггер — очищаем в <see cref="Update"/>.
        /// </summary>
        public bool IsEngagingInMelee =>
            _configured &&
            (_inMeleeWithTower || (_inMeleeWithHero && _heroInMelee != null && _heroInMelee.IsAlive));

        public void Configure(EnemyStatsConfig config, Transform towerRoot)
        {
            if (config == null)
                throw new System.ArgumentNullException(nameof(config));

            _towerRoot = towerRoot;
            ResolveTowerDamageable();
            _damage = Mathf.Max(0f, config.damage);
            var aps = Mathf.Max(0.01f, config.attacksPerSecond);
            _cooldown = 1f / aps;
            _isMeleeAttacker = config.isMeleeAttacker;
            _nextAttackTime = Time.time;
            _configured = true;
            _inMeleeWithTower = false;
            _inMeleeWithHero = false;
            _heroInMelee = null;
        }

        void ResolveTowerDamageable()
        {
            _towerDamageable = null;
            if (_towerRoot == null)
                return;
            if (!_towerRoot.TryGetComponent(out _towerDamageable))
                _towerDamageable = _towerRoot.GetComponentInChildren<IDamageable>(true);
        }

        void Awake()
        {
            _animator = GetComponentInChildren<Animator>(true);
        }

        void OnDisable()
        {
            EndAttackAnimation();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!_configured || other == null)
                return;

            RegisterHeroMeleeIfAny(other);

            if (IsTowerCollider(other))
            {
                _inMeleeWithTower = true;
                _nextAttackTime = Time.time;
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (!_configured || other == null)
                return;

            UnregisterHeroMeleeIfAny(other);

            if (IsTowerCollider(other))
            {
                _inMeleeWithTower = false;
                _awaitingAnimationHit = false;
                EndAttackAnimation();
            }
        }

        void RegisterHeroMeleeIfAny(Collider2D other)
        {
            var hero = other.GetComponentInParent<CharacterHeroStats>();
            if (hero == null)
                return;

            if (_isMeleeAttacker && hero.CompareTag(RangeHeroTag))
                return;

            _inMeleeWithHero = true;
            _heroInMelee = hero;
            _nextAttackTime = Time.time;
        }

        void UnregisterHeroMeleeIfAny(Collider2D other)
        {
            var hero = other.GetComponentInParent<CharacterHeroStats>();
            if (hero == null || _heroInMelee != hero)
                return;

            _inMeleeWithHero = false;
            _heroInMelee = null;
            _awaitingAnimationHit = false;
            EndAttackAnimation();
        }

        bool IsTowerCollider(Collider2D other)
        {
            if (!string.IsNullOrEmpty(towerCollisionTag) && other.CompareTag(towerCollisionTag))
                return true;
            if (_towerRoot == null)
                return false;
            var t = other.transform;
            return t == _towerRoot || t.IsChildOf(_towerRoot) || _towerRoot.IsChildOf(t);
        }

        void Update()
        {
            if (!_configured || _damage <= 0f)
                return;

            ClearStaleHeroMeleeState();

            if (!IsEngagingInMelee)
            {
                if (_awaitingAnimationHit)
                {
                    _awaitingAnimationHit = false;
                    EndAttackAnimation();
                }

                return;
            }

            if (_awaitingAnimationHit)
                return;

            if (Time.time < _nextAttackTime)
                return;

            if (useAttackAnimation && _animator != null && !string.IsNullOrEmpty(attackTrigger))
            {
                BeginAttackAnimation();
                _awaitingAnimationHit = true;
                _animator.SetTrigger(attackTrigger);
                return;
            }

            ApplyDamageNow();
            _nextAttackTime = Time.time + _cooldown;
        }

        void ApplyDamageNow()
        {
            var target = ResolveDamageTarget();
            if (target == null)
                return;

            target.ApplyDamage(_damage);
            if (target is TowerDamageable tower)
                Debug.Log($"[Tower HP after hit] {tower.CurrentHealth:F1} / {tower.MaxHealth} (enemy '{name}')");
        }

        IDamageable ResolveDamageTarget()
        {
            if (_inMeleeWithHero && _heroInMelee != null && _heroInMelee.IsAlive)
                return _heroInMelee;
            if (_inMeleeWithTower && _towerDamageable != null)
                return _towerDamageable;
            return null;
        }

        void ClearStaleHeroMeleeState()
        {
            if (!_inMeleeWithHero)
                return;
            if (_heroInMelee != null && _heroInMelee.IsAlive)
                return;

            _inMeleeWithHero = false;
            _heroInMelee = null;
            _awaitingAnimationHit = false;
            EndAttackAnimation();
        }

        /// <summary>
        /// Вызывается из Animation Event (relay на объекте с Animator).
        /// </summary>
        public void OnAttackAnimationHit()
        {
            if (!_awaitingAnimationHit)
                return;

            if (!_configured || !IsEngagingInMelee || _damage <= 0f)
            {
                _awaitingAnimationHit = false;
                EndAttackAnimation();
                return;
            }

            _awaitingAnimationHit = false;
            ApplyDamageNow();
            _nextAttackTime = Time.time + _cooldown;
            EndAttackAnimation();
        }

        void BeginAttackAnimation()
        {
            if (_animator == null)
                return;

            if (!_attackAnimatorSpeedOverridden)
                _attackAnimatorOriginalSpeed = _animator.speed;

            var baseDuration = Mathf.Max(0.01f, baseAttackAnimationDurationSeconds);
            var desiredCooldown = Mathf.Max(0.01f, _cooldown);
            _animator.speed = baseDuration / desiredCooldown;
            _attackAnimatorSpeedOverridden = true;
        }

        void EndAttackAnimation()
        {
            if (_animator == null || !_attackAnimatorSpeedOverridden)
                return;

            _animator.speed = _attackAnimatorOriginalSpeed;
            _attackAnimatorSpeedOverridden = false;
        }
    }
}
