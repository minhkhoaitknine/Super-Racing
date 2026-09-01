#if UNITY_EDITOR
using System;
using System.IO;
using SuperRacing.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.UI;
using System.Reflection;

namespace SuperRacing.Audio.Editor
{
    public static class AudioProjectSetup
    {
        private const string AudioRoot = "Assets/Game/Audio";
        private const string ResourcesRoot = AudioRoot + "/Resources";

        [MenuItem("Super Racing/Audio/Rebuild Audio Sandbox")]
        public static void Build()
        {
            Directory.CreateDirectory(ResourcesRoot);
            Directory.CreateDirectory(AudioRoot + "/Clips/Ambience");
            Directory.CreateDirectory(AudioRoot + "/Clips/Music");
            Directory.CreateDirectory(AudioRoot + "/Prefabs");
            AssetDatabase.Refresh();

            AudioCatalog catalog = LoadOrCreate<AudioCatalog>(ResourcesRoot + "/AudioCatalog.asset");
            AssignCatalog(catalog);
            BuildProfiles(catalog);
            EditorUtility.SetDirty(catalog);

            MapAudioProfile beach = LoadOrCreate<MapAudioProfile>(ResourcesRoot + "/BeachAudioProfile.asset");
            beach.displayName = "Beach"; beach.primaryAmbience = catalog.beachWaves; beach.secondaryAmbience = catalog.beachWind; beach.primaryVolume = .58f; beach.secondaryVolume = .08f;
            MapAudioProfile desert = LoadOrCreate<MapAudioProfile>(ResourcesRoot + "/DesertAudioProfile.asset");
            desert.displayName = "Desert"; desert.primaryAmbience = catalog.desertWind; desert.secondaryAmbience = catalog.desertSandGust; desert.primaryVolume = .28f; desert.secondaryVolume = .38f;
            EditorUtility.SetDirty(beach); EditorUtility.SetDirty(desert);

            AudioMixer mixer = BuildMixer();
            BuildPrefab(catalog, mixer);
            BuildSettingsPrefab();
            BuildSandbox();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Super Racing audio catalog and AudioSandbox were rebuilt successfully.");
        }

        public static void BuildFromCommandLine() { Build(); EditorApplication.Exit(0); }

