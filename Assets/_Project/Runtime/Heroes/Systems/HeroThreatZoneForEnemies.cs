using UnityEngine;
using TheLastTowerDefence.Enemies.Systems;

namespace TheLastTowerDefence.Heroes.Systems
{
    /// <summary>
    /// Вешается на триггер персонажа (например TriggerForEnemies): при пересечении с врагом
    /// переключает его приоритет цели на героя. После смерти героя / выхода из зоны — снова башня.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeroThreatZoneForEnemies : MonoBehaviour
    {
        CharacterHeroStats _hero;

        void Awake()
        {
            _hero = GetComponentInParent<CharacterHeroStats>();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null || _hero == null)
                return;

            var focus = FindEnemyFocus(other);
            if (focus != null)
                focus.NotifyThreatZoneEnter(_hero);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other == null || _hero == null)
                return;

            var focus = FindEnemyFocus(other);
            if (focus != null)
                focus.NotifyThreatZoneExit(_hero);
        }

        static EnemyHeroFocus FindEnemyFocus(Collider2D other)
        {
            if (other.attachedRigidbody != null)
            {
                var f = other.attachedRigidbody.GetComponent<EnemyHeroFocus>();
                if (f != null)
                    return f;
            }

            return other.GetComponentInParent<EnemyHeroFocus>();
        }
    }
}
