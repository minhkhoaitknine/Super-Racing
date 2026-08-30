using NUnit.Framework;
using SuperRacing.Race;

namespace SuperRacing.Tests
{
    public sealed class RecordManagerTests
    {
        private const string TrackId = "test_track_record_manager";
        private const string CarId = "test_car_record_manager";

        [SetUp]
        public void SetUp()
        {
            RecordManager.DeleteBestTime(TrackId, CarId);
        }

        [TearDown]
        public void TearDown()
        {
            RecordManager.DeleteBestTime(TrackId, CarId);
        }

        [Test]
        public void BuildsStableKey()
        {
            Assert.That(RecordManager.BuildKey("beach", "speedster"), Is.EqualTo("best_time_beach_speedster"));
        }

        [Test]
        public void SavesFirstValidTime()
        {
            Assert.That(RecordManager.TrySaveBestTime(TrackId, CarId, 42.5f), Is.True);
            Assert.That(RecordManager.TryGetBestTime(TrackId, CarId, out float bestTime), Is.True);
            Assert.That(bestTime, Is.EqualTo(42.5f));
        }

        [Test]
        public void ReplacesOnlyWithFasterTime()
        {
            RecordManager.TrySaveBestTime(TrackId, CarId, 42.5f);

            Assert.That(RecordManager.TrySaveBestTime(TrackId, CarId, 50f), Is.False);
            Assert.That(RecordManager.TrySaveBestTime(TrackId, CarId, 40f), Is.True);
            RecordManager.TryGetBestTime(TrackId, CarId, out float bestTime);
            Assert.That(bestTime, Is.EqualTo(40f));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        public void RejectsInvalidTime(float invalidTime)
        {
            Assert.That(RecordManager.TrySaveBestTime(TrackId, CarId, invalidTime), Is.False);
            Assert.That(RecordManager.TryGetBestTime(TrackId, CarId, out _), Is.False);
        }
    }
}
