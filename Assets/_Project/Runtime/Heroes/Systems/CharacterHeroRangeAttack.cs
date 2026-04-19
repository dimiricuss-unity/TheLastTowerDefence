using System.Collections.Generic;
using UnityEngine;
using TheLastTowerDefence.Enemies.Systems;
using TheLastTowerDefence.Heroes.Domain;

namespace TheLastTowerDefence.Heroes.Systems
{
    /// <summary>
    /// Дальний бой лучника: триггер <c>TriggerForEnemies</c>, в зоне несколько врагов — бьётся тот,
    /// чей центр дальше всего от позиции героя. APS из <see cref="CharacterHeroStats"/>; урон наносит
    /// <see cref="HeroBoltProjectile"/> при триггере с этой же целью. Опционально анимация + Animation Event (relay).
    /// Вылет стрелы — из <c>ShootPoint</c> (поле <see cref="shootPoint"/> или дочерний объект с таким именем), иначе из <see cref="ArrowFlightVfxConfig.spawnOffsetLocal"/>.
    /// Опционально <see cref="clericMagic"/>: приоритет лечения рыцаря тем же APS (без поворота к цели лечения).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterHeroRangeAttack : MonoBehaviour
    {
        [SerializeField] bool useAttackAnimation = true;
        [SerializeField] string attackTrigger = "Attack";
        [Tooltip("Урон по Animation Event (OnAttackDamage) на объекте с Animator. Нужен CharacterHeroMeleeAttackAnimationRelay. Если выключено — урон сразу по APS.")]
        [SerializeField] bool applyDamageOnAnimationEvent = true;
        [SerializeField] bool faceTarget = true;
        [Tooltip("Объект со спрайтом (например CharacterSprite). Пусто — дочерний CharacterSprite, иначе корень с Rigidbody2D.")]
        [SerializeField] Transform facingPivot;
        [SerializeField] float rotationOffsetDegrees;
        [SerializeField] float baseAttackAnimationDurationSeconds = 1f;
        [SerializeField] float idleReturnFacingDelaySeconds = 1f;
        [Tooltip("Полёт Bolt + след; урон при триггере болта только по выбранной цели.")]
        [SerializeField] ArrowFlightVfxConfig arrowFlightVfx;
        [Tooltip("Точка вылета стрелы (например дочерний ShootPoint на префабе). Пусто — ищется Transform с именем ShootPoint под героем, иначе позиция из spawnOffsetLocal в ArrowFlightVfx.")]
        [SerializeField] Transform shootPoint;
        [Tooltip("См. CharacterHeroMeleeAttack: не сбрасывать кулдаун при кратком 0→1 зоны после мерцания триггера.")]
        [SerializeField, Min(0f)] float reentryCooldownPullGraceSeconds = 0.12f;
        [Tooltip("Для клирика: приоритет лечения рыцаря вместо выстрела по тем же тикам APS.")]
        [SerializeField] CharacterClericMagic clericMagic;

        CharacterHeroStats _stats;
        Animator _animator;
        Transform _facingRoot;
        Rigidbody2D _facingRigidbody;

        readonly HashSet<EnemyHealth> _enemiesInRange = new HashSet<EnemyHealth>();

        EnemyHealth _attackTarget;
        float _nextAttackTime;

        bool _damageEventPending;
        EnemyHealth _pendingDamageVictim;
        float _pendingSwingStartedTime;
        [SerializeField] float animationDamageTimeoutSeconds = 2f;

        float _attackAnimatorOriginalSpeed = 1f;
        bool _attackAnimatorSpeedOverridden;

        bool _usePivotInitialFacing;
        Quaternion _initialFacingPivotLocalRotation;
        Vector3 _initialFacingPivotLocalPosition;

        bool _useRigidbodyInitialFacing;
        float _initialRigidbodyAngleDegrees;

        bool _useRootInitialFacing;
        Quaternion _initialFacingRootWorldRotation;

        bool _hadEnemyInRangeForIdleReturnFacing;
        bool _idleFacingReturnScheduled;
        float _idleFacingReturnAtTime;

        float _zoneLastBecameEmptyTime = float.NegativeInfinity;

        Transform _resolvedShootPoint;

        bool _clericHealSwingPending;

        void Awake()
        {
            _stats = GetComponentInParent<CharacterHeroStats>();
            _facingRoot = _stats != null ? _stats.transform : transform.root;
            _facingRigidbody = _facingRoot.GetComponent<Rigidbody2D>();
            _animator = _facingRoot.GetComponentInChildren<Animator>(true);
            if (facingPivot == null)
            {
                var found = _facingRoot.Find("CharacterSprite");
                if (found != null)
                    facingPivot = found;
            }

            _resolvedShootPoint = shootPoint;
            if (_resolvedShootPoint == null)
                _resolvedShootPoint = FindShootPointInHierarchy(_facingRoot);

            if (clericMagic == null && _stats != null)
                clericMagic = _stats.GetComponentInChildren<CharacterClericMagic>(true);
        }

        static Transform FindShootPointInHierarchy(Transform root)
        {
            if (root == null)
                return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "ShootPoint")
                    return t;
            }