        private static void BuildPrefab(AudioCatalog catalog, AudioMixer mixer)
        {
            GameObject root = new("AudioRoot");
            root.AddComponent<GameAudioManager>().Configure(catalog, mixer);
            root.AddComponent<RaceAudioBinder>();
            PrefabUtility.SaveAsPrefabAsset(root, AudioRoot + "/Prefabs/AudioRoot.prefab");
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static AudioMixer BuildMixer()
        {
            const string path = AudioRoot + "/SuperRacingAudioMixer.mixer";
            if (AssetDatabase.LoadAssetAtPath<AudioMixer>(path) != null) AssetDatabase.DeleteAsset(path);
            Type controllerType = Type.GetType("UnityEditor.Audio.AudioMixerController, UnityEditor");
            MethodInfo create = controllerType?.GetMethod("CreateMixerControllerAtPath", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            object controller = create?.Invoke(null, new object[] { path });
            if (controller == null) { Debug.LogWarning("AudioMixer could not be generated automatically; create it from the Unity Audio Mixer window."); return null; }
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            MethodInfo groupMethod = controllerType.GetMethod("CreateNewGroup", flags);
            MethodInfo addChild = controllerType.GetMethod("AddChildToParent", flags);
            object master = controllerType.GetProperty("masterGroup", flags)?.GetValue(controller);
            System.Collections.Generic.Dictionary<string, object> groups = new();
            foreach (string name in new[] { "Music", "SFX", "Vehicle", "Engine", "Tires", "Collision", "Race", "UI", "Ambience" })
            {
                try { groups[name] = groupMethod?.Invoke(controller, new object[] { name, false }); } catch (Exception e) { Debug.LogWarning("Mixer group generation: " + e.Message); }
            }
            AddMixerChild(addChild, controller, groups["Music"], master); AddMixerChild(addChild, controller, groups["SFX"], master); AddMixerChild(addChild, controller, groups["Ambience"], master);
            AddMixerChild(addChild, controller, groups["Vehicle"], groups["SFX"]); AddMixerChild(addChild, controller, groups["Race"], groups["SFX"]); AddMixerChild(addChild, controller, groups["UI"], groups["SFX"]);
            AddMixerChild(addChild, controller, groups["Engine"], groups["Vehicle"]); AddMixerChild(addChild, controller, groups["Tires"], groups["Vehicle"]); AddMixerChild(addChild, controller, groups["Collision"], groups["Vehicle"]);
            RenameDefaultSnapshot(controllerType, controller);
            ExposeVolume(controllerType, controller, master, "MasterVolume"); ExposeVolume(controllerType, controller, groups["Music"], "MusicVolume");
            ExposeVolume(controllerType, controller, groups["SFX"], "SfxVolume"); ExposeVolume(controllerType, controller, groups["Ambience"], "AmbienceVolume"); ExposeVolume(controllerType, controller, groups["UI"], "UiVolume");
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<AudioMixer>(path);
        }

        private static void AddMixerChild(MethodInfo method, object controller, object child, object parent)
        { if (method != null && child != null && parent != null) method.Invoke(controller, new[] { child, parent }); }
        private static void RenameDefaultSnapshot(Type controllerType, object controller)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            PropertyInfo property = controllerType.GetProperty("snapshots", flags); Array current = property?.GetValue(controller) as Array;
            if (current != null && current.Length > 0) ((UnityEngine.Object)current.GetValue(0)).name = "Default";
        }
        private static void ExposeVolume(Type controllerType, object controller, object group, string exposedName)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            try
            {
                Array effects = group.GetType().GetProperty("effects", flags)?.GetValue(group) as Array; if (effects == null || effects.Length == 0) return;
                object effect = effects.GetValue(0); object guid = effect.GetType().GetMethod("GetGUIDForMixLevel", flags)?.Invoke(effect, null); if (guid == null) return;
                PropertyInfo exposedProperty = controllerType.GetProperty("exposedParameters", flags); Array current = exposedProperty.GetValue(controller) as Array;
                Type exposedType = current.GetType().GetElementType(); object exposed = Activator.CreateInstance(exposedType);
                exposedType.GetField("guid", flags).SetValue(exposed, guid); exposedType.GetField("name", flags).SetValue(exposed, exposedName);
                Array next = Array.CreateInstance(exposedType, current.Length + 1); Array.Copy(current, next, current.Length); next.SetValue(exposed, current.Length); exposedProperty.SetValue(controller, next);
            }
            catch (Exception e) { Debug.LogWarning("Could not expose " + exposedName + ": " + e.GetBaseException().Message); }
        }

        private static void BuildProfiles(AudioCatalog c)
        {
            AudioClip idle = Clip("Vehicle/LOOP_Vehicle_EngineIdle_CHOSEN.wav");
            AudioClip low = Clip("Vehicle/LOOP_Vehicle_EngineLow_CHOSEN.wav");
            AudioClip mid = Clip("Vehicle/LOOP_Vehicle_EngineMid_CHOSEN.wav");
            AudioClip high = Clip("Vehicle/LOOP_Vehicle_EngineHigh_CHOSEN.wav");
            AudioClip offLoad = Clip("Vehicle/LOOP_Vehicle_EngineOffLoad_CHOSEN.wav");
            c.speedsterProfile = ConfigureVehicle("SpeedsterAudioProfile", "Speedster", c, idle, mid, high, high, offLoad, 6, 180f, .9f, 1.9f, .72f);
            c.balancedProfile = ConfigureVehicle("BalancedAudioProfile", "Balanced", c, idle, low, mid, high, offLoad, 5, 145f, .78f, 1.68f, .66f);
            c.controlProfile = ConfigureVehicle("ControlAudioProfile", "Control", c, idle, idle, low, mid, offLoad, 4, 120f, .64f, 1.42f, .72f);
            c.asphaltSurface = ConfigureSurface("AsphaltSurfaceProfile", SurfaceType.Asphalt, c.tireRoll, c.tireSkid, .34f, .7f, .32f, 1f);
            c.sandSurface = ConfigureSurface("SandSurfaceProfile", SurfaceType.Sand, c.tireRoll, c.tireSkid, .46f, .58f, .24f, .82f);
            c.grassSurface = ConfigureSurface("GrassSurfaceProfile", SurfaceType.Grass, c.tireRoll, c.tireSkid, .4f, .5f, .28f, .9f);
        }

