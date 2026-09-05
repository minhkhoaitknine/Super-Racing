using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SuperRacing.Data;
using SuperRacing.UI;

namespace SuperRacing.Audio.Tests
{
    public sealed class AudioRuntimeIntegrationTests
    {
        [UnityTest]
        public IEnumerator RuntimeManagerUsesMixerAndRoutesEveryBus()
        {
            yield return null;
            GameAudioManager manager = GameAudioManager.Instance;
            Assert.That(manager, Is.Not.Null);
            Assert.That(manager.HasMixer, Is.True);
            foreach (string group in new[] { "Music", "SFX", "Engine", "Tires", "Collision", "Race", "UI", "Ambience" })
                Assert.That(manager.GetMixerGroup(group), Is.Not.Null, group);
            foreach (string parameter in new[] { "MasterVolume", "MusicVolume", "SfxVolume", "AmbienceVolume", "UiVolume" })
                Assert.That(manager.Catalog.mixer.GetFloat(parameter, out _), Is.True, parameter);
        }

        [UnityTest]
        public IEnumerator SnapshotStackRestoresPreviousState()
        {
            yield return null;
            GameAudioManager manager = GameAudioManager.Instance;
            manager.ApplySnapshot(AudioSnapshotId.Countdown, 0f);
            manager.PushSnapshot(AudioSnapshotId.Paused, 0f);
            Assert.That(manager.CurrentSnapshot, Is.EqualTo(AudioSnapshotId.Paused));
            manager.PopSnapshot(0f);
            Assert.That(manager.CurrentSnapshot, Is.EqualTo(AudioSnapshotId.Countdown));
            manager.ApplySnapshot(AudioSnapshotId.Default, 0f);
        }

        [UnityTest]
        public IEnumerator ResetSnapshotStateClearsPausedHistory()
        {
            yield return null;
            GameAudioManager manager = GameAudioManager.Instance;
            manager.ApplySnapshot(AudioSnapshotId.Countdown, 0f);
            manager.PushSnapshot(AudioSnapshotId.Paused, 0f);
            manager.ResetSnapshotState(AudioSnapshotId.Default, 0f);
            manager.PopSnapshot(0f);
            Assert.That(manager.CurrentSnapshot, Is.EqualTo(AudioSnapshotId.Default));
        }