            return null;
        }

        void Start()
        {
            CaptureInitialFacing();
        }

        void OnDisable()
        {
            _idleFacingReturnScheduled = false;
            _hadEnemyInRangeForIdleReturnFacing = false;
            foreach (var e in _enemiesInRange)
            {
                if (e != null)
                    e.Died -= OnEnemyDied;
            }

            _enemiesInRange.Clear();
            _attackTarget = null;
            _damageEventPending = false;
            _pendingDamageVictim = null;
            _clericHealSwingPending = false;
            _zoneLastBecameEmptyTime = float.NegativeInfinity;
        }

        void Update()
        {
            if (_stats == null || !_stats.IsAlive)
                return;

            if (_enemiesInRange.Count > 0)
                _idleFacingReturnScheduled = false;

            TryApplyIdleFacingReturnIfReady();

            var cooldown = 1f / Mathf.Max(0.01f, _stats.AttacksPerSecond);
            if (_damageEventPending && Time.time - _pendingSwingStartedTime > animationDamageTimeoutSeconds)
                CancelPendingSwing(cooldown);

            var healFirst = clericMagic != null && clericMagic.ShouldPrioritizeHealing();
            if (healFirst && _damageEventPending && !_clericHealSwingPending)
                CancelPendingSwing(cooldown);
            if (!healFirst && _clericHealSwingPending)
                CancelPendingSwing(cooldown);

            if (healFirst)
            {
                TryHealingPriorityUpdate(cooldown);
                return;
            }

            if (_enemiesInRange.Count == 0)
            {
                _attackTarget = null;
                _damageEventPending = false;
                _pendingDamageVictim = null;

                if (faceTarget && _hadEnemyInRangeForIdleReturnFacing && !_idleFacingReturnScheduled)
                {
                    _idleFacingReturnScheduled = true;
                    _idleFacingReturnAtTime = Time.time + Mathf.Max(0.01f, idleReturnFacingDelaySeconds);
                }

                return;
            }

            EnsureAttackTarget();
            if (_attackTarget == null || !_attackTarget.IsAlive)
                return;

            if (Time.time < _nextAttackTime)
                return;

            var victim = _attackTarget;
            var useEvent = applyDamageOnAnimationEvent && useAttackAnimation && _animator != null &&
                           !string.IsNullOrEmpty(attackTrigger);

            if (useEvent)
            {
                if (_damageEventPending)
                    return;

                BeginAttackAnimation(cooldown);
                FaceAttackVictim(victim);
                _damageEventPending = true;
                _pendingDamageVictim = victim;
                _pendingSwingStartedTime = Time.time;
                _animator.SetTrigger(attackTrigger);
                return;
            }

            var damage = _stats.SampleStrikeDamage(out var isCritical);
            if (damage <= 0f)
                return;

            _nextAttackTime = Time.time + cooldown;

            FaceAttackVictim(victim);
            if (useAttackAnimation && _animator != null && !string.IsNullOrEmpty(attackTrigger))
                _animator.SetTrigger(attackTrigger);

            TrySpawnBolt(victim, damage, isCritical);
        }

        void TryHealingPriorityUpdate(float cooldown)
        {
            if (Time.time < _nextAttackTime)
                return;

            var useEvent = applyDamageOnAnimationEvent && useAttackAnimation && _animator != null &&
                           !string.IsNullOrEmpty(attackTrigger);

            if (useEvent)
            {
                if (_damageEventPending)
                    return;

                BeginAttackAnimation(cooldown);
                _clericHealSwingPending = true;
                _damageEventPending = true;
                _pendingDamageVictim = null;
                _pendingSwingStartedTime = Time.time;
                _animator.SetTrigger(attackTrigger);
                return;
            }

            if (!clericMagic.TryPerformHeal())
                return;

            _nextAttackTime = Time.time + cooldown;
            if (useAttackAnimation && _animator != null && !string.IsNullOrEmpty(attackTrigger))
                _animator.SetTrigger(attackTrigger);
        }

        /// <summary>Вызывается из Animation Event (через <see cref="CharacterHeroMeleeAttackAnimationRelay"/>).</summary>
        public void OnAttackDamageAnimationEvent()
        {
            TryApplyPendingDamageFromAnimation();
        }

        void TryApplyPendingDamageFromAnimation()
        {
            if (!_damageEventPending)
                return;

            if (_clericHealSwingPending)
            {
                _clericHealSwingPending = false;
                _damageEventPending = false;
                _pendingDamageVictim = null;

                var healCooldown = _stats != null ? 1f / Mathf.Max(0.01f, _stats.AttacksPerSecond) : 0.1f;
                EndAttackAnimation();

                if (_stats == null || !_stats.IsAlive)
                {
                    _nextAttackTime = Time.time + healCooldown;
                    return;
                }

                if (clericMagic == null || !clericMagic.TryPerformHeal())
                {
                    _nextAttackTime = Time.time + healCooldown;
                    return;
                }

                _nextAttackTime = Time.time + healCooldown;
                return;
            }

            _damageEventPending = false;
            var victim = _pendingDamageVictim;
            _pendingDamageVictim = null;

            var cooldown = _stats != null ? 1f / Mathf.Max(0.01f, _stats.AttacksPerSecond) : 0.1f;
            EndAttackAnimation();

            if (_stats == null || !_stats.IsAlive)
                return;

            var damage = _stats.SampleStrikeDamage(out var isCritical);
            if (damage <= 0f || victim == null)
            {
                _nextAttackTime = Time.time + cooldown;
                return;
            }

            if (!victim.IsAlive || !_enemiesInRange.Contains(victim))
            {
                _nextAttackTime = Time.time + cooldown;
                return;
            }

            TrySpawnBolt(victim, damage, isCritical);
            _nextAttackTime = Time.time + cooldown;
        }

        void CancelPendingSwing(float cooldown)
        {
            _clericHealSwingPending = false;
            _damageEventPending = false;
            _pendingDamageVictim = null;
            EndAttackAnimation();
            _nextAttackTime = Time.time + cooldown;
        }

        void TrySpawnBolt(EnemyHealth victim, float damage, bool isCritical = false)
        {
            if (arrowFlightVfx == null || victim == null || !victim.IsAlive || damage <= 0f)
                return;

            var from = GetBoltSpawnWorldPosition();
            var to = GetEnemyWorldCenter(victim);
            HeroBoltProjectile.Spawn(arrowFlightVfx, from, to, victim, damage, isCritical);
        }

        Vector3 GetBoltSpawnWorldPosition()
        {
            if (_resolvedShootPoint != null)
                return _resolvedShootPoint.position;

            var offset = arrowFlightVfx != null ? arrowFlightVfx.spawnOffsetLocal : Vector2.zero;
            return _facingRoot != null
                ? _facingRoot.TransformPoint(new Vector3(offset.x, offset.y, 0f))
                : transform.position;
        }

        void FaceAttackVictim(EnemyHealth victim)
        {
            if (!faceTarget || victim == null || !victim.IsAlive)
                return;
            FaceTowardWorldPosition(GetEnemyWorldCenter(victim));
        }

        void BeginAttackAnimation(float cooldown)
        {
            if (_animator == null)
                return;

            if (!_attackAnimatorSpeedOverridden)
                _attackAnimatorOriginalSpeed = _animator.speed;

            var baseDuration = Mathf.Max(0.01f, baseAttackAnimationDurationSeconds);
            var desiredCooldown = Mathf.Max(0.01f, cooldown);
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

        void CaptureInitialFacing()
        {
            if (facingPivot != null)
            {
                _usePivotInitialFacing = true;
                _initialFacingPivotLocalRotation = facingPivot.localRotation;
                _initialFacingPivotLocalPosition = facingPivot.localPosition;
                return;
            }

            if (_facingRigidbody != null)
            {
                _useRigidbodyInitialFacing = true;
                _initialRigidbodyAngleDegrees = _facingRigidbody.rotation;
                return;
            }

            if (_facingRoot != null)
            {
                _useRootInitialFacing = true;
                _initialFacingRootWorldRotation = _facingRoot.rotation;
            }
        }

        void TryApplyIdleFacingReturnIfReady()
        {
            if (!faceTarget || !_idleFacingReturnScheduled || _enemiesInRange.Count > 0)
                return;
            if (Time.time < _idleFacingReturnAtTime)
                return;

            RestoreInitialFacing();
            _idleFacingReturnScheduled = false;
            _hadEnemyInRangeForIdleReturnFacing = false;
        }

        void RestoreInitialFacing()
        {
            if (!faceTarget || _facingRoot == null)
                return;

            if (_usePivotInitialFacing && facingPivot != null)
            {
                facingPivot.localRotation = _initialFacingPivotLocalRotation;
                facingPivot.localPosition = _initialFacingPivotLocalPosition;
                if (_facingRigidbody != null)
                    _facingRigidbody.angularVelocity = 0f;
                return;
            }

            if (_useRigidbodyInitialFacing && _facingRigidbody != null)
            {
                _facingRigidbody.rotation = _initialRigidbodyAngleDegrees;
                _facingRigidbody.angularVelocity = 0f;
                return;
            }

            if (_useRootInitialFacing)
                _facingRoot.rotation = _initialFacingRootWorldRotation;
        }

        static Vector3 GetEnemyWorldCenter(EnemyHealth enemy)
        {
            if (enemy == null)
                return default;

            var collider2D = enemy.GetComponentInChildren<Collider2D>();
            if (collider2D != null)
                return collider2D.bounds.center;

            var renderer = enemy.GetComponentInChildren<Renderer>();
            if (renderer != null)
                return renderer.bounds.center;

            return enemy.transform.position;
        }

        void EnsureAttackTarget()
        {
            PickFarthestTarget();
        }

        void PickFarthestTarget()
        {
            if (_facingRoot == null)
            {
                _attackTarget = null;
                return;
            }

            var origin = _facingRigidbody != null ? (Vector2)_facingRigidbody.position : (Vector2)_facingRoot.position;
            EnemyHealth best = null;
            var bestSqr = -1f;
            foreach (var e in _enemiesInRange)
            {
                if (e == null || !e.IsAlive)
                    continue;

                var center = GetEnemyWorldCenter(e);
                var sqr = ((Vector2)center - origin).sqrMagnitude;
                if (best == null || sqr > bestSqr)
                {
                    bestSqr = sqr;
                    best = e;
                }
                else if (Mathf.Approximately(sqr, bestSqr) && e.GetInstanceID() < best.GetInstanceID())
                    best = e;
            }

            _attackTarget = best;
        }

        void FaceTowardWorldPosition(Vector3 worldPosition)
        {
            if (!faceTarget || _facingRoot == null)
                return;

            var origin = _facingRigidbody != null ? (Vector2)_facingRigidbody.position : (Vector2)_facingRoot.position;
            var delta = (Vector2)worldPosition - origin;
            if (delta.sqrMagnitude <= 1e-8f)
                return;

            var angleDeg = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg + rotationOffsetDegrees;
            var rotation = Quaternion.Euler(0f, 0f, angleDeg);

            if (facingPivot != null)
            {
                if (facingPivot.IsChildOf(_facingRoot))
                {
                    var retainLocal = facingPivot.InverseTransformPoint(_facingRoot.position);
                    var worldBefore = facingPivot.TransformPoint(retainLocal);
                    facingPivot.rotation = rotation;
                    var worldAfter = facingPivot.TransformPoint(retainLocal);
                    facingPivot.position += worldBefore - worldAfter;
                }
                else
                    facingPivot.rotation = rotation;

                return;
            }

            if (_facingRigidbody != null)
            {
                _facingRigidbody.rotation = angleDeg;
                _facingRigidbody.angularVelocity = 0f;
            }
            else
                _facingRoot.rotation = rotation;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            TryAddEnemy(other);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            TryRemoveEnemy(other);
        }

        void TryAddEnemy(Collider2D other)
        {
            if (other == null)
                return;

            var health = other.attachedRigidbody != null
                ? other.attachedRigidbody.GetComponent<EnemyHealth>()
                : null;
            if (health == null)
                health = other.GetComponentInParent<EnemyHealth>();
            if (health == null || !health.IsAlive)
                return;

            var wasEmpty = _enemiesInRange.Count == 0;
            if (!_enemiesInRange.Add(health))
                return;

            health.Died += OnEnemyDied;
            _hadEnemyInRangeForIdleReturnFacing = true;
            if (!_damageEventPending && wasEmpty &&
                Time.time - _zoneLastBecameEmptyTime >= reentryCooldownPullGraceSeconds)
                _nextAttackTime = Mathf.Min(_nextAttackTime, Time.time);

            if (_attackTarget == null || !_attackTarget.IsAlive || !_enemiesInRange.Contains(_attackTarget))
                EnsureAttackTarget();
        }

        void TryRemoveEnemy(Collider2D other)
        {
            if (other == null)
                return;

            var health = other.attachedRigidbody != null
                ? other.attachedRigidbody.GetComponent<EnemyHealth>()
                : null;
            if (health == null)
                health = other.GetComponentInParent<EnemyHealth>();
            if (health == null)
                return;

            if (health == _attackTarget)
                _attackTarget = null;

            RemoveEnemy(health);
        }

        void RemoveEnemy(EnemyHealth health)
        {
            if (health == null)
                return;
            health.Died -= OnEnemyDied;
            _enemiesInRange.Remove(health);
            if (_enemiesInRange.Count == 0)
                _zoneLastBecameEmptyTime = Time.time;
        }

        void OnEnemyDied(EnemyHealth health)
        {
            if (health == null)
                return;
            health.Died -= OnEnemyDied;
            _enemiesInRange.Remove(health);
            if (_enemiesInRange.Count == 0)
                _zoneLastBecameEmptyTime = Time.time;
            if (health == _attackTarget)
                _attackTarget = null;
        }

        /// <summary>
        /// Доля оставшегося кулдауна до следующего выстрела (1 = полный интервал APS; 0 = готов).
        /// Пока ждём Animation Event, для UI считаем полный кулдаун.
        /// </summary>
        public float AttackCooldownRemaining01
        {
            get
            {
                if (_stats == null)
                    return 0f;
                var cd = 1f / Mathf.Max(0.01f, _stats.AttacksPerSecond);
                if (_damageEventPending)
                    return 1f;
                var rem = Mathf.Max(0f, _nextAttackTime - Time.time);
                return Mathf.Clamp01(rem / cd);
            }
        }
    }
}
