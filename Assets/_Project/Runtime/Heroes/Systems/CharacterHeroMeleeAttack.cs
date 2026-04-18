using System.Collections.Generic;
using UnityEngine;
using TheLastTowerDefence.Enemies.Systems;

namespace TheLastTowerDefence.Heroes.Systems
{
    /// <summary>
    /// Зона ближнего боя на <c>TriggerForEnemies</c>: в зоне несколько врагов, но бьётся только один,
    /// выбираемый случайно; после смерти цели выбирается другой живой из зоны. Урон и APS из
    /// <see cref="CharacterHeroStats"/>, опционально триггер анимации Attack.
    /// Кулдаун APS отсчитывается от начала атаки (в момент <c>SetTrigger</c>); урон по Animation Event без сдвига таймера.
    /// Поворот к цели — только в момент начала замаха (перед <c>SetTrigger</c>), не каждый кадр в бою.
    /// Если в зоне больше никого не было (после того как там уже были враги), через <see cref="idleReturnFacingDelaySeconds"/>
    /// восстанавливается ориентация как при старте сцены.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterHeroMeleeAttack : MonoBehaviour
    {
        [SerializeField] bool useAttackAnimation = true;
        [SerializeField] string attackTrigger = "Attack";
        [Tooltip("Урон в кадр события Animation Event (OnAttackDamage) на объекте с Animator. Нужен CharacterHeroMeleeAttackAnimationRelay. Если выключено — урон сразу при срабатывании APS, как раньше.")]
        [SerializeField] bool applyDamageOnAnimationEvent = true;
        [SerializeField] bool faceTarget = true;
        [Tooltip("Сюда повеси объект со спрайтом/PSB (например CharacterSprite). Его ось +X (красная) будет смотреть на врага. Пусто — ищется дочерний CharacterSprite, иначе крутится корень с Rigidbody2D.")]
        [SerializeField] Transform facingPivot;
        [Tooltip("Добавка в градусах к углу поворота вокруг мировой Z.")]
        [SerializeField] float rotationOffsetDegrees;
        [Tooltip("После того как в melee-зоне не осталось врагов, через столько секунд вернуть поворот как при старте сцены.")]
        [SerializeField] float idleReturnFacingDelaySeconds = 1f;
        [Tooltip("Если зона 0→1 снова быстрее этого интервала после выхода последнего врага, не сдвигать _nextAttackTime в «сейчас» — иначе мерцание триггера (поворот/физика) даёт второй удар подряд без APS.")]
        [SerializeField, Min(0f)] float reentryCooldownPullGraceSeconds = 0.12f;

        CharacterHeroStats _stats;
        Animator _animator;
        Transform _facingRoot;
        Rigidbody2D _facingRigidbody;

        readonly HashSet<EnemyHealth> _enemiesInRange = new HashSet<EnemyHealth>();
        readonly List<EnemyHealth> _pickRandomBuffer = new List<EnemyHealth>(16);

        EnemyHealth _attackTarget;
        float _nextAttackTime;

        bool _damageEventPending;
        EnemyHealth _pendingDamageVictim;
        float _pendingSwingStartedTime;
        [Tooltip("Если событие анимации не пришло (битый клип), сбросить ожидание и продолжить APS.")]
        [SerializeField] float animationDamageTimeoutSeconds = 2f;

        bool _usePivotInitialFacing;
        Quaternion _initialFacingPivotLocalRotation;
        Vector3 _initialFacingPivotLocalPosition;

        bool _useRigidbodyInitialFacing;
        float _initialRigidbodyAngleDegrees;

        bool _useRootInitialFacing;
        Quaternion _initialFacingRootWorldRotation;

        bool _hadMeleeEnemyForIdleReturnFacing;
        bool _idleFacingReturnScheduled;
        float _idleFacingReturnAtTime;

        /// <summary>Время, когда в зоне не осталось ни одного врага (для отличия реального «пусто» от краткого выхода из триггера).</summary>
        float _zoneLastBecameEmptyTime = float.NegativeInfinity;

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
        }

        void Start()
        {
            CaptureInitialFacing();
        }

