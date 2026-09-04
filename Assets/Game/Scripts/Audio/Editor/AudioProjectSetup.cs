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
            ConfigureImportSettings();

            AudioCatalog catalog = LoadOrCreate<AudioCatalog>(ResourcesRoot + "/AudioCatalog.asset");
            AssignCatalog(catalog);
            BuildProfiles(catalog);
            EditorUtility.SetDirty(catalog);

            MapAudioProfile beach = LoadOrCreate<MapAudioProfile>(ResourcesRoot + "/BeachAudioProfile.asset");
            beach.displayName = "Beach"; beach.primaryAmbience = catalog.beachWaves; beach.secondaryAmbience = catalog.beachWind; beach.primaryVolume = 1f; beach.secondaryVolume = .75f;
            MapAudioProfile desert = LoadOrCreate<MapAudioProfile>(ResourcesRoot + "/DesertAudioProfile.asset");
            desert.displayName = "Desert"; desert.primaryAmbience = catalog.desertWind; desert.secondaryAmbience = catalog.desertSandGust; desert.primaryVolume = 1f; desert.secondaryVolume = .65f;
            MapAudioProfile townSquare = LoadOrCreate<MapAudioProfile>(ResourcesRoot + "/TownSquareAudioProfile.asset");
            townSquare.displayName = "Town Square"; townSquare.primaryAmbience = catalog.beachWind; townSquare.secondaryAmbience = catalog.desertWind; townSquare.primaryVolume = .7f; townSquare.secondaryVolume = .35f;
            EditorUtility.SetDirty(beach); EditorUtility.SetDirty(desert); EditorUtility.SetDirty(townSquare);

            AudioMixer mixer = BuildMixer();
            BuildSettingsPrefab();
            catalog.mixer = mixer;
            catalog.audioSettingsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AudioRoot + "/Prefabs/AudioSettingsPanel.prefab");
            EditorUtility.SetDirty(catalog);
            BuildPrefab(catalog, mixer);
            BuildSandbox();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Super Racing audio catalog and AudioSandbox were rebuilt successfully.");
        }

        public static void BuildFromCommandLine() { Build(); EditorApplication.Exit(0); }
        public static void BuildSettingsPrefabFromCommandLine()
        {
            BuildSettingsPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorApplication.Exit(0);
        }

        public static void AssignGoVoiceFromCommandLine()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AudioCatalog catalog = AssetDatabase.LoadAssetAtPath<AudioCatalog>(ResourcesRoot + "/AudioCatalog.asset");
            AudioClip goVoice = Clip("Race/EVT_Race_StartedGo_VOICE_NORMALIZED_CHOSEN.wav");
            if (catalog == null || goVoice == null) throw new InvalidOperationException("GO voice or AudioCatalog could not be imported.");
            catalog.startedGo = goVoice;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            EditorApplication.Exit(0);
        }

        [MenuItem("Super Racing/Audio/Normalize And Assign GO Voice")]
        public static void NormalizeAndAssignGoVoice()
        {
            const string sourcePath = AudioRoot + "/Clips/Race/EVT_Race_StartedGo_VOICE_CHOSEN.mp3";
            const string outputPath = AudioRoot + "/Clips/Race/EVT_Race_StartedGo_VOICE_NORMALIZED_CHOSEN.wav";
            AudioClip goVoice = NormalizeShortCue(sourcePath, outputPath, .24f, .72f);
            AudioCatalog catalog = AssetDatabase.LoadAssetAtPath<AudioCatalog>(ResourcesRoot + "/AudioCatalog.asset");
            if (catalog == null || goVoice == null) throw new InvalidOperationException("GO voice or AudioCatalog could not be processed.");
            catalog.startedGo = goVoice;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[AudioAudit] GO ready: {goVoice.name}, {goVoice.length:0.000}s, preloaded PCM.");
        }

        public static void NormalizeAndAssignGoVoiceFromCommandLine()
        {
            NormalizeAndAssignGoVoice();
            EditorApplication.Exit(0);
        }

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
            BuildSnapshots(controllerType, controller, groups);
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
        private static void BuildSnapshots(Type controllerType, object controller, System.Collections.Generic.Dictionary<string, object> groups)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            PropertyInfo snapshotsProperty = controllerType.GetProperty("snapshots", flags);
            PropertyInfo targetProperty = controllerType.GetProperty("TargetSnapshot", flags);
            MethodInfo clone = controllerType.GetMethod("CloneNewSnapshotFromTarget", flags);
            Array snapshots = snapshotsProperty?.GetValue(controller) as Array;
            if (snapshots == null || snapshots.Length == 0 || clone == null) return;
            object defaultSnapshot = snapshots.GetValue(0);
            SetGroupVolume(groups["Vehicle"], defaultSnapshot, 10f);
            SetGroupVolume(groups["Collision"], defaultSnapshot, 6f);
            CreateSnapshot("Countdown", new[] { (groups["Vehicle"], 10f), (groups["Collision"], 6f), (groups["Music"], -7f) });
            CreateSnapshot("Paused", new[] { (groups["Vehicle"], 8f), (groups["Collision"], 5f), (groups["Music"], -12f), (groups["SFX"], -6f), (groups["Ambience"], -12f) });
            CreateSnapshot("Finish", new[] { (groups["Vehicle"], 4f), (groups["Collision"], 5f), (groups["Engine"], -12f), (groups["Ambience"], -14f), (groups["Music"], -3f) });
            targetProperty?.SetValue(controller, defaultSnapshot);

            void SetGroupVolume(object group, object snapshot, float db)
            {
                MethodInfo setVolume = group.GetType().GetMethod("SetValueForVolume", flags);
                setVolume?.Invoke(group, new[] { controller, snapshot, (object)db });
            }

            void CreateSnapshot(string name, (object group, float db)[] values)
            {
                targetProperty?.SetValue(controller, defaultSnapshot);
                clone.Invoke(controller, new object[] { false });
                Array updated = snapshotsProperty.GetValue(controller) as Array;
                object snapshot = updated.GetValue(updated.Length - 1);
                ((UnityEngine.Object)snapshot).name = name;
                foreach ((object group, float db) in values)
                    SetGroupVolume(group, snapshot, db);
            }
        }
        private static void ExposeVolume(Type controllerType, object controller, object group, string exposedName)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            try
            {
                object guid = group.GetType().GetProperty("volume", flags)?.GetValue(group)
                    ?? group.GetType().GetMethod("GetGUIDForVolume", flags)?.Invoke(group, null);
                if (guid == null) throw new InvalidOperationException("Could not resolve the group volume GUID.");
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
            AudioClip asphaltRoll = Clip("Vehicle/Surface/LOOP_Surface_AsphaltRoll_REAL_CHOSEN.wav");
            AudioClip asphaltSkid = Clip("Vehicle/Surface/LOOP_Surface_AsphaltSkid_REAL_CHOSEN.wav");
            AudioClip sandRoll = Clip("Vehicle/Surface/LOOP_Surface_SandRoll_REAL_CHOSEN.mp3");
            AudioClip sandSkid = Clip("Vehicle/Surface/LOOP_Surface_SandSkid_REAL_CHOSEN.ogg");
            AudioClip grassRoll = Clip("Vehicle/Surface/LOOP_Surface_GrassRoll_REAL_CHOSEN.ogg");
            AudioClip grassSkid = Clip("Vehicle/Surface/LOOP_Surface_GrassSkid_REAL_CHOSEN.ogg");
            c.asphaltSurface = ConfigureSurface("AsphaltSurfaceProfile", SurfaceType.Asphalt, asphaltRoll, asphaltSkid, .34f, .62f, .3f, 1f);
            c.sandSurface = ConfigureSurface("SandSurfaceProfile", SurfaceType.Sand, sandRoll, sandSkid, .42f, .85f, .32f, .78f);
            c.grassSurface = ConfigureSurface("GrassSurfaceProfile", SurfaceType.Grass, grassRoll, grassSkid, .36f, 1f, .34f, .88f);
        }

        private static VehicleAudioProfile ConfigureVehicle(string asset, string display, AudioCatalog c, AudioClip idle, AudioClip low, AudioClip mid, AudioClip high, AudioClip offLoad, int gears, float maxSpeed, float minPitch, float maxPitch, float volume)
        {
            VehicleAudioProfile p = LoadOrCreate<VehicleAudioProfile>(ResourcesRoot + "/" + asset + ".asset"); p.displayName = display;
            p.engineStart = c.engineStart; p.gearShift = c.gearShift != c.restart ? c.gearShift : null; p.idle = idle; p.lowRpm = low; p.midRpm = mid; p.highRpm = high; p.onLoad = c.accelerationLoad; p.offLoad = offLoad;
            p.gearShiftVariants = new[] { Clip("Vehicle/EVT_Vehicle_GearShift_01_REAL_CHOSEN.wav"), Clip("Vehicle/EVT_Vehicle_GearShift_02_REAL_CHOSEN.wav") };
            p.backfire = Clip("Vehicle/EVT_Vehicle_Backfire_REAL_CHOSEN.mp3");
            p.backfireVariants = p.backfire != null ? new[] { p.backfire } : Array.Empty<AudioClip>();
            p.gearCount = gears; p.maxSpeedKmh = maxSpeed; p.minPitch = minPitch; p.maxPitch = maxPitch;
            p.engineVolume = display == "Balanced" ? .84f : display == "Control" ? .9f : .88f;
            p.loadVolume = display == "Control" ? .42f : display == "Balanced" ? .44f : .46f;
            EditorUtility.SetDirty(p); return p;
        }
        private static SurfaceAudioProfile ConfigureSurface(string asset, SurfaceType type, AudioClip roll, AudioClip skid, float rollVolume, float skidVolume, float threshold, float pitch)
        {
            SurfaceAudioProfile p = LoadOrCreate<SurfaceAudioProfile>(ResourcesRoot + "/" + asset + ".asset"); p.surface = type; p.tireRoll = roll; p.tireSkid = skid;
            p.rollVolume = rollVolume; p.skidVolume = skidVolume; p.skidThreshold = threshold; p.pitchMultiplier = pitch; EditorUtility.SetDirty(p); return p;
        }

        private static void BuildSettingsPrefab()
        {
            GameObject panel = new("AudioSettingsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(AudioSettingsPanel));
            RectTransform rect = panel.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(1920, 1080);
            Image dimmer = panel.GetComponent<Image>(); dimmer.color = new Color(.003f, .009f, .022f, .82f); dimmer.raycastTarget = true;
            AudioSettingsPanel component = panel.GetComponent<AudioSettingsPanel>(); SerializedObject serialized = new(component);

            RoundedRectGraphic glow = CreateRounded(panel.transform, "Card Glow", new Color(0f, .72f, 1f, .13f), 38f);
            SetRect(glow.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, -4f), new Vector2(784f, 724f));
            RoundedRectGraphic shadow = CreateRounded(panel.transform, "Card Shadow", new Color(0f, 0f, 0f, .55f), 34f);
            SetRect(shadow.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, -14f), new Vector2(766f, 706f));

            GameObject card = new("Settings Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(RoundedRectGraphic), typeof(Outline));
            card.transform.SetParent(panel.transform, false);
            RectTransform cardRect = card.GetComponent<RectTransform>(); cardRect.sizeDelta = new Vector2(760, 700);
            RoundedRectGraphic cardGraphic = card.GetComponent<RoundedRectGraphic>(); cardGraphic.Radius = 32f; cardGraphic.color = new Color(.012f, .035f, .065f, .985f);
            Outline cardOutline = card.GetComponent<Outline>(); cardOutline.effectColor = new Color(.05f, .75f, .95f, .48f); cardOutline.effectDistance = new Vector2(1f, -1f);

            RoundedRectGraphic accent = CreateRounded(card.transform, "Top Accent", new Color(.02f, .86f, 1f, 1f), 3f);
            SetRect(accent.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(96f, -30f), new Vector2(128f, 6f));
            RoundedRectGraphic badge = CreateRounded(card.transform, "Audio Badge", new Color(.02f, .72f, .9f, .14f), 12f);
            SetRect(badge.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(116f, -62f), new Vector2(168f, 30f));
            Text badgeText = CreateText(badge.transform, "AUDIO  /  MIXER", Vector2.zero, new Vector2(150f, 28f)); badgeText.fontSize = 12; badgeText.fontStyle = FontStyle.Bold; badgeText.color = new Color(.3f, .9f, 1f);
            Text title = CreateText(card.transform, "AUDIO SETTINGS", new Vector2(-245, 240), new Vector2(430, 58)); title.text = "SOUND & MUSIC"; title.alignment = TextAnchor.MiddleLeft; title.fontSize = 34; title.fontStyle = FontStyle.Bold; title.color = new Color(.9f, .98f, 1f);
            Text subtitle = CreateText(card.transform, "CUSTOMIZE YOUR RACE MIX", new Vector2(-245, 204), new Vector2(430, 28)); subtitle.text = "Balance every layer of your race."; subtitle.alignment = TextAnchor.MiddleLeft; subtitle.fontSize = 14; subtitle.color = new Color(.46f, .68f, .77f);
            RoundedRectGraphic live = CreateRounded(card.transform, "Live Mix", new Color(.05f, .86f, .68f, .14f), 14f);
            SetRect(live.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(268f, 236f), new Vector2(126f, 34f));
            Text liveText = CreateText(live.transform, "●  LIVE MIX", Vector2.zero, new Vector2(112f, 30f)); liveText.fontSize = 12; liveText.fontStyle = FontStyle.Bold; liveText.color = new Color(.18f, 1f, .76f);
            Image separator = CreateImage(card.transform, "Header Separator", new Color(.12f, .32f, .4f, .55f));
            SetRect(separator.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 176f), new Vector2(672f, 1f));

            string[] names = { "Master", "Music", "SFX", "Ambience", "UI" }; string[] fields = { "master", "music", "sfx", "ambience", "ui" };
            for (int i = 0; i < names.Length; i++)
            {
                float y = 132f - i * 72f;
                RoundedRectGraphic row = CreateRounded(card.transform, names[i] + " Row", i == 0 ? new Color(.025f, .13f, .18f, .96f) : new Color(.02f, .075f, .105f, .9f), 14f);
                SetRect(row.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, y), new Vector2(672f, 60f));
                RoundedRectGraphic marker = CreateRounded(row.transform, "Accent", i == 0 ? new Color(.04f, .9f, 1f) : new Color(.08f, .46f, .58f), 2f);
                SetRect(marker.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(10f, 0f), new Vector2(4f, 28f));
                Text label = CreateText(row.transform, names[i].ToUpperInvariant(), new Vector2(-256, 0), new Vector2(120, 36)); label.alignment = TextAnchor.MiddleLeft; label.fontSize = 15; label.fontStyle = FontStyle.Bold; label.color = i == 0 ? new Color(.84f, .98f, 1f) : new Color(.66f, .82f, .88f);
                Slider slider = CreateSlider(row.transform, new Vector2(24, 0));
                RoundedRectGraphic valuePill = CreateRounded(row.transform, "Value Pill", new Color(.02f, .18f, .24f, 1f), 11f);
                SetRect(valuePill.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(278f, 0f), new Vector2(72f, 34f));
                Text value = CreateText(valuePill.transform, "100%", Vector2.zero, new Vector2(68, 30)); value.fontSize = 14; value.fontStyle = FontStyle.Bold; value.color = new Color(.12f, .92f, 1f);
                serialized.FindProperty(fields[i] + "Slider").objectReferenceValue = slider; serialized.FindProperty(fields[i] + "Value").objectReferenceValue = value;
            }

            Text autosave = CreateText(card.transform, "SAVED AUTOMATICALLY", new Vector2(-240, -244), new Vector2(230, 24)); autosave.alignment = TextAnchor.MiddleLeft; autosave.fontSize = 11; autosave.color = new Color(.35f, .58f, .66f);
            Toggle toggle = CreateToggle(card.transform, new Vector2(-302, -290)); Text mute = CreateText(toggle.transform, "MUTE ALL", new Vector2(86, 0), new Vector2(140, 40)); mute.alignment = TextAnchor.MiddleLeft; mute.fontSize = 13; mute.fontStyle = FontStyle.Bold; mute.color = new Color(.67f, .86f, .91f);
            serialized.FindProperty("muteToggle").objectReferenceValue = toggle; serialized.FindProperty("muteLabel").objectReferenceValue = mute;
            Button reset = CreateButton(card.transform, "RESET", new Vector2(118, -290), new Vector2(146, 46), false); UnityEditor.Events.UnityEventTools.AddPersistentListener(reset.onClick, component.ResetDefaults);
            Button close = CreateButton(card.transform, "SAVE & CLOSE", new Vector2(277, -290), new Vector2(172, 46), true); UnityEditor.Events.UnityEventTools.AddPersistentListener(close.onClick, component.RequestClose);
            serialized.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(panel, AudioRoot + "/Prefabs/AudioSettingsPanel.prefab"); UnityEngine.Object.DestroyImmediate(panel);
        }
        private static Text CreateText(Transform parent, string text, Vector2 position, Vector2 size)
        { GameObject go = new(text, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); go.transform.SetParent(parent, false); RectTransform r = go.GetComponent<RectTransform>(); r.anchoredPosition = position; r.sizeDelta = size; Text t = go.GetComponent<Text>(); t.text = text; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.color = Color.white; t.alignment = TextAnchor.MiddleCenter; return t; }
        private static Slider CreateSlider(Transform parent, Vector2 position)
        {
            GameObject go = new("Slider", typeof(RectTransform), typeof(Slider)); go.transform.SetParent(parent, false);
            RectTransform r = go.GetComponent<RectTransform>(); r.anchoredPosition = position; r.sizeDelta = new Vector2(356, 32);
            Slider s = go.GetComponent<Slider>(); s.direction = Slider.Direction.LeftToRight;
            RoundedRectGraphic background = CreateRounded(go.transform, "Track", new Color(.055f, .15f, .19f), 4f);
            SetRect(background.rectTransform, new Vector2(0f, .5f), new Vector2(1f, .5f), Vector2.zero, new Vector2(0f, 8f));
            RoundedRectGraphic fill = CreateRounded(go.transform, "Fill", new Color(.02f, .84f, 1f), 5f);
            SetRect(fill.rectTransform, new Vector2(0f, .5f), new Vector2(1f, .5f), Vector2.zero, new Vector2(0f, 10f));
            RoundedRectGraphic handleGlow = CreateRounded(go.transform, "Handle Glow", new Color(0f, .8f, 1f, .2f), 15f);
            SetRect(handleGlow.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(30f, 30f));
            RoundedRectGraphic handle = CreateRounded(go.transform, "Handle", new Color(.9f, 1f, 1f), 12f);
            SetRect(handle.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(22f, 22f));
            Outline handleOutline = handle.gameObject.AddComponent<Outline>(); handleOutline.effectColor = new Color(0f, .72f, .9f, .9f); handleOutline.effectDistance = new Vector2(2f, -2f);
            s.fillRect = fill.rectTransform; s.handleRect = handle.rectTransform; s.targetGraphic = handle;
            return s;
        }
        private static Toggle CreateToggle(Transform parent, Vector2 position)
        {
            GameObject go = new("Mute Toggle", typeof(RectTransform), typeof(Toggle)); go.transform.SetParent(parent, false);
            RectTransform r = go.GetComponent<RectTransform>(); r.anchoredPosition = position; r.sizeDelta = new Vector2(42, 42);
            RoundedRectGraphic bg = CreateRounded(go.transform, "Background", new Color(.025f, .13f, .17f), 10f); bg.gameObject.AddComponent<Outline>().effectColor = new Color(.04f, .65f, .8f, .8f);
            RoundedRectGraphic check = CreateRounded(go.transform, "Checkmark", new Color(.02f, .86f, 1f), 7f); check.rectTransform.offsetMin = new Vector2(8f, 8f); check.rectTransform.offsetMax = new Vector2(-8f, -8f);
            Toggle t = go.GetComponent<Toggle>(); t.targetGraphic = bg; t.graphic = check; return t;
        }
        private static Button CreateButton(Transform parent, string label, Vector2 position, Vector2 size, bool primary)
        {
            GameObject go = new(label, typeof(RectTransform), typeof(RoundedRectGraphic), typeof(Button), typeof(Outline)); go.transform.SetParent(parent, false);
            RectTransform r = go.GetComponent<RectTransform>(); r.anchoredPosition = position; r.sizeDelta = size;
            RoundedRectGraphic image = go.GetComponent<RoundedRectGraphic>(); image.Radius = 13f; image.color = primary ? new Color(.02f, .82f, .96f) : new Color(.025f, .13f, .18f);
            Outline outline = go.GetComponent<Outline>(); outline.effectColor = new Color(0f, .78f, .92f, .85f); outline.effectDistance = new Vector2(1f, -1f);
            Button button = go.GetComponent<Button>(); ColorBlock colors = button.colors; colors.highlightedColor = new Color(.72f, 1f, 1f); colors.pressedColor = new Color(.45f, .82f, .9f); button.colors = colors;
            Text text = CreateText(go.transform, label, Vector2.zero, size); text.fontStyle = FontStyle.Bold; text.fontSize = 16; text.color = primary ? new Color(.01f, .06f, .08f) : new Color(.72f, .96f, 1f);
            return button;
        }
        private static Image CreateImage(Transform parent, string name, Color color)
        { GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); go.transform.SetParent(parent, false); RectTransform r = go.GetComponent<RectTransform>(); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero; Image image = go.GetComponent<Image>(); image.color = color; return image; }
        private static RoundedRectGraphic CreateRounded(Transform parent, string name, Color color, float radius)
        { GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RoundedRectGraphic)); go.transform.SetParent(parent, false); RectTransform r = go.GetComponent<RectTransform>(); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero; RoundedRectGraphic graphic = go.GetComponent<RoundedRectGraphic>(); graphic.color = color; graphic.Radius = radius; return graphic; }
        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        { rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = size; }


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
            c.tireRoll = Clip("Vehicle/Surface/LOOP_Surface_AsphaltRoll_REAL_CHOSEN.wav"); c.tireSkid = Clip("Vehicle/Surface/LOOP_Surface_AsphaltSkid_REAL_CHOSEN.wav");
            c.collisionLight = Clip("Vehicle/EVT_Vehicle_CollisionLight_TRIMMED_CHOSEN.wav");
            c.collisionMedium = Clip("Vehicle/EVT_Vehicle_CollisionMedium_TRIMMED_CHOSEN.wav");
            c.collisionHeavy = Clip("Vehicle/EVT_Vehicle_CollisionHeavy_TRIMMED_CHOSEN.wav");
            c.collisionLightVariants = new[] { c.collisionLight };
            c.collisionMediumVariants = new[] { c.collisionMedium };
            c.collisionHeavyVariants = new[] { c.collisionHeavy };
            c.respawn = Clip("Vehicle/EVT_Vehicle_Respawn_CHOSEN.ogg"); c.landing = Clip("Vehicle/EVT_Vehicle_Landing_CHOSEN.ogg");
            c.countdownTick = Clip("Race/EVT_Race_CountdownTick_CHOSEN.ogg"); c.startedGo = Clip("Race/EVT_Race_StartedGo_VOICE_NORMALIZED_CHOSEN.wav");
            c.checkpointPassed = Clip("Race/EVT_Race_CheckpointPassed_CHOSEN.ogg"); c.lapChanged = Clip("Race/EVT_Race_LapChanged_CHOSEN.ogg");
            c.finished = Clip("Race/EVT_Race_Finished_CHOSEN.ogg"); c.newRecord = Clip("Race/EVT_Race_NewRecord_CHOSEN.ogg");
            c.invalidCheckpoint = Clip("Race/EVT_Race_InvalidCheckpoint_CHOSEN.ogg"); c.restart = Clip("Race/EVT_Race_Restart_CHOSEN.ogg");
            c.gearShift = Clip("Vehicle/EVT_Vehicle_GearShift_01_REAL_CHOSEN.wav");
            c.uiHover = Clip("UI/EVT_UI_Hover_CHOSEN.ogg"); c.uiClick = Clip("UI/EVT_UI_Click_CHOSEN.ogg");
            c.uiConfirm = Clip("UI/EVT_UI_Confirm_CHOSEN.ogg"); c.uiBack = Clip("UI/EVT_UI_Back_CHOSEN.ogg");
            c.uiSelectionChanged = Clip("UI/EVT_UI_SelectionChanged_CHOSEN.ogg"); c.uiError = Clip("UI/EVT_UI_Error_CHOSEN.ogg");
            c.uiStartRace = Clip("UI/EVT_UI_StartRace_CHOSEN.ogg"); c.uiResultsOpen = Clip("UI/EVT_UI_ResultsOpen_CHOSEN.ogg");
            c.beachWaves = Clip("Ambience/LOOP_Ambience_BeachWaves_NORMALIZED_CHOSEN.wav"); c.beachWind = Clip("Ambience/LOOP_Ambience_BeachWind_NORMALIZED_CHOSEN.wav");
            c.desertWind = Clip("Ambience/LOOP_Ambience_DesertWind_NORMALIZED_CHOSEN.wav"); c.desertSandGust = Clip("Ambience/LOOP_Ambience_DesertSandGust_NORMALIZED_CHOSEN.wav");
            c.raceMusic = Clip("Music/LOOP_Music_Race_CHOSEN.ogg"); c.menuMusic = Clip("Music/LOOP_Music_Menu_CHOSEN.ogg"); c.resultMusic = Clip("Music/LOOP_Music_Result_CHOSEN.ogg");
        }

        private static AudioClip Clip(string relative) => AssetDatabase.LoadAssetAtPath<AudioClip>(AudioRoot + "/Clips/" + relative);

        private static void ConfigureImportSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { AudioRoot + "/Clips" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not AudioImporter importer) continue;
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                bool music = path.Contains("/Music/");
                bool ambience = path.Contains("/Ambience/");
                bool loop = Path.GetFileName(path).StartsWith("LOOP_", StringComparison.OrdinalIgnoreCase);
                settings.loadType = music || ambience ? AudioClipLoadType.Streaming : loop ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = music || ambience || loop ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
                settings.quality = music ? .72f : ambience ? .65f : .8f;
                settings.preloadAudioData = !music && !ambience;
                importer.defaultSampleSettings = settings;
                importer.loadInBackground = music || ambience;
                importer.SaveAndReimport();
            }
        }
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

        [MenuItem("Super Racing/Audio/Trim Collision Clips")]
        public static void TrimCollisionClips()
        {
            const string vehicleRoot = AudioRoot + "/Clips/Vehicle/";
            AudioClip light = TrimClip(vehicleRoot + "EVT_Vehicle_CollisionLight_REALISTIC_CHOSEN.ogg", vehicleRoot + "EVT_Vehicle_CollisionLight_TRIMMED_CHOSEN.wav", .38f);
            AudioClip medium = TrimClip(vehicleRoot + "EVT_Vehicle_CollisionMedium_02_REAL_CHOSEN.wav", vehicleRoot + "EVT_Vehicle_CollisionMedium_TRIMMED_CHOSEN.wav", .48f);
            AudioClip heavy = TrimClip(vehicleRoot + "EVT_Vehicle_CollisionHeavy_REALISTIC_CHOSEN.mp3", vehicleRoot + "EVT_Vehicle_CollisionHeavy_TRIMMED_CHOSEN.wav", .55f);
            AudioCatalog catalog = AssetDatabase.LoadAssetAtPath<AudioCatalog>(ResourcesRoot + "/AudioCatalog.asset");
            if (catalog == null || light == null || medium == null || heavy == null)
                throw new InvalidOperationException("Could not trim every collision clip.");
            catalog.collisionLight = light;
            catalog.collisionMedium = medium;
            catalog.collisionHeavy = heavy;
            catalog.collisionLightVariants = new[] { light };
            catalog.collisionMediumVariants = new[] { medium };
            catalog.collisionHeavyVariants = new[] { heavy };
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[VehicleAudio] Trimmed collisions: {light.length:0.000}s / {medium.length:0.000}s / {heavy.length:0.000}s");
        }

        [MenuItem("Super Racing/Audio/Audit Ambience Loudness")]
        public static void AuditAmbienceLoudness()
        {
            string[] paths =
            {
                AudioRoot + "/Clips/Ambience/LOOP_Ambience_BeachWaves_CHOSEN.flac",
                AudioRoot + "/Clips/Ambience/LOOP_Ambience_BeachWind_CHOSEN.ogg",
                AudioRoot + "/Clips/Ambience/LOOP_Ambience_DesertWind_CHOSEN.ogg",
                AudioRoot + "/Clips/Ambience/EVT_Ambience_DesertSandGust_CHOSEN.wav"
            };
            foreach (string path in paths)
            {
                AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) { Debug.LogError("[AudioAudit] Missing importer " + path); continue; }
                AudioImporterSampleSettings original = importer.defaultSampleSettings;
                bool originalBackground = importer.loadInBackground;
                try
                {
                    AudioImporterSampleSettings decoded = original;
                    decoded.loadType = AudioClipLoadType.DecompressOnLoad;
                    decoded.compressionFormat = AudioCompressionFormat.PCM;
                    decoded.preloadAudioData = true;
                    importer.defaultSampleSettings = decoded;
                    importer.loadInBackground = false;
                    importer.SaveAndReimport();
                    AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    float[] samples = new float[clip.samples * clip.channels];
                    if (!clip.GetData(samples, 0)) { Debug.LogError("[AudioAudit] Could not decode " + path); continue; }
                    double squares = 0d;
                    float peak = 0f;
                    foreach (float sample in samples) { squares += sample * sample; peak = Mathf.Max(peak, Mathf.Abs(sample)); }
                    float rms = Mathf.Sqrt((float)(squares / Mathf.Max(1, samples.Length)));
                    Debug.Log($"[AudioAudit] {clip.name}: {clip.length:0.00}s RMS={rms:0.0000} peak={peak:0.0000}");
                }
                finally
                {
                    importer = AssetImporter.GetAtPath(path) as AudioImporter;
                    if (importer != null)
                    {
                        importer.defaultSampleSettings = original;
                        importer.loadInBackground = originalBackground;
                        importer.SaveAndReimport();
                    }
                }
            }
        }

        [MenuItem("Super Racing/Audio/Audit Tire Skid Loudness")]
        public static void AuditTireSkidLoudness()
        {
            string[] paths =
            {
                AudioRoot + "/Clips/Vehicle/Surface/LOOP_Surface_AsphaltSkid_REAL_CHOSEN.wav",
                AudioRoot + "/Clips/Vehicle/Surface/LOOP_Surface_SandSkid_REAL_CHOSEN.ogg",
                AudioRoot + "/Clips/Vehicle/Surface/LOOP_Surface_GrassSkid_REAL_CHOSEN.ogg"
            };
            foreach (string path in paths) AuditImportedClip(path, "TireAudit");
        }

        private static void AuditImportedClip(string path, string label)
        {
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) { Debug.LogError($"[{label}] Missing importer {path}"); return; }
            AudioImporterSampleSettings original = importer.defaultSampleSettings;
            bool originalBackground = importer.loadInBackground;
            try
            {
                AudioImporterSampleSettings decoded = original;
                decoded.loadType = AudioClipLoadType.DecompressOnLoad;
                decoded.compressionFormat = AudioCompressionFormat.PCM;
                decoded.preloadAudioData = true;
                importer.defaultSampleSettings = decoded;
                importer.loadInBackground = false;
                importer.SaveAndReimport();
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                float[] samples = new float[clip.samples * clip.channels];
                if (!clip.GetData(samples, 0)) { Debug.LogError($"[{label}] Could not decode {path}"); return; }
                double squares = 0d;
                float peak = 0f;
                foreach (float sample in samples) { squares += sample * sample; peak = Mathf.Max(peak, Mathf.Abs(sample)); }
                float rms = Mathf.Sqrt((float)(squares / Mathf.Max(1, samples.Length)));
                Debug.Log($"[{label}] {clip.name}: {clip.length:0.00}s RMS={rms:0.0000} peak={peak:0.0000}");
            }
            finally
            {
                importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer != null)
                {
                    importer.defaultSampleSettings = original;
                    importer.loadInBackground = originalBackground;
                    importer.SaveAndReimport();
                }
            }
        }

        [MenuItem("Super Racing/Audio/Normalize Ambience Clips")]
        public static void NormalizeAmbienceClips()
        {
            const string ambienceRoot = AudioRoot + "/Clips/Ambience/";
            AudioClip beachWaves = NormalizeAmbienceClip(ambienceRoot + "LOOP_Ambience_BeachWaves_CHOSEN.flac", ambienceRoot + "LOOP_Ambience_BeachWaves_NORMALIZED_CHOSEN.wav", .09f);
            AudioClip beachWind = NormalizeAmbienceClip(ambienceRoot + "LOOP_Ambience_BeachWind_CHOSEN.ogg", ambienceRoot + "LOOP_Ambience_BeachWind_NORMALIZED_CHOSEN.wav", .08f);
            AudioClip desertWind = NormalizeAmbienceClip(ambienceRoot + "LOOP_Ambience_DesertWind_CHOSEN.ogg", ambienceRoot + "LOOP_Ambience_DesertWind_NORMALIZED_CHOSEN.wav", .08f);
            AudioClip desertGust = NormalizeAmbienceClip(ambienceRoot + "EVT_Ambience_DesertSandGust_CHOSEN.wav", ambienceRoot + "LOOP_Ambience_DesertSandGust_NORMALIZED_CHOSEN.wav", .07f);
            AudioCatalog catalog = AssetDatabase.LoadAssetAtPath<AudioCatalog>(ResourcesRoot + "/AudioCatalog.asset");
            MapAudioProfile beach = AssetDatabase.LoadAssetAtPath<MapAudioProfile>(ResourcesRoot + "/BeachAudioProfile.asset");
            MapAudioProfile desert = AssetDatabase.LoadAssetAtPath<MapAudioProfile>(ResourcesRoot + "/DesertAudioProfile.asset");
            if (catalog == null || beach == null || desert == null) throw new InvalidOperationException("Ambience catalog/profile assets are missing.");
            catalog.beachWaves = beachWaves; catalog.beachWind = beachWind; catalog.desertWind = desertWind; catalog.desertSandGust = desertGust;
            beach.primaryAmbience = beachWaves; beach.secondaryAmbience = beachWind;
            desert.primaryAmbience = desertWind; desert.secondaryAmbience = desertGust;
            EditorUtility.SetDirty(catalog); EditorUtility.SetDirty(beach); EditorUtility.SetDirty(desert);
            AssetDatabase.SaveAssets();
            Debug.Log("[AudioAudit] Normalized ambience clips created and assigned.");
        }

        private static AudioClip NormalizeAmbienceClip(string sourcePath, string outputPath, float targetRms)
        {
            AudioImporter sourceImporter = AssetImporter.GetAtPath(sourcePath) as AudioImporter;
            if (sourceImporter == null) throw new FileNotFoundException("Ambience source importer not found", sourcePath);
            AudioImporterSampleSettings original = sourceImporter.defaultSampleSettings;
            bool originalBackground = sourceImporter.loadInBackground;
            float[] samples;
            int channels;
            int frequency;
            try
            {
                AudioImporterSampleSettings decoded = original;
                decoded.loadType = AudioClipLoadType.DecompressOnLoad;
                decoded.compressionFormat = AudioCompressionFormat.PCM;
                decoded.preloadAudioData = true;
                sourceImporter.defaultSampleSettings = decoded;
                sourceImporter.loadInBackground = false;
                sourceImporter.SaveAndReimport();
                AudioClip source = AssetDatabase.LoadAssetAtPath<AudioClip>(sourcePath);
                samples = new float[source.samples * source.channels];
                if (!source.GetData(samples, 0)) throw new InvalidOperationException("Could not decode " + sourcePath);
                channels = source.channels;
                frequency = source.frequency;
            }
            finally
            {
                sourceImporter = AssetImporter.GetAtPath(sourcePath) as AudioImporter;
                if (sourceImporter != null)
                {
                    sourceImporter.defaultSampleSettings = original;
                    sourceImporter.loadInBackground = originalBackground;
                    sourceImporter.SaveAndReimport();
                }
            }

            double squares = 0d;
            float peak = 0f;
            foreach (float sample in samples) { squares += sample * sample; peak = Mathf.Max(peak, Mathf.Abs(sample)); }
            float rms = Mathf.Sqrt((float)(squares / Mathf.Max(1, samples.Length)));
            float gain = rms > .00001f ? targetRms / rms : 1f;
            if (peak > .00001f) gain = Mathf.Min(gain, .9f / peak);
            for (int i = 0; i < samples.Length; i++) samples[i] = Mathf.Clamp(samples[i] * gain, -1f, 1f);
            WritePcmWave(outputPath, samples, channels, frequency);
            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AudioImporter outputImporter = AssetImporter.GetAtPath(outputPath) as AudioImporter;
            if (outputImporter != null)
            {
                AudioImporterSampleSettings outputSettings = outputImporter.defaultSampleSettings;
                outputSettings.loadType = AudioClipLoadType.Streaming;
                outputSettings.compressionFormat = AudioCompressionFormat.Vorbis;
                outputSettings.quality = .8f;
                outputSettings.preloadAudioData = false;
                outputImporter.defaultSampleSettings = outputSettings;
                outputImporter.loadInBackground = true;
                outputImporter.SaveAndReimport();
            }
            Debug.Log($"[AudioAudit] Normalized {Path.GetFileName(sourcePath)} gain={gain:0.00}x RMS {rms:0.0000}->{targetRms:0.0000}");
            return AssetDatabase.LoadAssetAtPath<AudioClip>(outputPath);
        }

        private static AudioClip TrimClip(string sourcePath, string outputPath, float maximumSeconds)
        {
            AudioClip source = AssetDatabase.LoadAssetAtPath<AudioClip>(sourcePath);
            if (source == null) throw new FileNotFoundException("Collision source clip not found", sourcePath);
            source.LoadAudioData();
            float[] input = new float[source.samples * source.channels];
            if (!source.GetData(input, 0)) throw new InvalidOperationException("Could not decode " + sourcePath);

            float peak = 0f;
            for (int i = 0; i < input.Length; i++) peak = Mathf.Max(peak, Mathf.Abs(input[i]));
            float threshold = Mathf.Max(.0025f, peak * .025f);
            int firstFrame = 0;
            int lastFrame = source.samples - 1;
            bool found = false;
            for (int frame = 0; frame < source.samples && !found; frame++)
                for (int channel = 0; channel < source.channels; channel++)
                    if (Mathf.Abs(input[frame * source.channels + channel]) >= threshold) { firstFrame = frame; found = true; break; }
            found = false;
            for (int frame = source.samples - 1; frame >= firstFrame && !found; frame--)
                for (int channel = 0; channel < source.channels; channel++)
                    if (Mathf.Abs(input[frame * source.channels + channel]) >= threshold) { lastFrame = frame; found = true; break; }

            int preRoll = Mathf.RoundToInt(source.frequency * .006f);
            int postRoll = Mathf.RoundToInt(source.frequency * .018f);
            firstFrame = Mathf.Max(0, firstFrame - preRoll);
            int maxFrames = Mathf.Max(1, Mathf.RoundToInt(maximumSeconds * source.frequency));
            int frameCount = Mathf.Min(maxFrames, Mathf.Min(source.samples - firstFrame, lastFrame - firstFrame + 1 + postRoll));
            float[] output = new float[frameCount * source.channels];
            Array.Copy(input, firstFrame * source.channels, output, 0, output.Length);

            float outputPeak = 0f;
            for (int i = 0; i < output.Length; i++) outputPeak = Mathf.Max(outputPeak, Mathf.Abs(output[i]));
            float gain = outputPeak > .0001f ? Mathf.Min(4f, .9f / outputPeak) : 1f;
            int fadeInFrames = Mathf.Min(frameCount / 4, Mathf.RoundToInt(source.frequency * .003f));
            int fadeOutFrames = Mathf.Min(frameCount / 3, Mathf.RoundToInt(source.frequency * .035f));
            for (int frame = 0; frame < frameCount; frame++)
            {
                float envelope = 1f;
                if (fadeInFrames > 0 && frame < fadeInFrames) envelope *= frame / (float)fadeInFrames;
                int remaining = frameCount - 1 - frame;
                if (fadeOutFrames > 0 && remaining < fadeOutFrames) envelope *= remaining / (float)fadeOutFrames;
                for (int channel = 0; channel < source.channels; channel++)
                    output[frame * source.channels + channel] = Mathf.Clamp(output[frame * source.channels + channel] * gain * envelope, -1f, 1f);
            }

            WritePcmWave(outputPath, output, source.channels, source.frequency);
            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AudioImporter importer = AssetImporter.GetAtPath(outputPath) as AudioImporter;
            if (importer != null)
            {
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
                settings.preloadAudioData = true;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(outputPath);
        }

        private static AudioClip NormalizeShortCue(string sourcePath, string outputPath, float targetRms, float maximumSeconds)
        {
            AudioImporter sourceImporter = AssetImporter.GetAtPath(sourcePath) as AudioImporter;
            if (sourceImporter == null) throw new FileNotFoundException("Short cue source importer not found", sourcePath);

            AudioImporterSampleSettings original = sourceImporter.defaultSampleSettings;
            bool originalBackground = sourceImporter.loadInBackground;
            float[] input;
            int sourceSamples;
            int channels;
            int frequency;
            try
            {
                AudioImporterSampleSettings decoded = original;
                decoded.loadType = AudioClipLoadType.DecompressOnLoad;
                decoded.compressionFormat = AudioCompressionFormat.PCM;
                decoded.preloadAudioData = true;
                sourceImporter.defaultSampleSettings = decoded;
                sourceImporter.loadInBackground = false;
                sourceImporter.SaveAndReimport();

                AudioClip source = AssetDatabase.LoadAssetAtPath<AudioClip>(sourcePath);
                source.LoadAudioData();
                sourceSamples = source.samples;
                channels = source.channels;
                frequency = source.frequency;
                input = new float[sourceSamples * channels];
                if (!source.GetData(input, 0)) throw new InvalidOperationException("Could not decode " + sourcePath);
            }
            finally
            {
                sourceImporter = AssetImporter.GetAtPath(sourcePath) as AudioImporter;
                if (sourceImporter != null)
                {
                    sourceImporter.defaultSampleSettings = original;
                    sourceImporter.loadInBackground = originalBackground;
                    sourceImporter.SaveAndReimport();
                }
            }

            float inputPeak = 0f;
            for (int i = 0; i < input.Length; i++) inputPeak = Mathf.Max(inputPeak, Mathf.Abs(input[i]));
            float threshold = Mathf.Max(.003f, inputPeak * .018f);
            int firstFrame = 0;
            int lastFrame = sourceSamples - 1;
            bool found = false;
            for (int frame = 0; frame < sourceSamples && !found; frame++)
                for (int channel = 0; channel < channels; channel++)
                    if (Mathf.Abs(input[frame * channels + channel]) >= threshold) { firstFrame = frame; found = true; break; }
            found = false;
            for (int frame = sourceSamples - 1; frame >= firstFrame && !found; frame--)
                for (int channel = 0; channel < channels; channel++)
                    if (Mathf.Abs(input[frame * channels + channel]) >= threshold) { lastFrame = frame; found = true; break; }

            firstFrame = Mathf.Max(0, firstFrame - Mathf.RoundToInt(frequency * .008f));
            lastFrame = Mathf.Min(sourceSamples - 1, lastFrame + Mathf.RoundToInt(frequency * .025f));
            int maxFrames = Mathf.Max(1, Mathf.RoundToInt(maximumSeconds * frequency));
            int frameCount = Mathf.Min(maxFrames, lastFrame - firstFrame + 1);
            float[] output = new float[frameCount * channels];
            Array.Copy(input, firstFrame * channels, output, 0, output.Length);

            double squares = 0d;
            float outputPeak = 0f;
            foreach (float sample in output) { squares += sample * sample; outputPeak = Mathf.Max(outputPeak, Mathf.Abs(sample)); }
            float inputRms = Mathf.Sqrt((float)(squares / Mathf.Max(1, output.Length)));
            float gain = inputRms > .00001f ? targetRms / inputRms : 1f;
            if (outputPeak > .00001f) gain = Mathf.Min(gain, .92f / outputPeak);
            int fadeInFrames = Mathf.Min(frameCount / 4, Mathf.RoundToInt(frequency * .002f));
            int fadeOutFrames = Mathf.Min(frameCount / 3, Mathf.RoundToInt(frequency * .018f));
            for (int frame = 0; frame < frameCount; frame++)
            {
                float envelope = 1f;
                if (fadeInFrames > 0 && frame < fadeInFrames) envelope *= frame / (float)fadeInFrames;
                int remaining = frameCount - 1 - frame;
                if (fadeOutFrames > 0 && remaining < fadeOutFrames) envelope *= remaining / (float)fadeOutFrames;
                for (int channel = 0; channel < channels; channel++)
                    output[frame * channels + channel] = Mathf.Clamp(output[frame * channels + channel] * gain * envelope, -1f, 1f);
            }

            WritePcmWave(outputPath, output, channels, frequency);
            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AudioImporter outputImporter = AssetImporter.GetAtPath(outputPath) as AudioImporter;
            if (outputImporter != null)
            {
                AudioImporterSampleSettings settings = outputImporter.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
                settings.preloadAudioData = true;
                outputImporter.defaultSampleSettings = settings;
                outputImporter.loadInBackground = false;
                outputImporter.forceToMono = true;
                outputImporter.SaveAndReimport();
            }

            AudioClip result = AssetDatabase.LoadAssetAtPath<AudioClip>(outputPath);
            float[] verified = new float[result.samples * result.channels];
            if (!result.GetData(verified, 0)) throw new InvalidOperationException("Could not verify " + outputPath);
            double verifiedSquares = 0d;
            float verifiedPeak = 0f;
            foreach (float sample in verified) { verifiedSquares += sample * sample; verifiedPeak = Mathf.Max(verifiedPeak, Mathf.Abs(sample)); }
            float verifiedRms = Mathf.Sqrt((float)(verifiedSquares / Mathf.Max(1, verified.Length)));
            Debug.Log($"[AudioAudit] GO loudness: source peak={inputPeak:0.000}, trimmed RMS={inputRms:0.000}; output peak={verifiedPeak:0.000}, RMS={verifiedRms:0.000}, gain={gain:0.00}x");
            return result;
        }

        private static void WritePcmWave(string assetPath, float[] samples, int channels, int rate)
        {
            string fullPath = Path.GetFullPath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            using BinaryWriter writer = new(File.Create(fullPath));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples.Length * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1); writer.Write((short)channels);
            writer.Write(rate); writer.Write(rate * channels * 2); writer.Write((short)(channels * 2)); writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples.Length * 2);
            foreach (float sample in samples) writer.Write((short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
        }

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
