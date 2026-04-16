using System.Collections.Generic;
using UnityEngine;
using TheLastTowerDefence.Enemies.Systems;

namespace TheLastTowerDefence.Heroes.Systems
{
    /// <summary>
    /// Зона ближнего боя на <c>TriggerForEnemies</c>: в зоне несколько врагов, но бьётся только один,
    /// выбираемый случайно; после смерти цели выбирается другой живой из зоны. Урон и APS из
    /// <see cref="CharacterHeroStats"/>, опционально триггер анимации Attack.
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

        void OnDisable()
        {
            foreach (var e in _enemiesInRange)
            {
                if (e != null)
                    e.Died -= OnEnemyDied;
            }

            _enemiesInRange.Clear();
            _attackTarget = null;
            _damageEventPending = false;
            _pendingDamageVictim = null;
        }

        void Update()
        {
            if (_stats == null || !_stats.IsAlive)
                return;

            if (_enemiesInRange.Count == 0)
            {
                _attackTarget = null;
                return;
            }

            EnsureAttackTarget();
            if (_attackTarget == null || !_attackTarget.IsAlive)
                return;

            var cooldown = 1f / Mathf.Max(0.01f, _stats.AttacksPerSecond);
            if (Time.time < _nextAttackTime)
                return;

            _nextAttackTime = Time.time + cooldown;

            var damage = _stats.Damage;
            if (damage <= 0f)
                return;

            var victim = _attackTarget;
            var useEvent = applyDamageOnAnimationEvent && useAttackAnimation && _animator != null &&
                           !string.IsNullOrEmpty(attackTrigger);

            if (useEvent)
            {
                if (_damageEventPending)
                    TryApplyPendingDamageFromAnimation();

                _damageEventPending = true;
                _pendingDamageVictim = victim;
                _animator.SetTrigger(attackTrigger);
                return;
            }

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

        void ApplyDamageToEnemy(EnemyHealth victim, float damage)
        {
            if (victim == null || !victim.IsAlive)
                return;

            victim.ApplyDamage(damage);
            Debug.Log($"[Enemy HP after hero hit] {victim.CurrentHealth:F1} / {victim.MaxHealth} (enemy '{victim.name}')");
        }

        void LateUpdate()
        {
            if (!faceTarget || _stats == null || !_stats.IsAlive)
                return;
            if (_attackTarget == null || !_attackTarget.IsAlive)
                return;

            FaceTowardWorldPosition(_attackTarget.transform.position);
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

            if (!_enemiesInRange.Add(health))
                return;

            health.Died += OnEnemyDied;
            _nextAttackTime = Time.time;
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
        }

        void OnEnemyDied(EnemyHealth health)
        {
            if (health == null)
                return;
            health.Died -= OnEnemyDied;
            _enemiesInRange.Remove(health);
            if (health == _attackTarget)
                _attackTarget = null;
        }
    }
}
