using System.Reflection;
using NUnit.Framework;
using SuperRacing.Race;
using UnityEditor;
using UnityEngine;

namespace SuperRacing.Tests
{
    public sealed class RaceManagerFinishTests
    {
        private GameObject managerObject;
        private GameObject timerObject;
        private GameObject trackerObject;
        private GameObject finishObject;

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(managerObject);
            Object.DestroyImmediate(timerObject);
            Object.DestroyImmediate(trackerObject);
            Object.DestroyImmediate(finishObject);
        }

        [Test]
        public void FinishLineFallbackDoesNotCompleteBeforeExpectedCheckpoint()
        {
            managerObject = new GameObject("Race Manager");
            timerObject = new GameObject("Race Timer");
            trackerObject = new GameObject("Vehicle");
            finishObject = new GameObject("FinishLine");

            RaceManager manager = managerObject.AddComponent<RaceManager>();
            RaceTimer timer = timerObject.AddComponent<RaceTimer>();
            LapTracker tracker = trackerObject.AddComponent<LapTracker>();
            Checkpoint finish = CreateCheckpoint(finishObject, 0);

            tracker.Initialize(3, 1);
            SetPrivateField(manager, "lapTracker", tracker);
            SetPrivateField(manager, "raceTimer", timer);
            SetPrivateField(manager, "finishLineCheckpoint", finish);
            SetPrivateField(manager, "hasLeftFinishLine", true);
            SetPrivateField(manager, "minimumFinishSeconds", 0f);
            SetPrivateField(manager, "<State>k__BackingField", RaceManager.RaceState.Racing);

            Assert.That(tracker.ExpectedCheckpointIndex, Is.EqualTo(1));
            Assert.That(manager.TryCompleteFromFinishLine(tracker), Is.False);
            Assert.That(tracker.IsRaceComplete, Is.False);
            Assert.That(manager.State, Is.EqualTo(RaceManager.RaceState.Racing));
        }

        private static Checkpoint CreateCheckpoint(GameObject gameObject, int index)
        {
            gameObject.AddComponent<BoxCollider>().isTrigger = true;
            Checkpoint checkpoint = gameObject.AddComponent<Checkpoint>();

            SerializedObject serializedCheckpoint = new(checkpoint);
            serializedCheckpoint.FindProperty("checkpointIndex").intValue = index;
            serializedCheckpoint.ApplyModifiedPropertiesWithoutUndo();
            return checkpoint;
        }

        private static void SetPrivateField<T>(RaceManager manager, string fieldName, T value)
        {
            FieldInfo field = typeof(RaceManager).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName}");
            field.SetValue(manager, value);
        }
    }
}
