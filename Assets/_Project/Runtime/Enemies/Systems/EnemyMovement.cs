using System;
using UnityEngine;
using TheLastTowerDefence.Enemies.Domain;

namespace TheLastTowerDefence.Enemies.Systems
{
    /// <summary>
    /// Движение к цели через <see cref="Rigidbody2D.MovePosition"/> и поворот через
    /// <see cref="Rigidbody2D.MoveRotation"/> (шаг симуляции — <see cref="FixedUpdate"/>).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyMovement : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 2f;
        [SerializeField] string speedFloatParameter = "Speed";
        [SerializeField] bool faceMovementDirection = true;
        [Tooltip("Добавка к углу Atan2(dy,dx) в градусах Z, чтобы совпасть с ориентацией спрайта.")]
        [SerializeField] float rotationOffsetDegrees = -90f;

        Rigidbody2D _rb;
        Transform _target;
        Animator _animator;
        EnemyAttack _attack;
        bool _configured;

        public void Configure(EnemyStatsConfig config, Transform combatTarget)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (_rb == null)
                _rb = GetComponent<Rigidbody2D>();

            _target = combatTarget;
            _configured = true;
            SnapFacingTowardTarget();
        }

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();
            _attack = GetComponent<EnemyAttack>();
        }

        void SnapFacingTowardTarget()
        {
            if (_rb == null || !faceMovementDirection || _target == null)
                return;

            var delta = (Vector2)_target.position - _rb.position;
            if (delta.sqrMagnitude <= 1e-8f)
                return;

            var angleDeg = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg + rotationOffsetDegrees;
            _rb.SetRotation(angleDeg);
            _rb.angularVelocity = 0f;
        }

        void FixedUpdate()
        {
            if (!_configured || _target == null)
                return;

            if (_attack != null && _attack.IsEngagingTower)
            {
                _rb.velocity = Vector2.zero;
                if (faceMovementDirection)
                {
                    var toward = (Vector2)_target.position - _rb.position;
                    if (toward.sqrMagnitude > 1e-8f)
                    {
                        var angleDeg = Mathf.Atan2(toward.y, toward.x) * Mathf.Rad2Deg + rotationOffsetDegrees;
                        _rb.MoveRotation(angleDeg);
                    }
                }

                if (_animator != null && !string.IsNullOrEmpty(speedFloatParameter))
                    _animator.SetFloat(speedFloatParameter, 0f);
                return;
            }

            var current = _rb.position;
            var dest = (Vector2)_target.position;
            var next = Vector2.MoveTowards(current, dest, moveSpeed * Time.fixedDeltaTime);
            var delta = next - current;

            _rb.MovePosition(next);

            if (faceMovementDirection && delta.sqrMagnitude > 1e-8f)
            {
                var angleDeg = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg + rotationOffsetDegrees;
                _rb.MoveRotation(angleDeg);
            }

            if (_animator != null && !string.IsNullOrEmpty(speedFloatParameter))
            {
                var speed = delta.magnitude / Mathf.Max(Time.fixedDeltaTime, 1e-5f);
                _animator.SetFloat(speedFloatParameter, speed);
            }
        }
    }
}