        [UnityTest]
        public IEnumerator GarageSettingsButtonOpensExactlyOnePanel()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync("Garage");
            yield return null;
            Button settings = null;
            foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (button.name == "Settings") { settings = button; break; }
            Assert.That(settings, Is.Not.Null);
            settings.onClick.Invoke();
            yield return null;
            AudioSettingsPanel[] panels = Object.FindObjectsByType<AudioSettingsPanel>(FindObjectsSortMode.None);
            Assert.That(panels, Has.Length.EqualTo(1));
            foreach (Graphic graphic in panels[0].GetComponentsInChildren<Graphic>(true))
                Assert.That(graphic.canvasRenderer, Is.Not.Null,
                    $"Audio Settings graphic '{graphic.name}' is missing its CanvasRenderer.");
            foreach (Text text in panels[0].GetComponentsInChildren<Text>(true))
            {
                RectTransform textRect = text.rectTransform;
                Assert.That(text.preferredWidth, Is.LessThanOrEqualTo(textRect.rect.width + 1f),
                    $"Audio Settings text '{text.text}' overflows its allotted width.");
                Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(textRect.rect.height + 1f),
                    $"Audio Settings text '{text.text}' overflows its allotted height.");
            }
            panels[0].RequestClose();
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            LogAssert.ignoreFailingMessages = false;
        }

        [UnityTest]
        public IEnumerator RacePauseMenuContainsAudioSettingsAndRestoresTimeAndSnapshot()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync("Race");
            yield return new WaitForSecondsRealtime(.5f);
            Button settings = null;
            foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (button.name == "Audio Settings Button") { settings = button; break; }
            Assert.That(settings, Is.Not.Null, "Audio settings was not integrated into the teammate-owned pause menu.");
            Assert.That(GameObject.Find("Audio Pause Button"), Is.Not.Null, "Race has no visible pause launcher.");
            Assert.That(GameObject.Find("Audio Pause"), Is.Null, "Audio must not create a second pause system beside RacePauseMenu.");
            Assert.That(settings.GetComponent<UIButtonAudio>(), Is.Not.Null, "Runtime pause/settings button has no UI cue binder.");
            float previousScale = Time.timeScale;
            settings.onClick.Invoke();
            yield return null;
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(GameAudioManager.Instance.CurrentSnapshot, Is.EqualTo(AudioSnapshotId.Paused));
            AudioSettingsPanel panel = Object.FindFirstObjectByType<AudioSettingsPanel>();
            Assert.That(panel, Is.Not.Null);
            panel.RequestClose();
            yield return null;
            Assert.That(Time.timeScale, Is.EqualTo(previousScale));
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            LogAssert.ignoreFailingMessages = false;
        }

        [UnityTest]
        public IEnumerator CompleteRaceSceneUsesFinishMixAndResultMusic()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync("complete_race");
            yield return new WaitForSecondsRealtime(.5f);
            GameAudioManager manager = GameAudioManager.Instance;
            Assert.That(manager.CurrentSnapshot, Is.EqualTo(AudioSnapshotId.Finish));
            Assert.That(manager.CurrentMusicClipName, Is.EqualTo(manager.Catalog.resultMusic.name));
            Assert.That(manager.IsMusicPlaying, Is.True);
            Assert.That(manager.IsAmbiencePlaying, Is.False);
            Assert.That(Object.FindFirstObjectByType<VehicleAudioEmitter>(), Is.Null,
                "The result scene must not attach or keep gameplay vehicle loops.");
            Assert.That(Object.FindFirstObjectByType<AudioSettingsPanel>(), Is.Null,
                "The result scene must not be treated as a pausable race scene.");
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            LogAssert.ignoreFailingMessages = false;
        }

        [UnityTest]
        public IEnumerator TestVehicleHasPlayingAudibleEngineLayers()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync("Race");
            yield return new WaitForSecondsRealtime(1f);
            VehicleAudioEmitter emitter = Object.FindFirstObjectByType<VehicleAudioEmitter>();
            Assert.That(emitter, Is.Not.Null, "Runtime installer did not attach vehicle audio.");
            Assert.That(emitter.EngineLoopCount, Is.EqualTo(6));
            Assert.That(emitter.TireLoopCount, Is.EqualTo(3));
            Assert.That(emitter.LoudestEngineVolume, Is.GreaterThan(.05f), "Engine layers exist but are silent.");
            Assert.That(emitter.OneShotPlayCount, Is.EqualTo(0), "Spawn must not play engine-start/landing thumps automatically.");
            Assert.That(emitter.LandingPlayCount, Is.EqualTo(0), "Spawn settling must not play the landing cue.");
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            LogAssert.ignoreFailingMessages = false;
        }

        [UnityTest]
        public IEnumerator SelectedVehicleFromMenuKeepsTheSameAudibleEngineSetup()
        {
            LogAssert.ignoreFailingMessages = true;
            GameSelection.Clear();
            yield return SceneManager.LoadSceneAsync("Race");
            yield return new WaitForSecondsRealtime(1f);

            VehicleAudioEmitter directEmitter = Object.FindFirstObjectByType<VehicleAudioEmitter>();
            AudioListener directListener = Object.FindFirstObjectByType<AudioListener>();
            Assert.That(directEmitter, Is.Not.Null);
            Assert.That(directListener, Is.Not.Null);
            directEmitter.SetThrottle(1f);
            Rigidbody directBody = directEmitter.GetComponent<Rigidbody>();
            directBody.linearVelocity = directEmitter.transform.forward * 15f;
            yield return new WaitForSecondsRealtime(.5f);
            float directEngineVolume = directEmitter.LoudestEngineVolume;
            float directListenerDistance = Vector3.Distance(directListener.transform.position, directEmitter.transform.position);

            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;

            MainMenuUI mainMenu = Object.FindFirstObjectByType<MainMenuUI>();
            Assert.That(mainMenu, Is.Not.Null);
            mainMenu.OpenGarage();
            yield return new WaitForSecondsRealtime(.25f);
            GarageUI garage = Object.FindFirstObjectByType<GarageUI>();
            Assert.That(garage, Is.Not.Null);
            const string balancedOwnershipKey = "super_racing_owned_car_balanced";
            PlayerPrefs.SetInt(balancedOwnershipKey, 1);
            garage.SelectCar(1); // Balanced is intentionally unlocked only for this audio test.
            garage.ConfirmSelection();
            yield return new WaitForSecondsRealtime(.25f);
            TrackSelectionUI trackSelection = Object.FindFirstObjectByType<TrackSelectionUI>();
            Assert.That(trackSelection, Is.Not.Null);
            trackSelection.StartRace();
            yield return new WaitForSecondsRealtime(1f);

            VehicleAudioEmitter emitter = Object.FindFirstObjectByType<VehicleAudioEmitter>();
            AudioListener listener = Object.FindFirstObjectByType<AudioListener>();
            Assert.That(emitter, Is.Not.Null, "Selected runtime vehicle did not receive an audio emitter.");
            Assert.That(listener, Is.Not.Null, "Race scene has no audio listener.");
            Assert.That(emitter.Profile, Is.Not.Null);
            Assert.That(emitter.Profile.displayName, Is.EqualTo("Balanced"));
            Assert.That(emitter.EngineLoopCount, Is.EqualTo(6));
            Assert.That(emitter.LoudestEngineVolume, Is.GreaterThan(.05f));
            emitter.SetThrottle(1f);
            Rigidbody selectedBody = emitter.GetComponent<Rigidbody>();
            selectedBody.linearVelocity = emitter.transform.forward * 15f;
            yield return new WaitForSecondsRealtime(.5f);
            float selectedListenerDistance = Vector3.Distance(listener.transform.position, emitter.transform.position);
            Assert.That(selectedListenerDistance, Is.LessThan(20f),
                "The gameplay listener is too far from the selected car, which attenuates engine audio.");
            Assert.That(selectedListenerDistance, Is.EqualTo(directListenerDistance).Within(2f),
                "Menu selection changed the listener-to-car distance.");
            Assert.That(emitter.LoudestEngineVolume, Is.GreaterThanOrEqualTo(directEngineVolume * .95f),
                "The selected car became quieter than the direct Test_Vehicle setup at the same throttle and speed.");

            GameSelection.Clear();
            PlayerPrefs.DeleteKey(balancedOwnershipKey);
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            LogAssert.ignoreFailingMessages = false;
        }

        [UnityTest]
        public IEnumerator TrackSelectionPlaysMenuMusicAndHighlightedMapAmbienceTogether()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync("TrackSelection");
            yield return new WaitForSecondsRealtime(1f);
            GameAudioManager manager = GameAudioManager.Instance;
            Assert.That(manager.IsMusicPlaying, Is.True);
            Assert.That(manager.IsAmbiencePlaying, Is.True);
            Assert.That(manager.CurrentMusicClipName, Is.EqualTo(manager.Catalog.menuMusic.name));
            Assert.That(manager.CurrentAmbienceClipName, Is.Not.EqualTo("None"));
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            LogAssert.ignoreFailingMessages = false;
        }

        [UnityTest]
        public IEnumerator TownSquarePreviewAndRaceKeepMapAmbienceActive()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync("TrackSelection");
            yield return null;
            TrackSelectionUI selection = Object.FindFirstObjectByType<TrackSelectionUI>();
            Assert.That(selection, Is.Not.Null);
            selection.SelectTrack(2); // Town Square in the teammate-owned GameCatalog.
            yield return new WaitForSecondsRealtime(.6f);

            MapAudioProfile townSquare = Resources.Load<MapAudioProfile>("TownSquareAudioProfile");
            GameAudioManager manager = GameAudioManager.Instance;
            Assert.That(townSquare, Is.Not.Null);
            Assert.That(manager.IsMusicPlaying, Is.True);
            Assert.That(manager.IsAmbiencePlaying, Is.True);
            Assert.That(manager.CurrentAmbienceClipName, Is.EqualTo(townSquare.primaryAmbience.name));

            selection.StartRace();
            yield return new WaitForSecondsRealtime(1f);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Race"));
            Assert.That(manager.IsAmbiencePlaying, Is.True, "Town Square race must not run without ambience.");
            Assert.That(manager.CurrentAmbienceClipName, Is.EqualTo(townSquare.primaryAmbience.name));

            GameSelection.Clear();
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            LogAssert.ignoreFailingMessages = false;
        }

        [UnityTest]
        public IEnumerator TestRaceBindsAndPlaysCountdownThenGo()
        {
            LogAssert.ignoreFailingMessages = true;
            GameAudioManager manager = GameAudioManager.Instance;
            manager.ResetCueHistory();
            yield return SceneManager.LoadSceneAsync("Race");
            yield return new WaitForSecondsRealtime(3.5f);
            RaceAudioBinder binder = Object.FindFirstObjectByType<RaceAudioBinder>();
            Assert.That(binder, Is.Not.Null);
            Assert.That(binder.IsBound, Is.True, "Race audio did not bind to RaceManager/LapTracker.");
            Assert.That(manager.GetCuePlayCount(AudioCueId.CountdownTick), Is.EqualTo(3));
            Assert.That(manager.GetCuePlayCount(AudioCueId.StartedGo), Is.EqualTo(1));
            Assert.That(manager.CurrentMusicClipName, Is.EqualTo(manager.Catalog.raceMusic.name));
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            LogAssert.ignoreFailingMessages = false;
        }
    }
}
