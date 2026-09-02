using NUnit.Framework;
using SuperRacing.Race;
using UnityEditor;
using UnityEngine;

namespace SuperRacing.Tests
{
    public sealed class LapTrackerTests
    {
        private GameObject vehicle;
        private LapTracker tracker;
        private Checkpoint[] checkpoints;

        [SetUp]
        public void SetUp()
        {
            vehicle = new GameObject("Test Vehicle");
            tracker = vehicle.AddComponent<LapTracker>();
            checkpoints = new[]
            {
                CreateCheckpoint(0),
                CreateCheckpoint(1),
                CreateCheckpoint(2)
            };
            tracker.Initialize(checkpoints.Length, 2);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(vehicle);
            foreach (Checkpoint checkpoint in checkpoints)
            {
                Object.DestroyImmediate(checkpoint.gameObject);
            }
        }

        [Test]
        public void RejectsCheckpointWhenOrderIsWrong()
        {
            Assert.That(tracker.TryPassCheckpoint(checkpoints[0]), Is.False);
            Assert.That(tracker.ExpectedCheckpointIndex, Is.EqualTo(1));
            Assert.That(tracker.CompletedLaps, Is.Zero);
        }

        [Test]
        public void CompletingSequenceAdvancesLap()
        {
            PassCompleteSequence();

            Assert.That(tracker.CompletedLaps, Is.EqualTo(1));
            Assert.That(tracker.CurrentLap, Is.EqualTo(2));
            Assert.That(tracker.ExpectedCheckpointIndex, Is.EqualTo(1));
            Assert.That(tracker.IsRaceComplete, Is.False);
        }

        [Test]
        public void CompletingAllLapsFinishesRaceOnlyOnce()
        {
            int finishEventCount = 0;
            tracker.RaceCompleted += () => finishEventCount++;

            PassCompleteSequence();
            PassCompleteSequence();

            Assert.That(tracker.IsRaceComplete, Is.True);
            Assert.That(tracker.CompletedLaps, Is.EqualTo(2));
            Assert.That(finishEventCount, Is.EqualTo(1));
            Assert.That(tracker.TryPassCheckpoint(checkpoints[0]), Is.False);
            Assert.That(finishEventCount, Is.EqualTo(1));
        }

        [Test]
        public void ResetRestoresInitialProgress()
        {
            PassCompleteSequence();
            tracker.ResetProgress();

            Assert.That(tracker.CurrentLap, Is.EqualTo(1));
            Assert.That(tracker.CompletedLaps, Is.Zero);
            Assert.That(tracker.ExpectedCheckpointIndex, Is.EqualTo(1));
            Assert.That(tracker.IsRaceComplete, Is.False);
        }

        private void PassCompleteSequence()
        {
            Assert.That(tracker.TryPassCheckpoint(checkpoints[1]), Is.True);
            Assert.That(tracker.TryPassCheckpoint(checkpoints[2]), Is.True);
            Assert.That(tracker.TryPassCheckpoint(checkpoints[0]), Is.True);
        }

        private static Checkpoint CreateCheckpoint(int index)
        {
            GameObject gameObject = new($"Checkpoint {index}");
            gameObject.AddComponent<BoxCollider>().isTrigger = true;
            Checkpoint checkpoint = gameObject.AddComponent<Checkpoint>();

            SerializedObject serializedCheckpoint = new(checkpoint);
            serializedCheckpoint.FindProperty("checkpointIndex").intValue = index;
            serializedCheckpoint.ApplyModifiedPropertiesWithoutUndo();
            return checkpoint;
        }
    }
}
