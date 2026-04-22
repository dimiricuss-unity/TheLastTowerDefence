using System.Collections.Generic;
using NUnit.Framework;
using TheLastTowerDefence.Heroes.Domain;
using UnityEngine;

namespace TheLastTowerDefence.Tests
{
    public sealed class ExperienceStepsTests
    {
        [Test]
        public void Cumulative_milestones_yield_expected_segments_and_sums()
        {
            var e = ScriptableObject.CreateInstance<ExperienceSteps>();
            e.xpThresholdsForNextLevel = new List<int> { 250, 750, 1500, 2800 };

            Assert.AreEqual(250, e.GetXpToNextLevel(0));
            Assert.AreEqual(500, e.GetXpToNextLevel(1));
            Assert.AreEqual(750, e.GetXpToNextLevel(2));
            Assert.AreEqual(1300, e.GetXpToNextLevel(3));

            Assert.AreEqual(0, e.SumXpThresholdsFromLevelZeroUpToExclusive(0));
            Assert.AreEqual(250, e.SumXpThresholdsFromLevelZeroUpToExclusive(1));
            Assert.AreEqual(750, e.SumXpThresholdsFromLevelZeroUpToExclusive(2));

            Assert.AreEqual(250, e.SumXpThresholdsFromLevelZeroThroughInclusive(0));
            Assert.AreEqual(750, e.SumXpThresholdsFromLevelZeroThroughInclusive(1));
            Assert.AreEqual(1500, e.SumXpThresholdsFromLevelZeroThroughInclusive(2));

            Assert.AreEqual(2800, e.SumAllXpThresholds());
        }
    }
}
