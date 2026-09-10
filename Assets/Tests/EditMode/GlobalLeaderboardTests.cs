using System;
using NUnit.Framework;
using SuperRacing.Race;
using SuperRacing.UI;
using UnityEngine;

namespace SuperRacing.Tests
{
    public sealed class GlobalLeaderboardTests
    {
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("two words")]
        [TestCase("Racer#1234")]
        [TestCase("<b>Racer</b>")]
        [TestCase("1234567890123456789012345")]
        public void InvalidNamesAreRejectedBeforeNetworkRequest(string name)
        {
            Assert.That(GlobalLeaderboardService.ValidateUsername(name), Is.Not.Null);
        }

        [TestCase("Racer_01")]
        [TestCase("HoàngPhát")]
        public void ValidDisplayNamesAreAccepted(string name)
        {
            Assert.That(GlobalLeaderboardService.ValidateUsername(name), Is.Null);
        }

        [TestCase("Racer#1234", "Racer")]
        [TestCase("Racer", "Racer")]
        public void UnityDiscriminatorIsHiddenInDisplayName(string fullName, string expected)
        {
            Assert.That(GlobalLeaderboardService.WithoutNameTag(fullName), Is.EqualTo(expected));
        }

        [TestCase(61.234f, 61234L)]
        [TestCase(0.0001f, 1L)]
        public void RaceSecondsBecomePositiveMilliseconds(float seconds, long expected)
        {
            Assert.That(GlobalLeaderboardService.ToMilliseconds(seconds), Is.EqualTo(expected));
        }

        [TestCase(0f)] [TestCase(-1f)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)]
        public void InvalidRaceTimesCannotBeSubmitted(float seconds)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GlobalLeaderboardService.ToMilliseconds(seconds));
        }

        [Test]
        public void OfflineQueueKeepsFastestTimePerMapAcrossSerialization()
        {
            var queue = new PendingRaceScores();
            queue.KeepBest("map1", 60000);
            queue.KeepBest("map1", 70000);
            queue.KeepBest("map2", 80000);
            queue.KeepBest("map1", 50000);
            var restored = JsonUtility.FromJson<PendingRaceScores>(JsonUtility.ToJson(queue));
            Assert.That(restored.scores.Count, Is.EqualTo(2));
            Assert.That(restored.scores.Find(s => s.board == "map1").milliseconds, Is.EqualTo(50000));
            Assert.That(restored.scores.Find(s => s.board == "map2").milliseconds, Is.EqualTo(80000));
        }

        [Test]
        public void ProductionMapsAreConfiguredAndPrototypeIsExcluded()
        {
            var config = JsonUtility.FromJson<GlobalLeaderboardSettings>(Resources.Load<TextAsset>("GlobalLeaderboards").text);
            Assert.That(config.BoardFor("beach"), Is.EqualTo("map1"));
            Assert.That(config.BoardFor("desert"), Is.EqualTo("map2"));
            Assert.That(config.BoardFor("town_square"), Is.EqualTo("map3"));
            Assert.That(config.BoardFor("test_track"), Is.Null);
        }

        [TestCase(61234d, "01:01.234")]
        [TestCase(59999d, "00:59.999")]
        public void GlobalTimesShowMilliseconds(double score, string expected)
        {
            Assert.That(GlobalLeaderboardPanel.FormatScore(score), Is.EqualTo(expected));
        }
    }
}
