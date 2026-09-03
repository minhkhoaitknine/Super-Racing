using System.Collections.Generic;
using SuperRacing.Data;
using SuperRacing.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SuperRacing.EditorTools
{
    public static class TrackSelectionSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/TrackSelection.unity";
        private const string CatalogPath = "Assets/Data/GameCatalog.asset";
        private const string RenderTexturePath = "Assets/UI/TrackSelection/TrackPreview.renderTexture";
        private const string BackgroundPath = "Assets/UI/Lobby/Lobby_Background.png";
        private const string PanelPath = "Assets/UI/Garage/GlassPanel_Bg.png";
        private const string SelectedPanelPath = "Assets/UI/Garage/GlassPanel_Selected.png";
        private const string PrimaryButtonPath = "Assets/UI/Garage/GlassButton_Primary.png";
        private const string NormalButtonPath = "Assets/UI/Garage/GlassButton_Normal.png";

        private static Canvas canvas;
        private static Font font;
        private static Sprite panelSprite;
        private static Sprite selectedPanelSprite;
        private static Sprite primaryButtonSprite;
        private static Sprite normalButtonSprite;

        [MenuItem("Super Racing/Build Track Selection Scene")]
        public static void Build()
        {
            ConfigureTrackPreview("Assets/Data/BeachTrack.asset", "Assets/Game/Prefabs/Maps/BeachMap.prefab");
            ConfigureTrackPreview("Assets/Data/DesertTrack.asset", "Assets/Game/Prefabs/Maps/DesertMap.prefab");
            ConfigureTrackPreview("Assets/Data/TownSquareTrack.asset", "Assets/Game/Prefabs/Maps/TownSquareMap.prefab");
            GameCatalog catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogPath);
            if (catalog == null || catalog.Tracks.Count == 0)
            {
                Debug.LogError("Track Selection requires GameCatalog with at least one track.");
                return;
            }

            EnsureFolder();
            LoadSprites();
            RenderTexture previewTexture = GetOrCreateRenderTexture();
            int previewLayer = LayerMask.NameToLayer("GaragePreview");
            if (previewLayer < 0)
            {
                Debug.LogError("Create the GaragePreview layer before building Track Selection.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateDisplayCamera();
            Transform previewRoot = CreatePreviewStage(previewTexture, previewLayer);
            CreateInterface(catalog, previewTexture, previewRoot);
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureBuildSettings();
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = canvas.gameObject;
            Debug.Log("Track Selection scene rebuilt with interactive 3D map preview.");
        }

        private static void CreateDisplayCamera()
        {
            var cameraObject = new GameObject("Track Selection Display Camera", typeof(Camera), typeof(AudioListener));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = 0;
            camera.depth = -100f;
            camera.targetDisplay = 0;
        }

        private static void ConfigureTrackPreview(string definitionPath, string prefabPath)
        {
            TrackDefinition definition = AssetDatabase.LoadAssetAtPath<TrackDefinition>(definitionPath);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (definition == null || prefab == null)
            {
                Debug.LogError($"Cannot configure track preview: {definitionPath} / {prefabPath}");
                return;
            }

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("previewPrefab").objectReferenceValue = prefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/UI/TrackSelection"))
            {
                AssetDatabase.CreateFolder("Assets/UI", "TrackSelection");
            }
        }

        private static void LoadSprites()
        {
            panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelPath);
            selectedPanelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SelectedPanelPath);
            primaryButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PrimaryButtonPath);
            normalButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(NormalButtonPath);
        }

        private static RenderTexture GetOrCreateRenderTexture()
        {
            RenderTexture texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
            if (texture != null)
            {
                return texture;
            }

            texture = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32)
            {
                name = "TrackPreview",
                antiAliasing = 4,
                useMipMap = false
            };
            AssetDatabase.CreateAsset(texture, RenderTexturePath);
            return texture;
        }

        private static Transform CreatePreviewStage(RenderTexture texture, int layer)
        {
            var root = new GameObject("Track Preview Root");
            root.layer = layer;

            var cameraObject = new GameObject("Track Preview Camera");
            cameraObject.transform.position = new Vector3(0f, 8.5f, 10.5f);
            cameraObject.transform.LookAt(new Vector3(0f, 0.5f, 0f));
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.008f, 0.018f, 0.045f, 0f);
            camera.cullingMask = 1 << layer;
            camera.fieldOfView = 38f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.targetTexture = texture;

            CreateLight("Track Preview Key", layer, 2.2f, new Color(0.82f, 0.94f, 1f), new Vector3(42f, -35f, 0f));
            CreateLight("Track Preview Fill", layer, 1.2f, new Color(0.15f, 0.65f, 1f), new Vector3(20f, 145f, 0f));
            return root.transform;
        }

        private static void CreateLight(string name, int layer, float intensity, Color color, Vector3 rotation)
        {
            var target = new GameObject(name);
            target.layer = layer;
            target.transform.rotation = Quaternion.Euler(rotation);
            Light light = target.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = color;
            light.cullingMask = 1 << layer;
        }

        private static void CreateInterface(GameCatalog catalog, RenderTexture previewTexture, Transform previewRoot)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject = new GameObject("Track Selection Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Image background = CreateImage("Background", canvas.transform, Color.white);
            background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
            background.preserveAspect = false;
            background.raycastTarget = false;
            Stretch(background.rectTransform);

            Image shade = CreateImage("Dark Overlay", canvas.transform, new Color(0.005f, 0.015f, 0.04f, 0.60f));
            shade.raycastTarget = false;
            Stretch(shade.rectTransform);

            Image topBar = CreatePanel("Top Bar", canvas.transform, new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(1880f, 76f), false);
            Text heading = CreateText("Heading", topBar.transform, "SELECT TRACK", 30, TextAnchor.MiddleCenter, new Color(0.2f, 0.95f, 1f));
            heading.fontStyle = FontStyle.Bold;
            Stretch(heading.rectTransform);

            Button back = CreateButton("Back", "‹  GARAGE", topBar.transform, new Vector2(0f, 0.5f), new Vector2(110f, 0f), new Vector2(190f, 48f), false);

            Image previewFrame = CreatePanel("3D Map Preview", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(-120f, -40f), new Vector2(1260f, 820f), false);
            RawImage preview = CreateUIObject("Map Render", previewFrame.transform).AddComponent<RawImage>();
            preview.texture = previewTexture;
            preview.color = Color.white;
            preview.raycastTarget = true;
            preview.rectTransform.anchorMin = Vector2.zero;
            preview.rectTransform.anchorMax = Vector2.one;
            preview.rectTransform.offsetMin = new Vector2(18f, 18f);
            preview.rectTransform.offsetMax = new Vector2(-18f, -18f);
            TrackPreviewRotator rotator = preview.gameObject.AddComponent<TrackPreviewRotator>();
            rotator.Configure(previewRoot);

            Image listPanel = CreatePanel("Track List", canvas.transform, new Vector2(0f, 0.5f), new Vector2(205f, -40f), new Vector2(330f, 820f), false);
            Text listTitle = CreateText("List Title", listPanel.transform, "AVAILABLE TRACKS", 18, TextAnchor.MiddleCenter, new Color(0.3f, 0.9f, 1f));
            listTitle.fontStyle = FontStyle.Bold;
            SetRect(listTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(280f, 36f), new Vector2(0.5f, 0.5f));

            var trackButtons = new List<Button>();
            var trackCards = new List<Image>();
            for (int index = 0; index < catalog.Tracks.Count; index++)
            {
                TrackDefinition track = catalog.Tracks[index];
                Image card = CreatePanel($"Track {index + 1:00}", listPanel.transform, new Vector2(0.5f, 1f), new Vector2(0f, -120f - index * 142f), new Vector2(286f, 112f), index == 0);
                trackCards.Add(card);
                Button button = card.gameObject.AddComponent<Button>();
                Text number = CreateText("Number", card.transform, $"{index + 1:00}", 16, TextAnchor.UpperLeft, new Color(0.2f, 0.9f, 1f));
                SetRect(number.rectTransform, new Vector2(0f, 1f), new Vector2(16f, -12f), new Vector2(50f, 24f), new Vector2(0f, 1f));
                Text name = CreateText("Name", card.transform, track.DisplayName.ToUpperInvariant(), 22, TextAnchor.MiddleLeft, Color.white);
                name.fontStyle = FontStyle.Bold;
                SetRect(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(16f, -5f), new Vector2(250f, 38f), new Vector2(0f, 0.5f));
                Text laps = CreateText("Laps", card.transform, $"{track.LapCount} LAPS", 13, TextAnchor.LowerLeft, new Color(0.75f, 0.85f, 0.95f));
                SetRect(laps.rectTransform, new Vector2(0f, 0f), new Vector2(16f, 10f), new Vector2(220f, 24f), new Vector2(0f, 0f));
                trackButtons.Add(button);
            }

            Image infoPanel = CreatePanel("Track Information", canvas.transform, new Vector2(1f, 0.5f), new Vector2(-205f, -40f), new Vector2(330f, 820f), false);
            Text infoTitle = CreateText("Info Title", infoPanel.transform, "TRACK DATA", 18, TextAnchor.MiddleCenter, new Color(0.3f, 0.9f, 1f));
            infoTitle.fontStyle = FontStyle.Bold;
            SetRect(infoTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(280f, 36f), new Vector2(0.5f, 0.5f));

            Text trackName = CreateText("Track Name", infoPanel.transform, catalog.Tracks[0].DisplayName.ToUpperInvariant(), 30, TextAnchor.MiddleCenter, Color.white);
            trackName.fontStyle = FontStyle.Bold;
            SetRect(trackName.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -115f), new Vector2(280f, 54f), new Vector2(0.5f, 0.5f));
            Text lapCount = CreateInfoRow(infoPanel.transform, "LAPS", catalog.Tracks[0].LapCount.ToString(), -205f);
            Text record = CreateInfoRow(infoPanel.transform, "PERSONAL BEST", "--:--.---", -300f);
            Text hint = CreateText("Rotate Hint", infoPanel.transform, "DRAG MAP TO ROTATE 360°", 13, TextAnchor.MiddleCenter, new Color(0.6f, 0.8f, 0.9f));
            SetRect(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(280f, 32f), new Vector2(0.5f, 0.5f));
            Button start = CreateButton("Start Race", "START RACE  ▶", infoPanel.transform, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(280f, 70f), true);

            var controllerObject = new GameObject("Track Selection Controller");
            TrackSelectionUI controller = controllerObject.AddComponent<TrackSelectionUI>();
            controller.Configure(catalog, trackName, lapCount, record, previewRoot, 10f, "Garage", trackCards, panelSprite, selectedPanelSprite);

            UnityEventTools.AddPersistentListener(back.onClick, controller.ReturnToGarage);
            UnityEventTools.AddPersistentListener(start.onClick, controller.StartRace);
            for (int index = 0; index < trackButtons.Count; index++)
            {
                UnityEventTools.AddIntPersistentListener(trackButtons[index].onClick, controller.SelectTrack, index);
            }
        }

        private static Text CreateInfoRow(Transform parent, string label, string value, float y)
        {
            Image row = CreatePanel(label, parent, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(280f, 72f), false);
            Text title = CreateText("Label", row.transform, label, 12, TextAnchor.UpperLeft, new Color(0.45f, 0.78f, 0.95f));
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(14f, -8f), new Vector2(250f, 22f), new Vector2(0f, 1f));
            Text valueText = CreateText("Value", row.transform, value, 22, TextAnchor.LowerLeft, Color.white);
            valueText.fontStyle = FontStyle.Bold;
            SetRect(valueText.rectTransform, new Vector2(0f, 0f), new Vector2(14f, 8f), new Vector2(250f, 32f), new Vector2(0f, 0f));
            return valueText;
        }

        private static Image CreatePanel(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, bool selected)
        {
            Image image = CreateImage(name, parent, new Color(0.12f, 0.2f, 0.34f, 1f));
            image.sprite = selected ? selectedPanelSprite : panelSprite;
            image.type = Image.Type.Sliced;
            SetRect(image.rectTransform, anchor, position, size, new Vector2(0.5f, 0.5f));
            return image;
        }

        private static Button CreateButton(string name, string label, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, bool primary)
        {
            Image image = CreateImage(name, parent, Color.white);
            image.sprite = primary ? primaryButtonSprite : normalButtonSprite;
            image.type = Image.Type.Sliced;
            SetRect(image.rectTransform, anchor, position, size, new Vector2(0.5f, 0.5f));
            Button button = image.gameObject.AddComponent<Button>();
            Text textLabel = CreateText("Label", image.transform, label, 18, TextAnchor.MiddleCenter, Color.white);
            textLabel.fontStyle = FontStyle.Bold;
            textLabel.raycastTarget = false;
            Stretch(textLabel.rectTransform);
            return button;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            Image image = CreateUIObject(name, parent).AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(string name, Transform parent, string value, int size, TextAnchor alignment, Color color)
        {
            Text text = CreateUIObject(name, parent).AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var target = new GameObject(name, typeof(RectTransform));
            target.transform.SetParent(parent, false);
            return target;
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(item => item.path == ScenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }
    }
}
