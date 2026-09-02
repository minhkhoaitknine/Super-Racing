using NUnit.Framework;
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
