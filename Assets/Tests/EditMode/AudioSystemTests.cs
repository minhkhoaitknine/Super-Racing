using NUnit.Framework;
using SuperRacing.Audio;
using UnityEngine;

namespace SuperRacing.Tests
{
    public sealed class AudioSystemTests
    {
        [TestCase(1f, 0f)]
        [TestCase(.1f, -20f)]
        [TestCase(0f, -80f)]
        public void NormalizedVolumeConvertsToDecibels(float normalized, float expected)
        { Assert.That(GameAudioManager.NormalizedToDb(normalized), Is.EqualTo(expected).Within(.01f)); }

        [Test]
        public void VehicleProfileCalculatesBoundedGearsAndRpm()
        {
            VehicleAudioProfile profile = ScriptableObject.CreateInstance<VehicleAudioProfile>(); profile.gearCount = 6; profile.maxSpeedKmh = 180f;
            Assert.That(profile.GearForSpeed(0f), Is.EqualTo(1)); Assert.That(profile.GearForSpeed(180f), Is.EqualTo(6));
            Assert.That(profile.RpmForSpeed(95f, 4, .8f), Is.InRange(0f, 1f)); Object.DestroyImmediate(profile);
        }

        [Test]
        public void RpmCrossfadeAlwaysHasAnAudibleLayer()
        {
            for (int i = 0; i <= 20; i++)
            {
                float[] weights = VehicleAudioEmitter.RpmWeights(i / 20f); float sum = 0f;
                foreach (float weight in weights) { Assert.That(weight, Is.InRange(0f, 1f)); sum += weight; }
                Assert.That(sum, Is.GreaterThan(.5f));
            }
        }

        [Test]
        public void CatalogMapsTypedUiCue()
        {
            AudioCatalog catalog = ScriptableObject.CreateInstance<AudioCatalog>(); AudioClip clip = AudioClip.Create("click", 64, 1, 8000, false); catalog.uiClick = clip;
            Assert.That(catalog.GetCue(AudioCueId.UIClick), Is.SameAs(clip)); Object.DestroyImmediate(clip); Object.DestroyImmediate(catalog);
        }
    }
}