        private static VehicleAudioProfile ConfigureVehicle(string asset, string display, AudioCatalog c, AudioClip idle, AudioClip low, AudioClip mid, AudioClip high, AudioClip offLoad, int gears, float maxSpeed, float minPitch, float maxPitch, float volume)
        {
            VehicleAudioProfile p = LoadOrCreate<VehicleAudioProfile>(ResourcesRoot + "/" + asset + ".asset"); p.displayName = display;
            p.engineStart = c.engineStart; p.gearShift = c.gearShift != c.restart ? c.gearShift : null; p.idle = idle; p.lowRpm = low; p.midRpm = mid; p.highRpm = high; p.onLoad = c.accelerationLoad; p.offLoad = offLoad;
            p.gearCount = gears; p.maxSpeedKmh = maxSpeed; p.minPitch = minPitch; p.maxPitch = maxPitch; p.engineVolume = volume; EditorUtility.SetDirty(p); return p;
        }
        private static SurfaceAudioProfile ConfigureSurface(string asset, SurfaceType type, AudioClip roll, AudioClip skid, float rollVolume, float skidVolume, float threshold, float pitch)
        {
            SurfaceAudioProfile p = LoadOrCreate<SurfaceAudioProfile>(ResourcesRoot + "/" + asset + ".asset"); p.surface = type; p.tireRoll = roll; p.tireSkid = skid;
            p.rollVolume = rollVolume; p.skidVolume = skidVolume; p.skidThreshold = threshold; p.pitchMultiplier = pitch; EditorUtility.SetDirty(p); return p;
        }

