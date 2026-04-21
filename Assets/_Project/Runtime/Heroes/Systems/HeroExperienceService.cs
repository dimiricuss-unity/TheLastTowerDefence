using UnityEngine;

namespace TheLastTowerDefence.Heroes.Systems
{
    /// <summary>
    /// Раздаёт опыт за убийство всем героям: суммарный XP врага делится на три (вниз до целого), одинаковая доля каждому.
    /// </summary>
    public static class HeroExperienceService
    {
        public static void GrantSharedKillExperience(int totalExperienceFromEnemy)
        {
            if (totalExperienceFromEnemy <= 0)
                return;

            var share = Mathf.FloorToInt(totalExperienceFromEnemy / 3f);
            if (share <= 0)
                return;

            var heroes = Object.FindObjectsByType<CharacterHeroStats>(FindObjectsSortMode.None);
            for (var i = 0; i < heroes.Length; i++)
            {
                if (heroes[i] != null)
                    heroes[i].AddExperience(share);
            }
        }
    }
}
