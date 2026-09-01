using System.Collections;
using NUnit.Framework;
using SuperRacing.Audio;
using UnityEngine;
using UnityEngine.TestTools;

namespace SuperRacing.Tests
{
    public sealed class AudioPlayModeTests
    {
        [UnityTest]
        public IEnumerator CatalogAndProfilesContainRequiredAssets()
        {
            AudioCatalog catalog = Resources.Load<AudioCatalog>("AudioCatalog"); yield return null;
            Assert.That(catalog, Is.Not.Null); Assert.That(catalog.menuMusic, Is.Not.Null); Assert.That(catalog.raceMusic, Is.Not.Null);
            Assert.That(catalog.speedsterProfile, Is.Not.Null); Assert.That(catalog.balancedProfile, Is.Not.Null); Assert.That(catalog.controlProfile, Is.Not.Null);
            Assert.That(catalog.asphaltSurface, Is.Not.Null); Assert.That(catalog.beachWaves, Is.Not.Null); Assert.That(catalog.desertWind, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator RuntimeInstallerCreatesSinglePersistentManager()
        {
            yield return null; Assert.That(GameAudioManager.Instance, Is.Not.Null);
            GameObject duplicate = new("Duplicate Audio Manager"); duplicate.AddComponent<GameAudioManager>(); yield return null;
            Assert.That(Object.FindObjectsByType<GameAudioManager>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator BusSettingsRoundTripAndMute()
        {
            yield return null; GameAudioManager manager = GameAudioManager.Instance; Assert.That(manager, Is.Not.Null);
            manager.SetBusVolume(AudioBus.Ambience, .42f); Assert.That(manager.GetBusVolume(AudioBus.Ambience), Is.EqualTo(.42f).Within(.001f));
            manager.SetMuted(true); Assert.That(manager.IsMuted, Is.True); manager.ResetAudioSettings(); Assert.That(manager.IsMuted, Is.False);
        }
    }
}