        private static void BuildSettingsPrefab()
        {
            GameObject panel = new("AudioSettingsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(AudioSettingsPanel));
            RectTransform rect = panel.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(520, 430); panel.GetComponent<Image>().color = new Color(.04f, .05f, .08f, .94f);
            AudioSettingsPanel component = panel.GetComponent<AudioSettingsPanel>(); SerializedObject serialized = new(component);
            string[] names = { "Master", "Music", "SFX", "Ambience", "UI" }; string[] fields = { "master", "music", "sfx", "ambience", "ui" };
            for (int i = 0; i < names.Length; i++)
            {
                Text label = CreateText(panel.transform, names[i], new Vector2(-190, 145 - i * 58), new Vector2(105, 35));
                Slider slider = CreateSlider(panel.transform, new Vector2(25, 145 - i * 58));
                Text value = CreateText(panel.transform, "100%", new Vector2(205, 145 - i * 58), new Vector2(70, 35));
                serialized.FindProperty(fields[i] + "Slider").objectReferenceValue = slider; serialized.FindProperty(fields[i] + "Value").objectReferenceValue = value;
            }
            Toggle toggle = CreateToggle(panel.transform, new Vector2(-100, -165)); Text mute = CreateText(toggle.transform, "Mute", new Vector2(55, 0), new Vector2(100, 35));
            serialized.FindProperty("muteToggle").objectReferenceValue = toggle; serialized.FindProperty("muteLabel").objectReferenceValue = mute;
            Button reset = CreateButton(panel.transform, "Reset defaults", new Vector2(120, -165)); UnityEditor.Events.UnityEventTools.AddPersistentListener(reset.onClick, component.ResetDefaults);
            serialized.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(panel, AudioRoot + "/Prefabs/AudioSettingsPanel.prefab"); UnityEngine.Object.DestroyImmediate(panel);
        }
        private static Text CreateText(Transform parent, string text, Vector2 position, Vector2 size)
        { GameObject go = new(text, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); go.transform.SetParent(parent, false); RectTransform r = go.GetComponent<RectTransform>(); r.anchoredPosition = position; r.sizeDelta = size; Text t = go.GetComponent<Text>(); t.text = text; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.color = Color.white; t.alignment = TextAnchor.MiddleCenter; return t; }
        private static Slider CreateSlider(Transform parent, Vector2 position)
        { GameObject go = new("Slider", typeof(RectTransform), typeof(Slider)); go.transform.SetParent(parent, false); RectTransform r = go.GetComponent<RectTransform>(); r.anchoredPosition = position; r.sizeDelta = new Vector2(300, 28); Slider s = go.GetComponent<Slider>(); Image background = CreateImage(go.transform, "Background", new Color(.2f, .22f, .28f)); Image fill = CreateImage(go.transform, "Fill", new Color(.2f, .65f, 1f)); Image handle = CreateImage(go.transform, "Handle", Color.white); s.fillRect = fill.rectTransform; s.handleRect = handle.rectTransform; s.targetGraphic = handle; return s; }
        private static Toggle CreateToggle(Transform parent, Vector2 position)
        { GameObject go = new("Mute Toggle", typeof(RectTransform), typeof(Toggle)); go.transform.SetParent(parent, false); RectTransform r = go.GetComponent<RectTransform>(); r.anchoredPosition = position; r.sizeDelta = new Vector2(40, 40); Image bg = CreateImage(go.transform, "Background", new Color(.2f, .22f, .28f)); Image check = CreateImage(go.transform, "Checkmark", new Color(.2f, .65f, 1f)); Toggle t = go.GetComponent<Toggle>(); t.targetGraphic = bg; t.graphic = check; return t; }
        private static Button CreateButton(Transform parent, string label, Vector2 position)
        { GameObject go = new(label, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false); RectTransform r = go.GetComponent<RectTransform>(); r.anchoredPosition = position; r.sizeDelta = new Vector2(170, 42); go.GetComponent<Image>().color = new Color(.15f, .45f, .75f); CreateText(go.transform, label, Vector2.zero, r.sizeDelta); return go.GetComponent<Button>(); }
        private static Image CreateImage(Transform parent, string name, Color color)
        { GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); go.transform.SetParent(parent, false); RectTransform r = go.GetComponent<RectTransform>(); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero; Image image = go.GetComponent<Image>(); image.color = color; return image; }


        private static void BuildSandbox()
        {
            const string target = "Assets/Game/Scenes/AudioSandbox.unity";
            AssetDatabase.DeleteAsset(target);
            AssetDatabase.CopyAsset("Assets/Scenes/Test_Race.unity", target);
            Scene scene = EditorSceneManager.OpenScene(target, OpenSceneMode.Single);
            PrototypeVehicleController vehicle = UnityEngine.Object.FindFirstObjectByType<PrototypeVehicleController>();
            if (vehicle != null && vehicle.GetComponent<VehicleAudioEmitter>() == null) vehicle.gameObject.AddComponent<VehicleAudioEmitter>();
            BuildSandboxCourse();
            new GameObject("Audio Sandbox Debug Panel").AddComponent<AudioSandboxDebugPanel>();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void BuildSandboxCourse()
        {
            GameObject root = new("Audio Test Course");
            CreateCoursePrimitive(root.transform, "Asphalt Surface", PrimitiveType.Cube, new Vector3(-12f, .05f, 18f), new Vector3(10f, .1f, 22f), Quaternion.identity, new Color(.18f, .2f, .23f));
            CreateCoursePrimitive(root.transform, "Sand Surface", PrimitiveType.Cube, new Vector3(0f, .05f, 18f), new Vector3(10f, .1f, 22f), Quaternion.identity, new Color(.76f, .58f, .28f));
            CreateCoursePrimitive(root.transform, "Grass Surface", PrimitiveType.Cube, new Vector3(12f, .05f, 18f), new Vector3(10f, .1f, 22f), Quaternion.identity, new Color(.2f, .55f, .25f));
            CreateCoursePrimitive(root.transform, "Landing Ramp", PrimitiveType.Cube, new Vector3(0f, 1.1f, 31f), new Vector3(7f, .5f, 9f), Quaternion.Euler(14f, 0f, 0f), new Color(.25f, .27f, .3f));
            for (int i = -1; i <= 1; i++) CreateCoursePrimitive(root.transform, "Collision Barrier " + (i + 2), PrimitiveType.Cube, new Vector3(i * 4f, 1f, 39f), new Vector3(3f, 2f, 1f), Quaternion.identity, new Color(.85f, .25f, .15f));
        }
        private static GameObject CreateCoursePrimitive(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Quaternion rotation, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent); go.transform.SetPositionAndRotation(position, rotation); go.transform.localScale = scale;
            Renderer renderer = go.GetComponent<Renderer>(); if (renderer != null) { Material material = new(Shader.Find("Universal Render Pipeline/Lit")); material.color = color; renderer.sharedMaterial = material; }
            return go;
        }

