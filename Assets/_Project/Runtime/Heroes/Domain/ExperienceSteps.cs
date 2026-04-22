using System.Collections.Generic;
using UnityEngine;

namespace TheLastTowerDefence.Heroes.Domain
{
    /// <summary>
    /// Накопительные вехи опыта: <c>xpThresholdsForNextLevel[i]</c> — сколько всего XP с нуля нужно набрать,
    /// чтобы **достичь уровня <c>i + 1</c>** (после перехода 0→1 на счёте <c>[0]</c>, после 1→2 — <c>[1]</c>, и т.д.).
    /// Размер сегмента уровня <c>L</c> → <c>L + 1</c>: <c>[L] - ([L-1]</c> или 0<c>)</c>.
    /// Список должен быть **строго возрастающим**; минимум 1 на каждом шаге.
    /// </summary>
    [CreateAssetMenu(fileName = "ExperienceSteps", menuName = "TLTD/Heroes/Experience Steps")]
    public sealed class ExperienceSteps : ScriptableObject
    {
        [Tooltip("Накопительный XP к достижению уровня: [0]=до ур.1, [1]=до ур.2, … (строго возрастающий список).")]
        public List<int> xpThresholdsForNextLevel = new();

        [Tooltip("Сколько очков характеристик добавить в пул при каждом повышении уровня.")]
        [Min(0)] public int attributePointsGrantedPerLevelUp = 1;

        /// <summary>Накопительная веха для перехода с уровня <paramref name="levelIndex"/> на <c>levelIndex+1</c>.</summary>
        public int GetCumulativeXpMilestone(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= xpThresholdsForNextLevel.Count)
                return 0;

            return Mathf.Max(1, xpThresholdsForNextLevel[levelIndex]);
        }

        /// <summary>Сколько XP нужно набрать **на текущем уровне** (в сегменте), чтобы перейти дальше.</summary>
        public int GetXpToNextLevel(int currentLevel)
        {
            if (currentLevel < 0 || currentLevel >= xpThresholdsForNextLevel.Count)
                return 0;

            var cur = GetCumulativeXpMilestone(currentLevel);
            var prev = currentLevel > 0 ? GetCumulativeXpMilestone(currentLevel - 1) : 0;
            return Mathf.Max(1, cur - prev);
        }

        public bool HasNextLevel(int currentLevel) => GetXpToNextLevel(currentLevel) > 0;

        /// <summary>Накопительный XP в начале уровня <paramref name="targetLevel"/> (сумма сегментов уровней 0…<c>targetLevel-1</c>).</summary>
        public int SumXpThresholdsFromLevelZeroUpToExclusive(int targetLevel)
        {
            if (targetLevel <= 0 || xpThresholdsForNextLevel.Count == 0)
                return 0;

            var idx = targetLevel - 1;
            if (idx < 0)
                return 0;
            if (idx >= xpThresholdsForNextLevel.Count)
                idx = xpThresholdsForNextLevel.Count - 1;

            return GetCumulativeXpMilestone(idx);
        }

        /// <summary>Накопительный XP — верхняя граница текущего этапа (веха до следующего уровня для уровня <paramref name="levelIndex"/>).</summary>
        public int SumXpThresholdsFromLevelZeroThroughInclusive(int levelIndex)
        {
            if (levelIndex < 0 || xpThresholdsForNextLevel.Count == 0)
                return 0;

            var idx = Mathf.Min(levelIndex, xpThresholdsForNextLevel.Count - 1);
            return GetCumulativeXpMilestone(idx);
        }

        /// <summary>Сумма всех сегментов = последняя накопительная веха.</summary>
        public int SumAllXpThresholds()
        {
            if (xpThresholdsForNextLevel.Count == 0)
                return 0;

            return GetCumulativeXpMilestone(xpThresholdsForNextLevel.Count - 1);
        }

        void OnValidate()
        {
            for (var i = 1; i < xpThresholdsForNextLevel.Count; i++)
            {
                if (xpThresholdsForNextLevel[i] <= xpThresholdsForNextLevel[i - 1])
                {
                    Debug.LogWarning(
                        $"[{nameof(ExperienceSteps)}] '{name}': элементы должны строго возрастать " +
                        $"(индекс {i}: {xpThresholdsForNextLevel[i]} <= {xpThresholdsForNextLevel[i - 1]}).",
                        this);
                }
            }
        }
    }
}
