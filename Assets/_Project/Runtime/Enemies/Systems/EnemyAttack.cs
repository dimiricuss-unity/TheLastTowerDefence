using UnityEngine;
using TheLastTowerDefence.Common.Combat;
using TheLastTowerDefence.CoreLoop.Battle;
using TheLastTowerDefence.Enemies.Domain;
using TheLastTowerDefence.Heroes.Systems;

namespace TheLastTowerDefence.Enemies.Systems
{
    /// <summary>
    /// Ближний бой: триггер врага пересекается с коллайдером башни и/или героя.
    /// Если одновременно башня и герой — урон наносится герою. Движение останавливается в ближнем бою с любой из целей.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyAttack : MonoBehaviour
    {
        [SerializeField] string towerCollisionTag = "Tower";
        [SerializeField] string attackTrigger = "Attack";
        [SerializeField] bool useAttackAnimation;

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
            }
        }

        void RegisterHeroMeleeIfAny(Collider2D other)
        {
            var hero = other.GetComponentInParent<CharacterHeroStats>();
            if (hero == null)
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
                return;

            if (Time.time < _nextAttackTime)
                return;

            _nextAttackTime = Time.time + _cooldown;

            if (useAttackAnimation && _animator != null && !string.IsNullOrEmpty(attackTrigger))
            {
                _awaitingAnimationHit = true;
                _animator.SetTrigger(attackTrigger);
                return;
            }

            ApplyDamageNow();
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
        }

        /// <summary>
        /// Вызывается из Animation Event (relay на объекте с Animator).
        /// </summary>
        public void OnAttackAnimationHit()
        {
            if (!_configured || !IsEngagingInMelee || _damage <= 0f)
                return;
            if (!_awaitingAnimationHit)
                return;

            _awaitingAnimationHit = false;
            ApplyDamageNow();
        }
    }
}