        void OnDisable()
        {
            _idleFacingReturnScheduled = false;
            _hadMeleeEnemyForIdleReturnFacing = false;
            foreach (var e in _enemiesInRange)
            {
                if (e != null)
                    e.Died -= OnEnemyDied;
            }

            _enemiesInRange.Clear();
            _attackTarget = null;
            _damageEventPending = false;
            _pendingDamageVictim = null;
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

            if (_enemiesInRange.Count == 0)
            {
                _attackTarget = null;
                _damageEventPending = false;
                _pendingDamageVictim = null;

                if (faceTarget && _hadMeleeEnemyForIdleReturnFacing && !_idleFacingReturnScheduled)
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

            var damage = _stats.Damage;
            if (damage <= 0f)
                return;

            var victim = _attackTarget;
            var useEvent = applyDamageOnAnimationEvent && useAttackAnimation && _animator != null &&
                           !string.IsNullOrEmpty(attackTrigger);

            if (useEvent)
            {
                if (_damageEventPending)
                    return;

                FaceAttackVictim(victim);
                _damageEventPending = true;
                _pendingDamageVictim = victim;
                _pendingSwingStartedTime = Time.time;
                _nextAttackTime = Time.time + cooldown;
                _animator.SetTrigger(attackTrigger);
                return;
            }

            _nextAttackTime = Time.time + cooldown;

            FaceAttackVictim(victim);
            if (useAttackAnimation && _animator != null && !string.IsNullOrEmpty(attackTrigger))
                _animator.SetTrigger(attackTrigger);

            ApplyDamageToEnemy(victim, damage);
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

            _damageEventPending = false;
            var victim = _pendingDamageVictim;
            _pendingDamageVictim = null;

            if (_stats == null || !_stats.IsAlive)
                return;

            var damage = _stats.Damage;
            if (damage <= 0f || victim == null)
                return;

            if (!victim.IsAlive || !_enemiesInRange.Contains(victim))
                return;

            ApplyDamageToEnemy(victim, damage);
        }

        void CancelPendingSwing(float cooldown)
        {
            _damageEventPending = false;
            _pendingDamageVictim = null;
            _nextAttackTime = Time.time + cooldown;
        }

        void ApplyDamageToEnemy(EnemyHealth victim, float damage)
        {
            if (victim == null || !victim.IsAlive)
                return;

            victim.ApplyDamage(damage);
            Debug.Log($"[Enemy HP after hero hit] {victim.CurrentHealth:F1} / {victim.MaxHealth} (enemy '{victim.name}')");
        }

        void FaceAttackVictim(EnemyHealth victim)
        {
            if (!faceTarget || victim == null || !victim.IsAlive)
                return;
            FaceTowardWorldPosition(GetEnemyWorldCenter(victim));
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
            _hadMeleeEnemyForIdleReturnFacing = false;
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

            // Prefer a collider center so we face the "body" center, not the root/pivot.
            var collider2D = enemy.GetComponentInChildren<Collider2D>();
            if (collider2D != null)
                return collider2D.bounds.center;

            // Fallback: renderer bounds often match the visible sprite.
            var renderer = enemy.GetComponentInChildren<Renderer>();
            if (renderer != null)
                return renderer.bounds.center;

            return enemy.transform.position;
        }

        void EnsureAttackTarget()
        {
            if (_attackTarget != null && _attackTarget.IsAlive && _enemiesInRange.Contains(_attackTarget))
                return;

            PickRandomTarget();
        }

        void PickRandomTarget()
        {
            _pickRandomBuffer.Clear();
            foreach (var e in _enemiesInRange)
            {
                if (e != null && e.IsAlive)
                    _pickRandomBuffer.Add(e);
            }

            if (_pickRandomBuffer.Count == 0)
            {
                _attackTarget = null;
                return;
            }

            _attackTarget = _pickRandomBuffer[Random.Range(0, _pickRandomBuffer.Count)];
        }

        void FaceTowardWorldPosition(Vector3 worldPosition)
        {
            if (!faceTarget || _facingRoot == null)
                return;

            var origin = _facingRigidbody != null ? (Vector2)_facingRigidbody.position : (Vector2)_facingRoot.position;
            var delta = (Vector2)worldPosition - origin;
            if (delta.sqrMagnitude <= 1e-8f)
                return;

            // Мировой угол Z: ось +X объекта после поворота направлена на цель (2D, вид сверху).
            var angleDeg = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg + rotationOffsetDegrees;
            var rotation = Quaternion.Euler(0f, 0f, angleDeg);

            if (facingPivot != null)
            {
                // Пивот спрайта не в центре персонажа: без сдвига меш «катается» вокруг точки.
                // Фиксируем в мире точку на facingPivot, которая совпадает с корнем героя (RB).
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
            _hadMeleeEnemyForIdleReturnFacing = true;
            // Не сдвигать таймер при каждом новом враге: иначе второй вход в зону
            // «подтягивает» следующий удар раньше APS и получаются два удара подряд.
            // Краткий 0→1 после OnTriggerExit (мерцание) не должен сбрасывать кулдаун — см. reentryCooldownPullGraceSeconds.
            if (!_damageEventPending && wasEmpty &&
                Time.time - _zoneLastBecameEmptyTime >= reentryCooldownPullGraceSeconds)
                _nextAttackTime = Mathf.Min(_nextAttackTime, Time.time);

            if (_attackTarget == null || !_attackTarget.IsAlive || !_enemiesInRange.Contains(_attackTarget))
                PickRandomTarget();
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
        /// Доля оставшегося кулдауна до следующего удара (1 = полный интервал APS; 0 = готов).
        /// Таймер стартует с начала атаки (<see cref="_nextAttackTime"/> выставляется вместе с <c>SetTrigger</c>).
        /// </summary>
        public float AttackCooldownRemaining01
        {
            get
            {
                if (_stats == null)
                    return 0f;
                var cd = 1f / Mathf.Max(0.01f, _stats.AttacksPerSecond);
                var rem = Mathf.Max(0f, _nextAttackTime - Time.time);
                return Mathf.Clamp01(rem / cd);
            }
        }
    }
}
