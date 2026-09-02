using NUnit.Framework;
using SuperRacing.Data;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SuperRacing.Tests
{
    public sealed class GameSelectionTests
    {
        private GameCatalog catalog;
        private CarDefinition car;
        private TrackDefinition track;

        [SetUp]
        public void SetUp()
        {
            GameSelection.Clear();
            catalog = ScriptableObject.CreateInstance<GameCatalog>();
            car = ScriptableObject.CreateInstance<CarDefinition>();
            track = ScriptableObject.CreateInstance<TrackDefinition>();

            SetPrivateString(car, "carId", "phase4_car");
            SetPrivateString(track, "trackId", "phase4_track");

            SerializedObject serializedCatalog = new(catalog);
            SerializedProperty cars = serializedCatalog.FindProperty("cars");
            cars.arraySize = 1;
            cars.GetArrayElementAtIndex(0).objectReferenceValue = car;

            SerializedProperty tracks = serializedCatalog.FindProperty("tracks");
            tracks.arraySize = 1;
            tracks.GetArrayElementAtIndex(0).objectReferenceValue = track;
            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            GameSelection.Clear();
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(car);
            Object.DestroyImmediate(track);
        }

        [Test]
        public void SelectionRestoresFromSavedIds()
        {
            GameSelection.SelectCar(car);
            GameSelection.SelectTrack(track);
            ClearRuntimeSelectionOnly();

            GameSelection.RestoreFromCatalog(catalog);

            Assert.That(GameSelection.SelectedCar, Is.SameAs(car));
            Assert.That(GameSelection.SelectedTrack, Is.SameAs(track));
            Assert.That(GameSelection.IsReadyToRace, Is.True);
        }

        private static void SetPrivateString(Object target, string propertyName, string value)
        {
            SerializedObject serializedObject = new(target);
            serializedObject.FindProperty(propertyName).stringValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ClearRuntimeSelectionOnly()
        {
            BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            typeof(GameSelection).GetField("<SelectedCar>k__BackingField", flags)?.SetValue(null, null);
            typeof(GameSelection).GetField("<SelectedTrack>k__BackingField", flags)?.SetValue(null, null);
        }
    }
}
