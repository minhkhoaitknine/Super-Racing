using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace SuperRacing.Audio.Tests
{
    public sealed class AudioIntegrationTests
    {
        private AudioCatalog catalog;

        [SetUp]
        public void SetUp() => catalog = Resources.Load<AudioCatalog>("AudioCatalog");

        [Test]
        public void CatalogHasMixerSettingsAndRuntimeProfiles()
        {
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.mixer, Is.Not.Null);
            Assert.That(catalog.audioSettingsPrefab, Is.Not.Null);
            Assert.That(catalog.speedsterProfile, Is.Not.Null);
            Assert.That(catalog.balancedProfile, Is.Not.Null);
            Assert.That(catalog.controlProfile, Is.Not.Null);
        }

        [Test]
        public void MixerContainsRequiredGroupsAndSnapshots()
        {
            foreach (string group in new[] { "Music", "SFX", "Vehicle", "Engine", "Tires", "Collision", "Race", "UI", "Ambience" })
                Assert.That(catalog.mixer.FindMatchingGroups(group), Is.Not.Empty, group);
            foreach (string snapshot in new[] { "Default", "Countdown", "Paused", "Finish" })
                Assert.That(catalog.mixer.FindSnapshot(snapshot), Is.Not.Null, snapshot);
            foreach (string parameter in new[] { "MasterVolume", "MusicVolume", "SfxVolume", "AmbienceVolume", "UiVolume" })
                Assert.That(catalog.mixer.GetFloat(parameter, out _), Is.True, parameter + " is not a valid exposed mixer parameter");
        }

        [Test]
        public void MasterVolumeHasSafeMaximum()
        {
            Assert.That(GameAudioManager.MaxMasterVolume, Is.EqualTo(.7f));
            Assert.That(GameAudioManager.DefaultAmbienceVolume, Is.EqualTo(.7f));
            Assert.That(GameAudioManager.AmbienceHeadroomDb, Is.Zero);
        }

        [Test]
        public void SpaceBrakeCanDriveSkidWithoutWheelSlip()
        {
            Assert.That(VehicleAudioEmitter.BrakeSkidAmount(1f, 0f), Is.Zero);
            Assert.That(VehicleAudioEmitter.BrakeSkidAmount(0f, 50f), Is.Zero);
            Assert.That(VehicleAudioEmitter.BrakeSkidAmount(1f, 20f), Is.GreaterThan(.25f));
            Assert.That(VehicleAudioEmitter.BrakeSkidAmount(1f, 40f), Is.GreaterThan(.8f));
            float brake = VehicleAudioEmitter.BrakeSkidAmount(1f, 40f);
            Assert.That(VehicleAudioEmitter.BrakeSkidTargetVolume(brake, .62f), Is.GreaterThan(.58f));
        }

        [Test]
        public void VehicleProfilesHaveDistinctTuningAndRealShiftVariants()
        {
            Assert.That(catalog.speedsterProfile.gearCount, Is.EqualTo(6));
            Assert.That(catalog.balancedProfile.gearCount, Is.EqualTo(5));
            Assert.That(catalog.controlProfile.gearCount, Is.EqualTo(4));
            foreach (VehicleAudioProfile profile in new[] { catalog.speedsterProfile, catalog.balancedProfile, catalog.controlProfile })
            {
                Assert.That(profile.gearShiftVariants, Has.Length.GreaterThanOrEqualTo(2));
                Assert.That(profile.gearShiftVariants, Has.None.Null);
                Assert.That(profile.backfire, Is.Not.Null);
                Assert.That(profile.backfireVariants, Is.Not.Empty);
                Assert.That(profile.backfireVariants, Has.None.Null);
                Assert.That(profile.engineVolume, Is.GreaterThanOrEqualTo(.84f));
                Assert.That(profile.loadVolume, Is.GreaterThanOrEqualTo(.42f));
            }
        }

        [Test]
        public void SurfaceProfilesUseDifferentRealRecordings()
        {
            SurfaceAudioProfile[] surfaces = { catalog.asphaltSurface, catalog.sandSurface, catalog.grassSurface };
            Assert.That(surfaces, Has.None.Null);
            Assert.That(surfaces[0].tireRoll, Is.Not.SameAs(surfaces[1].tireRoll));
            Assert.That(surfaces[1].tireRoll, Is.Not.SameAs(surfaces[2].tireRoll));
            Assert.That(surfaces[0].tireSkid, Is.Not.SameAs(surfaces[1].tireSkid));
            Assert.That(surfaces[1].tireSkid, Is.Not.SameAs(surfaces[2].tireSkid));
            foreach (SurfaceAudioProfile surface in surfaces)
            {
                Assert.That(surface.tireRoll, Is.Not.Null);
                Assert.That(surface.tireSkid, Is.Not.Null);
                Assert.That(surface.rollVolume, Is.GreaterThanOrEqualTo(.34f));
                Assert.That(surface.skidVolume, Is.GreaterThanOrEqualTo(.5f));
                Assert.That(surface.skidThreshold, Is.LessThanOrEqualTo(.34f));
            }
        }

        [Test]
        public void MapProfilesKeepWindAudible()
        {
            MapAudioProfile beach = Resources.Load<MapAudioProfile>("BeachAudioProfile");
            MapAudioProfile desert = Resources.Load<MapAudioProfile>("DesertAudioProfile");
            MapAudioProfile townSquare = Resources.Load<MapAudioProfile>("TownSquareAudioProfile");
            Assert.That(beach.secondaryAmbience, Is.Not.Null);
            Assert.That(beach.primaryVolume, Is.EqualTo(1f));
            Assert.That(beach.secondaryVolume, Is.GreaterThanOrEqualTo(.7f));
            Assert.That(desert.primaryAmbience, Is.Not.Null);
            Assert.That(desert.primaryVolume, Is.EqualTo(1f));
            Assert.That(desert.secondaryVolume, Is.GreaterThanOrEqualTo(.6f));
            Assert.That(desert.secondaryVolume, Is.LessThan(desert.primaryVolume));
            Assert.That(beach.primaryAmbience.name, Does.Contain("NORMALIZED_CHOSEN"));
            Assert.That(beach.secondaryAmbience.name, Does.Contain("NORMALIZED_CHOSEN"));
            Assert.That(desert.primaryAmbience.name, Does.Contain("NORMALIZED_CHOSEN"));
            Assert.That(desert.secondaryAmbience.name, Does.Contain("NORMALIZED_CHOSEN"));
            Assert.That(townSquare, Is.Not.Null, "Town Square was added to the game catalog and must not run silent.");
            Assert.That(townSquare.primaryAmbience, Is.Not.Null);
            Assert.That(townSquare.secondaryAmbience, Is.Not.Null);
            Assert.That(townSquare.primaryVolume, Is.GreaterThanOrEqualTo(.65f));
        }

        [Test]
        public void CollisionTiersHaveIndependentVariants()
        {
            Assert.That(catalog.collisionLightVariants, Is.Not.Empty);
            Assert.That(catalog.collisionMediumVariants, Is.Not.Empty);
            Assert.That(catalog.collisionHeavyVariants, Is.Not.Empty);
            Assert.That(catalog.collisionLightVariants, Has.None.Null);
            Assert.That(catalog.collisionMediumVariants, Has.None.Null);
            Assert.That(catalog.collisionHeavyVariants, Has.None.Null);
            Assert.That(catalog.collisionMedium, Is.Not.SameAs(catalog.collisionHeavy));
            Assert.That(catalog.collisionLight.length, Is.LessThanOrEqualTo(.4f));
            Assert.That(catalog.collisionMedium.length, Is.LessThanOrEqualTo(.5f));
            Assert.That(catalog.collisionHeavy.length, Is.LessThanOrEqualTo(.56f));
            Assert.That(catalog.collisionLight.name, Does.Contain("TRIMMED_CHOSEN"));
            Assert.That(catalog.collisionMedium.name, Does.Contain("TRIMMED_CHOSEN"));
            Assert.That(catalog.collisionHeavy.name, Does.Contain("TRIMMED_CHOSEN"));
        }

        [Test]
        public void SurfaceResolverUsesRequestedFallback()
            => Assert.That(SurfaceAudioResolver.Resolve(null, SurfaceType.Grass), Is.EqualTo(SurfaceType.Grass));

        [Test]
        public void EveryPublicCueHasAClip()
        {
            foreach (AudioCueId cue in System.Enum.GetValues(typeof(AudioCueId)))
                Assert.That(catalog.GetCue(cue), Is.Not.Null, cue.ToString());
        }

        [Test]
        public void GoCueUsesShortSpokenVoiceInsteadOfUiConfirmation()
        {
            Assert.That(catalog.startedGo.name, Does.Contain("StartedGo_VOICE_NORMALIZED_CHOSEN"));
            Assert.That(catalog.startedGo.length, Is.LessThan(1f));
            Assert.That(catalog.startedGo, Is.Not.SameAs(catalog.uiConfirm));

            float[] samples = new float[catalog.startedGo.samples * catalog.startedGo.channels];
            Assert.That(catalog.startedGo.GetData(samples, 0), Is.True, "GO must be decoded and preloaded before its first playback.");
            double squares = 0d;
            float peak = 0f;
            foreach (float sample in samples)
            {
                squares += sample * sample;
                peak = Mathf.Max(peak, Mathf.Abs(sample));
            }
            float rms = Mathf.Sqrt((float)(squares / samples.Length));
            Assert.That(peak, Is.GreaterThanOrEqualTo(.8f), "GO peak is too quiet.");
            Assert.That(rms, Is.GreaterThanOrEqualTo(.18f), "GO RMS is too quiet.");
        }

        [TestCase("Start Race", AudioCueId.UIStartRace)]
        [TestCase("start_race", AudioCueId.UIStartRace)]
        [TestCase("Invalid Choice", AudioCueId.UIError)]
        [TestCase("Cancel", AudioCueId.UIBack)]
        [TestCase("Garage Button", AudioCueId.UIBack)]
        [TestCase("Main Menu", AudioCueId.UIBack)]
        [TestCase("Select Car", AudioCueId.UISelectionChanged)]
        public void SemanticButtonsResolveToExpectedCue(string name, AudioCueId expected)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(Button));
            try { Assert.That(AudioRuntimeInstaller.CueForButton(go.GetComponent<Button>()), Is.EqualTo(expected)); }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void SettingsPrefabHasCompletePolishedLayout()
        {
            GameObject prefab = catalog.audioSettingsPrefab;
            Assert.That(prefab, Is.Not.Null);
            RectTransform root = prefab.GetComponent<RectTransform>();
            Assert.That(root, Is.Not.Null);
            Assert.That(root.sizeDelta, Is.EqualTo(new Vector2(1920f, 1080f)));

            Transform card = prefab.transform.Find("Settings Card");
            Assert.That(card, Is.Not.Null);
            Assert.That(((RectTransform)card).sizeDelta, Is.EqualTo(new Vector2(720f, 650f)));
            Assert.That(prefab.GetComponentsInChildren<Slider>(true), Has.Length.EqualTo(5));
            Assert.That(card.Find("AUDIO SETTINGS"), Is.Not.Null);
            Assert.That(card.Find("CUSTOMIZE YOUR RACE MIX"), Is.Not.Null);
            Assert.That(card.Find("RESET"), Is.Not.Null);
            Assert.That(card.Find("DONE"), Is.Not.Null);
        }
    }
}
