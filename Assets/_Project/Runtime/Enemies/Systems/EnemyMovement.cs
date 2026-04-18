using System;
using UnityEngine;
using TheLastTowerDefence.Enemies.Domain;

namespace TheLastTowerDefence.Enemies.Systems
{
    /// <summary>
    /// Движение к цели через <see cref="Rigidbody2D.AddForce"/> и поворот через
    /// <see cref="Rigidbody2D.MoveRotation"/> (шаг симуляции — <see cref="FixedUpdate"/>).
    /// Опционально после спавна можно задать промежуточную точку (<see cref="SetInitialApproachTarget"/>), пока не достигнута — идём к ней, затем к башне / герою как обычно.
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
        [Tooltip("Насколько быстро velocity врага стремится к moveSpeed.")]
        [SerializeField] float acceleration = 16f;
        [Tooltip("Ограничение силы steering-импульса за шаг физики.")]
        [SerializeField] float maxSteeringForce = 24f;
        [Tooltip("Радиус достижения промежуточной точки после спавна (world space, 2D).")]
        [SerializeField, Min(0.01f)] float approachArrivalRadius = 0.22f;

        Rigidbody2D _rb;
        Transform _target;
        Transform _towerTransform;
        Transform _approachWaypoint;
        bool _approachCompleted = true;
        Transform _focusHero;
        Animator _animator;
        EnemyAttack _attack;
        bool _configured;
        bool _warnedBodyTypeFallback;

        public void Configure(EnemyStatsConfig config, Transform combatTarget)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (_rb == null)
                _rb = GetComponent<Rigidbody2D>();

            _towerTransform = combatTarget;
            _approachWaypoint = null;
            _approachCompleted = true;
            _focusHero = null;
            RecalculateChaseTarget();
            _configured = true;
            SnapFacingTowardTarget();
        }

        /// <summary>
        /// Вызывается с <see cref="EnemySpawnPoint"/> сразу после Instantiate: сначала движение к этой точке, затем прежняя логика (башня / фокус героя).
        /// </summary>
        public void SetInitialApproachTarget(Transform waypoint)
        {
            if (!_configured || waypoint == null || _towerTransform == null)
                return;

            _approachWaypoint = waypoint;
            _approachCompleted = false;
            RecalculateChaseTarget();
            SnapFacingTowardTarget();
        }

        /// <summary>Из <see cref="EnemyHeroFocus"/>: преследовать героя в зоне угрозы.</summary>
        public void SetFocusHeroTarget(Transform heroTransform)
        {
            _focusHero = heroTransform;
            if (_configured)
            {
                RecalculateChaseTarget();
                SnapFacingTowardTarget();
            }
        }

        /// <summary>Из <see cref="EnemyHeroFocus"/>: отпустить героя и вернуться к башне / незавершённому подходу.</summary>
        public void ClearFocusHeroTarget()
        {
            _focusHero = null;
            if (_configured)
            {
                RecalculateChaseTarget();
                SnapFacingTowardTarget();
            }
        }

        void RecalculateChaseTarget()
        {
            if (_focusHero != null)
                _target = _focusHero;
            else if (!_approachCompleted && _approachWaypoint != null)
                _target = _approachWaypoint;
            else
                _target = _towerTransform;
        }

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            // Animator находится на дочернем объекте enemy, поэтому ищем в иерархии.
            _animator = GetComponentInChildren<Animator>(true);
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

            TryCompleteApproachPhase();

            if (_attack != null && _attack.IsEngagingInMelee)
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
            var toTarget = dest - current;
            var desiredDirection = toTarget.sqrMagnitude > 1e-8f ? toTarget.normalized : Vector2.zero;

            Vector2 velocity;
            if (_rb.bodyType == RigidbodyType2D.Dynamic)
            {
                var desiredVelocity = desiredDirection * moveSpeed;
                var steering = desiredVelocity - _rb.velocity;
                var steeringForce = Vector2.ClampMagnitude(steering * acceleration, maxSteeringForce);
                _rb.AddForce(steeringForce, ForceMode2D.Force);

                velocity = _rb.velocity;
                if (velocity.sqrMagnitude > moveSpeed * moveSpeed)
                {
                    velocity = velocity.normalized * moveSpeed;
                    _rb.velocity = velocity;
                }
            }
            else
            {
                if (!_warnedBodyTypeFallback)
                {
                    Debug.LogWarning(
                        $"[{nameof(EnemyMovement)}] '{name}': Rigidbody2D bodyType={_rb.bodyType}. AddForce работает только с Dynamic, используется fallback MovePosition.",
                        this);
                    _warnedBodyTypeFallback = true;
                }

                var next = Vector2.MoveTowards(current, dest, moveSpeed * Time.fixedDeltaTime);
                _rb.MovePosition(next);
                velocity = (next - current) / Mathf.Max(Time.fixedDeltaTime, 1e-5f);
            }

            var delta = velocity * Time.fixedDeltaTime;

            if (faceMovementDirection && delta.sqrMagnitude > 1e-8f)
            {
                var angleDeg = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg + rotationOffsetDegrees;
                _rb.MoveRotation(angleDeg);
            }

            if (_animator != null && !string.IsNullOrEmpty(speedFloatParameter))
            {
                var speed = velocity.magnitude;
                _animator.SetFloat(speedFloatParameter, speed);
            }
        }

        void TryCompleteApproachPhase()
        {
            if (_approachCompleted || _approachWaypoint == null || _focusHero != null)
                return;

            var r = Mathf.Max(0.01f, approachArrivalRadius);
            var rSqr = r * r;
            var toWp = (Vector2)_approachWaypoint.position - _rb.position;
            if (toWp.sqrMagnitude > rSqr)
                return;

            _approachCompleted = true;
            _approachWaypoint = null;
            RecalculateChaseTarget();
            SnapFacingTowardTarget();
        }
    }
}