        private static void AssignCatalog(AudioCatalog c)
        {
            c.engineStart = Clip("Vehicle/EVT_Vehicle_EngineStart_CHOSEN.wav");
            c.engineIdle = Clip("Vehicle/LOOP_Vehicle_EngineIdle_CHOSEN.wav");
            c.engineDrive = Clip("Vehicle/LOOP_Vehicle_EngineDrive_CHOSEN.wav");
            c.accelerationLoad = Clip("Vehicle/LOOP_Vehicle_AccelerationLoad_CHOSEN.wav");
            c.engineOffLoad = Clip("Vehicle/LOOP_Vehicle_EngineOffLoad_CHOSEN.wav");
            c.tireRoll = Clip("Vehicle/LOOP_Vehicle_TireRoll_CHOSEN.ogg"); c.tireSkid = Clip("Vehicle/LOOP_Vehicle_TireSkid_CHOSEN.ogg");
            c.collisionLight = Clip("Vehicle/EVT_Vehicle_CollisionLight_REALISTIC_CHOSEN.ogg"); c.collisionMedium = Clip("Vehicle/EVT_Vehicle_CollisionHeavy_REALISTIC_CHOSEN.mp3"); c.collisionHeavy = Clip("Vehicle/EVT_Vehicle_CollisionHeavy_REALISTIC_CHOSEN.mp3");
            c.respawn = Clip("Vehicle/EVT_Vehicle_Respawn_CHOSEN.ogg"); c.landing = Clip("Vehicle/EVT_Vehicle_Landing_CHOSEN.ogg");
            c.countdownTick = Clip("Race/EVT_Race_CountdownTick_CHOSEN.ogg"); c.startedGo = Clip("Race/EVT_Race_StartedGo_CHOSEN.ogg");
            c.checkpointPassed = Clip("Race/EVT_Race_CheckpointPassed_CHOSEN.ogg"); c.lapChanged = Clip("Race/EVT_Race_LapChanged_CHOSEN.ogg");
            c.finished = Clip("Race/EVT_Race_Finished_CHOSEN.ogg"); c.newRecord = Clip("Race/EVT_Race_NewRecord_CHOSEN.ogg");
            c.invalidCheckpoint = Clip("Race/EVT_Race_InvalidCheckpoint_CHOSEN.ogg"); c.restart = Clip("Race/EVT_Race_Restart_CHOSEN.ogg");
            c.gearShift = null; // No dedicated shift sample yet; never reuse the race Restart cue.
            c.uiHover = Clip("UI/EVT_UI_Hover_CHOSEN.ogg"); c.uiClick = Clip("UI/EVT_UI_Click_CHOSEN.ogg");
            c.uiConfirm = Clip("UI/EVT_UI_Confirm_CHOSEN.ogg"); c.uiBack = Clip("UI/EVT_UI_Back_CHOSEN.ogg");
            c.uiSelectionChanged = Clip("UI/EVT_UI_SelectionChanged_CHOSEN.ogg"); c.uiError = Clip("UI/EVT_UI_Error_CHOSEN.ogg");
            c.uiStartRace = Clip("UI/EVT_UI_StartRace_CHOSEN.ogg"); c.uiResultsOpen = Clip("UI/EVT_UI_ResultsOpen_CHOSEN.ogg");
            c.beachWaves = Clip("Ambience/LOOP_Ambience_BeachWaves_CHOSEN.flac"); c.beachWind = Clip("Ambience/LOOP_Ambience_BeachWind_CHOSEN.ogg");
            c.desertWind = Clip("Ambience/LOOP_Ambience_DesertWind_CHOSEN.ogg"); c.desertSandGust = Clip("Ambience/EVT_Ambience_DesertSandGust_CHOSEN.wav");
            c.raceMusic = Clip("Music/LOOP_Music_Race_CHOSEN.ogg"); c.menuMusic = Clip("Music/LOOP_Music_Menu_CHOSEN.ogg"); c.resultMusic = Clip("Music/LOOP_Music_Result_CHOSEN.ogg");
        }

