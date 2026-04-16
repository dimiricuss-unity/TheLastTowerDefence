using UnityEngine;
using TheLastTowerDefence.Heroes.Systems;

namespace TheLastTowerDefence.Enemies.Systems
{
    /// <summary>
    /// Пока герой в зоне угрозы (триггер персонажа пересёк врага), движение ведёт к герою; иначе — к башне.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyHeroFocus : MonoBehaviour
    {
        [SerializeField] EnemyMovement movement;
        Transform _tower;
        CharacterHeroStats _heroInZone;

        void Reset()
        {
            movement = GetComponent<EnemyMovement>();
        }

        public void Initialize(Transform towerTransform)
        {
            _tower = towerTransform;
            ApplyChaseTarget();
        }

        public void NotifyThreatZoneEnter(CharacterHeroStats hero)
        {
            if (hero == null || !hero.IsAlive)
                return;

            if (_heroInZone == hero)
                return;

            if (_heroInZone != null)
                _heroInZone.Died -= OnHeroDied;

            _heroInZone = hero;
            _heroInZone.Died += OnHeroDied;
            ApplyChaseTarget();
        }

        public void NotifyThreatZoneExit(CharacterHeroStats hero)
        {
            if (hero == null || _heroInZone != hero)
                return;

            _heroInZone.Died -= OnHeroDied;
            _heroInZone = null;
            ApplyChaseTarget();
        }

        void OnDestroy()
        {
            if (_heroInZone != null)
                _heroInZone.Died -= OnHeroDied;
        }

        void OnHeroDied(CharacterHeroStats _)
        {
            if (_heroInZone != null)
                _heroInZone.Died -= OnHeroDied;
            _heroInZone = null;
            ApplyChaseTarget();
        }

        void ApplyChaseTarget()
        {
            if (movement == null)
                return;

            var chase = _heroInZone != null && _heroInZone.IsAlive ? _heroInZone.transform : _tower;
            movement.SetChaseTarget(chase);
        }
    }
}
