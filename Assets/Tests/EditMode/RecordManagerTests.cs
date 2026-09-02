using NUnit.Framework;
using SuperRacing.Race;

namespace SuperRacing.Tests
{
    public sealed class RecordManagerTests
    {
        private const string Beach = "test_phase3_beach";
        private const string Desert = "test_phase3_desert";
        private const string Speedster = "test_phase3_speedster";
        private const string Balanced = "test_phase3_balanced";

        [SetUp]
        public void SetUp()
        {
            DeleteTestRecords();
        }

        [TearDown]
        public void TearDown()
        {
            DeleteTestRecords();
        }

        [Test]
        public void BuildsKeyFromTrackAndCar()
        {
            Assert.That(RecordManager.BuildKey(Beach, Speedster), Is.EqualTo("best_time_test_phase3_beach_test_phase3_speedster"));
        }

        [Test]
        public void MissingRecordReturnsFalseAndZero()
        {
            bool hasRecord = RecordManager.TryGetBestTime(Beach, Speedster, out float bestTime);

            Assert.That(hasRecord, Is.False);
            Assert.That(bestTime, Is.Zero);
        }

        [Test]
        public void SavesIndependentRecordsPerTrackAndCar()
        {
            Assert.That(RecordManager.TrySaveBestTime(Beach, Speedster, 61.5f), Is.True);
            Assert.That(RecordManager.TrySaveBestTime(Beach, Balanced, 72.25f), Is.True);
            Assert.That(RecordManager.TrySaveBestTime(Desert, Speedster, 83.75f), Is.True);

            Assert.That(RecordManager.TryGetBestTime(Beach, Speedster, out float beachSpeedster), Is.True);
            Assert.That(RecordManager.TryGetBestTime(Beach, Balanced, out float beachBalanced), Is.True);
            Assert.That(RecordManager.TryGetBestTime(Desert, Speedster, out float desertSpeedster), Is.True);

            Assert.That(beachSpeedster, Is.EqualTo(61.5f));
            Assert.That(beachBalanced, Is.EqualTo(72.25f));
            Assert.That(desertSpeedster, Is.EqualTo(83.75f));
        }

        [Test]
        public void OnlyOverwritesWhenNewTimeIsBetter()
        {
            Assert.That(RecordManager.TrySaveBestTime(Beach, Speedster, 70f), Is.True);
            Assert.That(RecordManager.TrySaveBestTime(Beach, Speedster, 80f), Is.False);
            Assert.That(RecordManager.TryGetBestTime(Beach, Speedster, out float slowerRejectedTime), Is.True);
            Assert.That(slowerRejectedTime, Is.EqualTo(70f));

            Assert.That(RecordManager.TrySaveBestTime(Beach, Speedster, 60f), Is.True);
            Assert.That(RecordManager.TryGetBestTime(Beach, Speedster, out float betterAcceptedTime), Is.True);
            Assert.That(betterAcceptedTime, Is.EqualTo(60f));
        }

        [Test]
        public void RejectsInvalidRecordInputs()
        {
            Assert.That(RecordManager.TrySaveBestTime("", Speedster, 60f), Is.False);
            Assert.That(RecordManager.TrySaveBestTime(Beach, "", 60f), Is.False);
            Assert.That(RecordManager.TrySaveBestTime(Beach, Speedster, 0f), Is.False);
            Assert.That(RecordManager.TrySaveBestTime(Beach, Speedster, -1f), Is.False);
        }

        private static void DeleteTestRecords()
        {
            RecordManager.DeleteBestTime(Beach, Speedster);
            RecordManager.DeleteBestTime(Beach, Balanced);
            RecordManager.DeleteBestTime(Desert, Speedster);
            RecordManager.DeleteBestTime(Desert, Balanced);
        }
    }
}