        private static AudioClip Clip(string relative) => AssetDatabase.LoadAssetAtPath<AudioClip>(AudioRoot + "/Clips/" + relative);
        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T value = AssetDatabase.LoadAssetAtPath<T>(path);
            if (value != null) return value;
            value = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(value, path); return value;
        }

        private static void GenerateProceduralClips()
        {
            WriteWave(AudioRoot + "/Clips/Ambience/LOOP_Ambience_BeachWaves_CHOSEN.wav", 8f, (t, r) => 0.10f * Noise(r) + 0.09f * Mathf.Sin(t * 1.4f) * Mathf.Sin(t * 0.25f));
            WriteWave(AudioRoot + "/Clips/Ambience/LOOP_Ambience_BeachWind_CHOSEN.wav", 8f, (t, r) => 0.07f * Noise(r) * (0.65f + 0.35f * Mathf.Sin(t * 0.7f)));
            WriteWave(AudioRoot + "/Clips/Ambience/LOOP_Ambience_DesertWind_CHOSEN.wav", 8f, (t, r) => 0.11f * Noise(r) * (0.5f + 0.5f * Mathf.Sin(t * 0.35f)));
            WriteWave(AudioRoot + "/Clips/Ambience/LOOP_Ambience_DesertSandGust_CHOSEN.wav", 8f, (t, r) => 0.08f * Noise(r) * Mathf.Pow(Mathf.Sin(t * 0.42f) * 0.5f + 0.5f, 3f));
            WriteWave(AudioRoot + "/Clips/Music/LOOP_Music_Race_CHOSEN.wav", 8f, (t, r) =>
            {
                float[] notes = { 110f, 146.83f, 164.81f, 196f, 220f, 196f, 164.81f, 146.83f };
                float note = notes[Mathf.FloorToInt(t * 4f) % notes.Length];
                float beat = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * Mathf.PI * 4f)), 5f);
                return 0.10f * Mathf.Sin(t * note * Mathf.PI * 2f) + 0.08f * beat * Mathf.Sin(t * 55f * Mathf.PI * 2f);
            });
        }

        private static float Noise(System.Random random) => (float)(random.NextDouble() * 2.0 - 1.0);
        private static void WriteWave(string assetPath, float seconds, Func<float, System.Random, float> sample)
        {
            const int rate = 22050; int count = Mathf.RoundToInt(seconds * rate); System.Random random = new(42);
            string fullPath = Path.GetFullPath(assetPath); Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            using BinaryWriter writer = new(File.Create(fullPath));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + count * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1); writer.Write((short)1);
            writer.Write(rate); writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(count * 2);
            for (int i = 0; i < count; i++) writer.Write((short)(Mathf.Clamp(sample(i / (float)rate, random), -1f, 1f) * short.MaxValue));
        }
    }
}
#endif
