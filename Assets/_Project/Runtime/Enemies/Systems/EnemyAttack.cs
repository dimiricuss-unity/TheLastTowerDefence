using UnityEngine;
using TheLastTowerDefence.Common.Combat;
using TheLastTowerDefence.CoreLoop.Battle;
using TheLastTowerDefence.Enemies.Domain;

namespace TheLastTowerDefence.Enemies.Systems
{
    /// <summary>
    /// Атака по башне, пока триггер врага (дочерний коллайдер, см. префаб <c>Trigger</c>) пересекается с коллайдером башни.
    /// Колбэки 2D приходят на корень с <see cref="Rigidbody2D"/> даже если триггер на дочернем объекте.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyAttack : MonoBehaviour
    {
        [SerializeField] string towerCollisionTag = "Tower";
        [SerializeField] string attackTrigger = "Attack";
        [SerializeField] bool useAttackAnimation;

        Transform _target;
        Animator _animator;
        IDamageable _damageable;
        float _damage;
        float _cooldown;
        float _nextAttackTime;
        bool _configured;
        bool _inMeleeContact;

        /// <summary>Триггер «досягаемость башни» активен — движение должно остановиться.</summary>
        public bool IsEngagingTower => _configured && _inMeleeContact;

        public void Configure(EnemyStatsConfig config, Transform combatTarget)
        {
            if (config == null)
                throw new System.ArgumentNullException(nameof(config));

            _target = combatTarget;
            ResolveDamageable();
            _damage = Mathf.Max(0f, config.damage);
            var aps = Mathf.Max(0.01f, config.attacksPerSecond);
            _cooldown = 1f / aps;
            _nextAttackTime = Time.time;
            _configured = true;
            _inMeleeContact = false;
        }

        void ResolveDamageable()
        {
            _damageable = null;
            if (_target == null)
                return;
            if (!_target.TryGetComponent(out _damageable))
                _damageable = _target.GetComponentInChildren<IDamageable>(true);
        }

        void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!_configured || other == null)
                return;
            if (!IsTowerCollider(other))
                return;
            _inMeleeContact = true;
            _nextAttackTime = Time.time;
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (!_configured || other == null)
                return;
            if (!IsTowerCollider(other))
                return;
            _inMeleeContact = false;
        }

        bool IsTowerCollider(Collider2D other)
        {
            if (!string.IsNullOrEmpty(towerCollisionTag) && other.CompareTag(towerCollisionTag))
                return true;
            if (_target == null)
                return false;
            var t = other.transform;
            return t == _target || t.IsChildOf(_target) || _target.IsChildOf(t);
        }

        void Update()
        {
            if (!_configured || _damage <= 0f)
                return;
            if (!_inMeleeContact)
                return;

            if (Time.time < _nextAttackTime)
                return;

            if (_damageable != null)
            {
                _damageable.ApplyDamage(_damage);
                if (_damageable is TowerDamageable tower)
                    Debug.Log($"[Tower HP after hit] {tower.CurrentHealth:F1} / {tower.MaxHealth} (enemy '{name}')");
            }

            if (useAttackAnimation && _animator != null && !string.IsNullOrEmpty(attackTrigger))
                _animator.SetTrigger(attackTrigger);

            _nextAttackTime = Time.time + _cooldown;
        }
    }
}
