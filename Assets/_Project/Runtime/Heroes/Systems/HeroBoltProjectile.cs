using UnityEngine;
using TheLastTowerDefence.Enemies.Systems;
using TheLastTowerDefence.Heroes.Domain;

namespace TheLastTowerDefence.Heroes.Systems
{
    /// <summary>
    /// Инстанс Bolt: прямой полёт по направлению к точке прицела, урон один раз при триггере с <b>заданной</b> целью,
    /// затем короткий пролёт «в плоть» (коллайдер болта отключён, повторного урона нет).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeroBoltProjectile : MonoBehaviour
    {
        static Material _sharedTrailMaterial;

        ArrowFlightVfxConfig _cfg;
        EnemyHealth _targetEnemy;
        float _damage;
        Vector2 _dir;
        float _maxTravelDist;
        float _traveled;
        bool _damageApplied;
        float _embedRemaining;
        bool _destroyScheduled;

        Rigidbody2D _rb;
        TrailRenderer _trail;
        SpriteRenderer _sprite;
        Collider2D _boltCollider;

        public static void Spawn(
            ArrowFlightVfxConfig config,
            Vector3 worldFrom,
            Vector3 worldTo,
            EnemyHealth targetEnemy,
            float damage)
        {
            if (config == null || config.boltPrefab == null || targetEnemy == null || !targetEnemy.IsAlive || damage <= 0f)
                return;

            var go = Instantiate(config.boltPrefab, worldFrom, Quaternion.identity);
            var proj = go.GetComponent<HeroBoltProjectile>();
            if (proj == null)
                proj = go.AddComponent<HeroBoltProjectile>();
            proj.Init(config, worldFrom, worldTo, targetEnemy, damage);
        }

        void Init(
            ArrowFlightVfxConfig cfg,
            Vector3 from,
            Vector3 to,
            EnemyHealth targetEnemy,
            float damage)
        {
            _cfg = cfg;
            _targetEnemy = targetEnemy;
            _damage = damage;
            _traveled = 0f;
            _damageApplied = false;
            _embedRemaining = 0f;
            _destroyScheduled = false;

            var delta = (Vector2)to - (Vector2)from;
            _dir = delta.sqrMagnitude > 1e-8f ? delta.normalized : Vector2.right;
            var initialDist = delta.magnitude;
            _maxTravelDist = Mathf.Max(
                initialDist * Mathf.Max(1f, cfg.maxTravelDistanceFactor),
                initialDist + 0.25f);

            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null)
                _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.gravityScale = 0f;
            _rb.simulated = true;
            _rb.useFullKinematicContacts = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.position = from;
            _rb.rotation = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg + cfg.boltRotationOffsetDegrees;

            _sprite = GetComponentInChildren<SpriteRenderer>(true);
            _boltCollider = GetComponent<Collider2D>();
            if (_boltCollider == null)
                _boltCollider = GetComponentInChildren<Collider2D>(true);

            _trail = GetComponent<TrailRenderer>();
            if (_trail == null)
                _trail = gameObject.AddComponent<TrailRenderer>();

            ConfigureTrail(_trail, cfg, _sprite);
        }

        static void ConfigureTrail(TrailRenderer trail, ArrowFlightVfxConfig cfg, SpriteRenderer sprite)
        {
            trail.alignment = LineAlignment.TransformZ;
            trail.textureMode = LineTextureMode.Stretch;

            trail.time = cfg.trailTime;
            trail.startWidth = cfg.trailWidthStart;
            trail.endWidth = cfg.trailWidthEnd;
            trail.minVertexDistance = Mathf.Min(cfg.trailMinVertexDistance, 0.02f);
            trail.numCapVertices = 2;
            trail.numCornerVertices = 2;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.generateLightingData = false;
            trail.autodestruct = false;

            if (_sharedTrailMaterial == null)
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader == null)
                    shader = Shader.Find("Unlit/Color");
                if (shader != null)
                {
                    _sharedTrailMaterial = new Material(shader) { name = "HeroBoltTrailShared" };
                    _sharedTrailMaterial.hideFlags = HideFlags.DontUnloadUnusedAsset;
                }
            }

            if (_sharedTrailMaterial != null)
                trail.material = _sharedTrailMaterial;

            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(cfg.trailStartColor, 0f),
                    new GradientColorKey(cfg.trailEndColor, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(cfg.trailStartColor.a, 0f),
                    new GradientAlphaKey(cfg.trailEndColor.a, 1f),
                });
            trail.colorGradient = g;

            if (sprite != null)
            {
                trail.sortingLayerID = sprite.sortingLayerID;
                var order = sprite.sortingOrder;
                trail.sortingOrder = order > 0 ? order - 1 : order;
            }

            trail.Clear();
            trail.emitting = true;
        }

        void FixedUpdate()
        {
            if (_cfg == null || _destroyScheduled || _rb == null)
                return;

            var stepMag = _cfg.flySpeed * Time.fixedDeltaTime;
            var rot = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg + _cfg.boltRotationOffsetDegrees;

            if (_embedRemaining > 1e-6f)
            {
                var step = Mathf.Min(stepMag, _embedRemaining);
                _rb.MovePosition(_rb.position + _dir * step);
                _embedRemaining -= step;
                _rb.MoveRotation(rot);

                if (_embedRemaining <= 1e-6f)
                    FinishVisualsAndDestroy();
                return;
            }

            if (_damageApplied)
                return;

            var fullStep = _dir * stepMag;
            _rb.MovePosition(_rb.position + fullStep);
            _traveled += stepMag;
            _rb.MoveRotation(rot);

            if (_traveled >= _maxTravelDist)
                ResolveMiss();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_damageApplied || _destroyScheduled || other == null || _targetEnemy == null || _cfg == null)
                return;

            var health = ResolveEnemyHealth(other);
            if (health == null || health != _targetEnemy || !health.IsAlive)
                return;

            _damageApplied = true;
            if (_boltCollider != null)
                _boltCollider.enabled = false;

            health.ApplyDamage(_damage);
            Debug.Log($"[Enemy HP after hero hit] {health.CurrentHealth:F1} / {health.MaxHealth} (enemy '{health.name}')");

            _embedRemaining = Mathf.Max(0f, _cfg.embedDistanceAfterHitWorld);
            if (_embedRemaining <= 1e-6f)
                FinishVisualsAndDestroy();
        }

        static EnemyHealth ResolveEnemyHealth(Collider2D other)
        {
            if (other.attachedRigidbody != null)
            {
                var h = other.attachedRigidbody.GetComponentInParent<EnemyHealth>();
                if (h != null)
                    return h;
            }

            return other.GetComponentInParent<EnemyHealth>();
        }

        void ResolveMiss()
        {
            if (_damageApplied || _destroyScheduled)
                return;
            FinishVisualsAndDestroy();
        }

        void FinishVisualsAndDestroy()
        {
            if (_destroyScheduled)
                return;
            _destroyScheduled = true;

            if (_boltCollider != null)
                _boltCollider.enabled = false;
            if (_sprite != null)
                _sprite.enabled = false;
            if (_trail != null)
                _trail.emitting = false;

            enabled = false;
            if (_rb != null)
                _rb.simulated = false;

            Destroy(gameObject, _cfg != null ? _cfg.trailTime + _cfg.destroyDelayAfterHit : 0.5f);
        }
    }
}
