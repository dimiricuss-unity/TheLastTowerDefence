using System.Collections.Generic;
using UnityEngine;

namespace TheLastTowerDefence.Heroes.Domain
{
    /// <summary>
    /// Пороги опыта для следующего уровня: индекс 0 — сколько XP нужно накопить на 0-м уровне для перехода 0→1, и т.д.
    /// </summary>
    [CreateAssetMenu(fileName = "ExperienceSteps", menuName = "TLTD/Heroes/Experience Steps")]
    public sealed class ExperienceSteps : ScriptableObject
    {
        [Tooltip("Порог XP в текущем уровне для следующего уровня: [0] = 0→1, [1] = 1→2.")]
        public List<int> xpThresholdsForNextLevel = new();

        [Tooltip("Сколько очков характеристик добавить в пул при каждом повышении уровня.")]
        [Min(0)] public int attributePointsGrantedPerLevelUp = 1;

        public int GetXpToNextLevel(int currentLevel)
        {
            if (currentLevel < 0 || currentLevel >= xpThresholdsForNextLevel.Count)
                return 0;

            return Mathf.Max(1, xpThresholdsForNextLevel[currentLevel]);
        }

        public bool HasNextLevel(int currentLevel) => GetXpToNextLevel(currentLevel) > 0;

        /// <summary>Сумма порогов для переходов с уровня 0 до (не включая) <paramref name="targetLevel"/>.</summary>
        public int SumXpThresholdsFromLevelZeroUpToExclusive(int targetLevel)
        {
            var sum = 0;
            for (var i = 0; i < targetLevel && i < xpThresholdsForNextLevel.Count; i++)
                sum += GetXpToNextLevel(i);
            return sum;
        }

        /// <summary>Сумма порогов с индекса 0 по <paramref name="levelIndex"/> включительно (накопительный «потолок» до следующего уровня).</summary>
        public int SumXpThresholdsFromLevelZeroThroughInclusive(int levelIndex)
        {
            var sum = 0;
            for (var i = 0; i <= levelIndex && i < xpThresholdsForNextLevel.Count; i++)
                sum += GetXpToNextLevel(i);
            return sum;
        }

        public int SumAllXpThresholds()
        {
            var sum = 0;
            for (var i = 0; i < xpThresholdsForNextLevel.Count; i++)
                sum += GetXpToNextLevel(i);
            return sum;
        }
    }
}
