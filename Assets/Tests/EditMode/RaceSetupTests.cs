using NUnit.Framework;
using SuperRacing.Data;
using SuperRacing.Race;
using UnityEditor;
using UnityEngine;

namespace SuperRacing.Tests
{
    public sealed class RaceSetupTests
    {
        private GameObject setupObject;
        private GameObject vehicleObject;
        private GameObject selectedRoot;
        private GameObject inactiveRoot;
        private RaceSetup setup;
        private LapTracker tracker;

        [SetUp]
        public void SetUp()
        {
            setupObject = new GameObject("RaceSetup");
            setup = setupObject.AddComponent<RaceSetup>();

            vehicleObject = new GameObject("Vehicle");
            tracker = vehicleObject.AddComponent<LapTracker>();

            selectedRoot = new GameObject("Selected Map");
            inactiveRoot = new GameObject("Inactive Map");
            inactiveRoot.SetActive(false);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(setupObject);
            Object.DestroyImmediate(vehicleObject);
            Object.DestroyImmediate(selectedRoot);
            Object.DestroyImmediate(inactiveRoot);
        }

        [Test]
        public void ConfigureDiscoversOnlyCheckpointsUnderSelectedRoot()
        {
            CreateCheckpoint(selectedRoot.transform, 0);
            CreateCheckpoint(selectedRoot.transform, 1);
            CreateCheckpoint(inactiveRoot.transform, 0);
            CreateCheckpoint(null, 0);

            setup.Configure(null, tracker, selectedRoot.transform);

            Assert.That(setup.Checkpoints, Has.Count.EqualTo(2));
            Assert.That(tracker.TotalLaps, Is.EqualTo(1));
            Assert.That(tracker.TryPassCheckpoint(setup.Checkpoints[1]), Is.True);
            Assert.That(tracker.TryPassCheckpoint(setup.Checkpoints[0]), Is.True);
            Assert.That(tracker.IsRaceComplete, Is.True);
        }

        [Test]
        public void ConfigureUsesOnlyFinishLineWhenTrackDoesNotRequireOrderedCheckpoints()
        {
            TrackDefinition track = ScriptableObject.CreateInstance<TrackDefinition>();
            try
            {
                SerializedObject serializedTrack = new(track);
                serializedTrack.FindProperty("lapCount").intValue = 1;
                serializedTrack.FindProperty("requireOrderedCheckpoints").boolValue = false;
                serializedTrack.ApplyModifiedPropertiesWithoutUndo();

                Checkpoint finishLine = CreateCheckpoint(selectedRoot.transform, 0);
                Checkpoint extraCheckpoint = CreateCheckpoint(selectedRoot.transform, 1);
                CreateCheckpoint(selectedRoot.transform, 2);

                setup.Configure(track, tracker, selectedRoot.transform);

                Assert.That(setup.Checkpoints, Has.Count.EqualTo(1));
                Assert.That(setup.Checkpoints[0], Is.SameAs(finishLine));
                Assert.That(tracker.TotalLaps, Is.EqualTo(1));
                Assert.That(tracker.TryPassCheckpoint(extraCheckpoint), Is.False);
                Assert.That(tracker.TryPassCheckpoint(finishLine), Is.True);
                Assert.That(tracker.IsRaceComplete, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(track);
            }
        }

        private static Checkpoint CreateCheckpoint(Transform parent, int index)
        {
            GameObject gameObject = new($"Checkpoint {index}");
            if (parent != null)
            {
                gameObject.transform.SetParent(parent);
            }

            gameObject.AddComponent<BoxCollider>().isTrigger = true;
            Checkpoint checkpoint = gameObject.AddComponent<Checkpoint>();

            SerializedObject serializedCheckpoint = new(checkpoint);
            serializedCheckpoint.FindProperty("checkpointIndex").intValue = index;
            serializedCheckpoint.ApplyModifiedPropertiesWithoutUndo();
            return checkpoint;
        }
    }
}
