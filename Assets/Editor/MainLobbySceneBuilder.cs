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
    public static class MainLobbySceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string CatalogPath = "Assets/Data/GameCatalog.asset";
        private const string RenderTexturePath = "Assets/UI/Lobby/LobbyPreview.renderTexture";
        private const string PanelSpritePath = "Assets/UI/Garage/GlassPanel_Bg.png";
        private const string PrimarySpritePath = "Assets/UI/Garage/GlassButton_Primary.png";
        private const string BackgroundPath = "Assets/UI/Lobby/Lobby_Background.png";
        private const string ShadowPath = "Assets/UI/Garage/CarShadow.png";

        private static Canvas canvas;
        private static Font font;
        private static Sprite panelSprite;
        private static Sprite primarySprite;

        [MenuItem("Super Racing/Build Main Lobby Scene")]
        public static void Build()
        {
            EnsureFolder();
            EnsureSprite(BackgroundPath, false);
            EnsureSprite(ShadowPath, true);
            GameCatalog catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogPath);
            if (catalog == null || catalog.Cars.Count == 0 || catalog.Cars[0].VehiclePrefab == null)
            {
                Debug.LogError("A catalog car with a vehicle prefab is required to build the lobby.");
                return;
            }

            int layer = LayerMask.NameToLayer("GaragePreview");
            if (layer < 0)
            {
                Debug.LogError("GaragePreview layer is required to build the lobby.");
                return;
            }

            panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelSpritePath);
            primarySprite = AssetDatabase.LoadAssetAtPath<Sprite>(PrimarySpritePath);
            RenderTexture previewTexture = GetOrCreateRenderTexture();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Transform carRoot = CreateShowroom(catalog.Cars[0], previewTexture, layer);
            CreateInterface(previewTexture, carRoot);
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            EditorSceneManager.SaveScene(scene, ScenePath);
            UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = canvas.gameObject;
            Debug.Log("Main lobby scene created successfully.");
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/UI/Lobby"))
            {
                AssetDatabase.CreateFolder("Assets/UI", "Lobby");
            }
        }

        private static void EnsureSprite(string path, bool alphaIsTransparency)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = alphaIsTransparency;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
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
                name = "LobbyPreview",
                antiAliasing = 4,
                useMipMap = false
            };
            AssetDatabase.CreateAsset(texture, RenderTexturePath);
            return texture;
        }

        private static Transform CreateShowroom(CarDefinition car, RenderTexture texture, int layer)
        {
            var stage = new GameObject("Lobby Preview Stage");
            SetLayerRecursively(stage, layer);

            var carRoot = new GameObject("Interactive Vehicle Root").transform;
            carRoot.SetParent(stage.transform, false);
            carRoot.localRotation = Quaternion.Euler(0f, 8f, 0f);
            GameObject vehicle = (GameObject)PrefabUtility.InstantiatePrefab(car.VehiclePrefab);
            vehicle.name = car.DisplayName;
            vehicle.transform.SetParent(carRoot, false);
            DisableVehicleBehaviour(vehicle);
            SetLayerRecursively(vehicle, layer);
            FitVehicle(vehicle, 4.2f, 0.02f);

            var cameraObject = new GameObject("Lobby Preview Camera");
            Camera previewCamera = cameraObject.AddComponent<Camera>();
            cameraObject.transform.position = new Vector3(0f, 1.45f, 7.2f);
            cameraObject.transform.LookAt(new Vector3(0f, 0.35f, 0f));
            previewCamera.fieldOfView = 34f;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            previewCamera.cullingMask = 1 << layer;
            previewCamera.targetTexture = texture;
            previewCamera.allowHDR = true;

            CreateLight("Lobby Key Light", layer, 2.8f, new Color(0.82f, 0.94f, 1f), new Vector3(32f, -34f, 0f));
            CreateLight("Lobby Fill Light", layer, 1.5f, new Color(1f, 0.28f, 0.58f), new Vector3(18f, 145f, 0f));
            CreateLight("Lobby Rim Light", layer, 2.2f, new Color(0.05f, 0.85f, 1f), new Vector3(-18f, 20f, 0f));
            return carRoot;
        }

        private static void CreateInterface(RenderTexture previewTexture, Transform carRoot)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject = new GameObject("Lobby Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Image background = CreateImage("Lobby Background", canvas.transform, Color.white);
            background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
            background.type = Image.Type.Simple;
            background.preserveAspect = false;
            Stretch(background.rectTransform);

            Image shade = CreateImage("Lobby Shade", canvas.transform, new Color(0.01f, 0.02f, 0.06f, 0.36f));
            Stretch(shade.rectTransform);

            Image contactShadow = CreateImage("Vehicle Contact Shadow", canvas.transform, new Color(0f, 0f, 0f, 0.72f));
            contactShadow.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShadowPath);
            contactShadow.preserveAspect = false;
            contactShadow.raycastTarget = false;
            SetRect(contactShadow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -165f), new Vector2(540f, 130f), new Vector2(0.5f, 0.5f));

            RawImage preview = CreateUIObject("Vehicle Preview", canvas.transform).AddComponent<RawImage>();
            preview.texture = previewTexture;
            preview.raycastTarget = false;
            Stretch(preview.rectTransform);

            Image topBar = CreateImage("Top Bar", canvas.transform, new Color(0.18f, 0.34f, 0.52f, 0.95f));
            topBar.sprite = panelSprite;
            topBar.type = Image.Type.Sliced;
            SetRect(topBar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(1840f, 72f), new Vector2(0.5f, 0.5f));

            Image usernamePanel = CreatePanel("Username", topBar.transform, new Vector2(0f, 0.5f), new Vector2(150f, 0f), new Vector2(260f, 48f));
            Text username = CreateText("Username Label", usernamePanel.transform, "●  PLAYER", 21, TextAnchor.MiddleCenter, Color.white);
            username.fontStyle = FontStyle.Bold;
            Stretch(username.rectTransform);

            Image moneyPanel = CreatePanel("Money", topBar.transform, new Vector2(1f, 0.5f), new Vector2(-160f, 0f), new Vector2(280f, 48f));
            Text money = CreateText("Money Label", moneyPanel.transform, "◆  1,000", 21, TextAnchor.MiddleCenter, new Color(1f, 0.84f, 0.2f));
            money.fontStyle = FontStyle.Bold;
            Stretch(money.rectTransform);

            Text title = CreateText("Lobby Title", topBar.transform, "SUPER RACING", 24, TextAnchor.MiddleCenter, new Color(0.18f, 0.95f, 1f));
            title.fontStyle = FontStyle.Bold;
            SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380f, 52f), new Vector2(0.5f, 0.5f));

            var controllerObject = new GameObject("Main Menu Controller");
            MainMenuUI menu = controllerObject.AddComponent<MainMenuUI>();

            Image vehicleHitArea = CreateImage("Vehicle Interaction Area", canvas.transform, new Color(1f, 1f, 1f, 0.001f));
            SetRect(vehicleHitArea.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(820f, 690f), new Vector2(0.5f, 0.5f));
            LobbyVehicleInteractor interactor = vehicleHitArea.gameObject.AddComponent<LobbyVehicleInteractor>();
            interactor.Configure(carRoot, menu);

            Button play = CreateButton("Play", "PLAY   ▶", new Vector2(1f, 0f), new Vector2(-225f, 86f), new Vector2(390f, 82f));
            UnityEventTools.AddPersistentListener(play.onClick, menu.OpenTrackSelection);

            Text garageLabel = CreateText("Garage Prompt", canvas.transform, "CLICK VEHICLE TO OPEN GARAGE", 18, TextAnchor.MiddleCenter, new Color(0.7f, 0.94f, 1f, 0.9f));
            garageLabel.fontStyle = FontStyle.Bold;
            SetRect(garageLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 95f), new Vector2(520f, 40f), new Vector2(0.5f, 0.5f));
        }

        private static Image CreatePanel(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            Image panel = CreateImage(name, parent, new Color(0.42f, 0.62f, 0.82f, 1f));
            panel.sprite = panelSprite;
            panel.type = Image.Type.Sliced;
            SetRect(panel.rectTransform, anchor, position, size, new Vector2(0.5f, 0.5f));
            return panel;
        }

        private static Button CreateButton(string name, string label, Vector2 anchor, Vector2 position, Vector2 size)
        {
            Image image = CreateImage(name, canvas.transform, Color.white);
            image.sprite = primarySprite;
            image.type = Image.Type.Sliced;
            SetRect(image.rectTransform, anchor, position, size, new Vector2(0.5f, 0.5f));
            Button button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.88f, 1f, 0.94f, 1f);
            colors.pressedColor = new Color(0.66f, 0.86f, 0.74f, 1f);
            button.colors = colors;
            Text text = CreateText("Label", image.transform, label, 26, TextAnchor.MiddleCenter, Color.white);
            text.fontStyle = FontStyle.Bold;
            Stretch(text.rectTransform);
            return button;
        }

        private static void FitVehicle(GameObject vehicle, float targetSize, float groundY)
        {
            Renderer[] renderers = vehicle.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            float largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (largest > 0.001f) vehicle.transform.localScale *= targetSize / largest;
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            vehicle.transform.position += new Vector3(-bounds.center.x, groundY - bounds.min.y, -bounds.center.z);
        }

        private static void DisableVehicleBehaviour(GameObject vehicle)
        {
            foreach (MonoBehaviour behaviour in vehicle.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            foreach (Collider collider in vehicle.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (Rigidbody body in vehicle.GetComponentsInChildren<Rigidbody>(true)) { body.isKinematic = true; body.useGravity = false; }
        }

        private static void CreateLight(string name, int layer, float intensity, Color color, Vector3 rotation)
        {
            var lightObject = new GameObject(name);
            lightObject.layer = layer;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = color;
            light.cullingMask = 1 << layer;
            lightObject.transform.rotation = Quaternion.Euler(rotation);
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;
            foreach (Transform child in target.transform) SetLayerRecursively(child.gameObject, layer);
        }

        private static void UpdateBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.path != ScenePath) scenes.Add(scene);
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
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

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
