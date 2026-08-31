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
    public static class GarageSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Garage.unity";
        private const string CatalogPath = "Assets/Data/GameCatalog.asset";
        private const string RenderTexturePath = "Assets/UI/Garage/GaragePreview.renderTexture";

        private static Canvas canvas;
        private static Font font;

        [MenuItem("Super Racing/Build Garage Scene")]
        public static void Build()
        {
            CarDefinition car = ConfigurePrototypeCar();
            GameCatalog catalog = ConfigureCatalog(car);
            RenderTexture previewTexture = GetOrCreateRenderTexture();
            int previewLayer = LayerMask.NameToLayer("GaragePreview");
            if (previewLayer < 0)
            {
                Debug.LogError("Create the GaragePreview layer before building the Garage scene.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Transform previewRoot = CreatePreviewStage(previewTexture, previewLayer);
            CreateInterface(catalog, previewRoot, previewTexture);
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddScenesToBuildSettings();
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = canvas.gameObject;
            Debug.Log("Garage scene created successfully.");
        }

        private static CarDefinition ConfigurePrototypeCar()
        {
            CarDefinition car = AssetDatabase.LoadAssetAtPath<CarDefinition>("Assets/Data/PrototypeCar.asset");
            GameObject vehicle = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Game/Prefabs/Vehicles/SportCar_Prototype.prefab");
            var serialized = new SerializedObject(car);
            serialized.FindProperty("displayName").stringValue = "SPORT GT";
            serialized.FindProperty("vehiclePrefab").objectReferenceValue = vehicle;
            serialized.FindProperty("maxSpeedKmh").floatValue = 180f;
            serialized.FindProperty("motorTorque").floatValue = 2200f;
            serialized.FindProperty("steeringAngle").floatValue = 32f;
            serialized.FindProperty("grip").floatValue = 1.15f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(car);
            return car;
        }

        private static GameCatalog ConfigureCatalog(CarDefinition car)
        {
            GameCatalog catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<GameCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var serialized = new SerializedObject(catalog);
            SerializedProperty cars = serialized.FindProperty("cars");
            cars.arraySize = 1;
            cars.GetArrayElementAtIndex(0).objectReferenceValue = car;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static RenderTexture GetOrCreateRenderTexture()
        {
            RenderTexture texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
            if (texture != null)
            {
                return texture;
            }

            texture = new RenderTexture(1024, 576, 24, RenderTextureFormat.ARGB32)
            {
                name = "GaragePreview",
                antiAliasing = 4
            };
            AssetDatabase.CreateAsset(texture, RenderTexturePath);
            return texture;
        }

        private static Transform CreatePreviewStage(RenderTexture texture, int layer)
        {
            var previewRoot = new GameObject("Vehicle Preview Root");
            previewRoot.layer = layer;

            var cameraObject = new GameObject("Garage Preview Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 1.4f, 8.2f);
            cameraObject.transform.rotation = Quaternion.LookRotation(
                new Vector3(0f, 0.52f, 0f) - cameraObject.transform.position);
            Camera previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = Color.clear;
            previewCamera.cullingMask = 1 << layer;
            previewCamera.fieldOfView = 34f;
            previewCamera.nearClipPlane = 0.1f;
            previewCamera.farClipPlane = 50f;
            previewCamera.targetTexture = texture;

            CreateLight("Garage Key Light", layer, 2.2f, new Color(0.75f, 0.9f, 1f), new Vector3(35f, -35f, 0f));
            CreateLight("Garage Fill Light", layer, 1.1f, new Color(1f, 0.35f, 0.75f), new Vector3(25f, 145f, 0f));
            return previewRoot.transform;
        }

        private static void CreateLight(string name, int layer, float intensity, Color color, Vector3 rotation)
        {
            var lightObject = new GameObject(name);
            lightObject.layer = layer;
            lightObject.transform.rotation = Quaternion.Euler(rotation);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = color;
            light.cullingMask = 1 << layer;
        }

        private static void CreateInterface(GameCatalog catalog, Transform previewRoot, RenderTexture previewTexture)
        {
            var canvasObject = new GameObject("Garage Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Image background = CreateImage("Garage Background", canvas.transform, Color.white);
            background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Garage/Garage_Background.png");
            background.raycastTarget = false;
            Stretch(background.rectTransform);

            Image shade = CreateImage("Dark Garage Overlay", canvas.transform, new Color(0.005f, 0.015f, 0.045f, 0.78f));
            shade.raycastTarget = false;
            Stretch(shade.rectTransform);

            var previewObject = CreateUIObject("3D Car Preview", canvas.transform);
            RawImage preview = previewObject.AddComponent<RawImage>();
            preview.texture = previewTexture;
            preview.raycastTarget = false;
            SetAnchors(preview.rectTransform, new Vector2(0.2f, 0.13f), new Vector2(0.75f, 0.88f));

            Image topStrip = CreateImage("Top Controls", canvas.transform, new Color(0.01f, 0.025f, 0.06f, 0.94f));
            SetRect(topStrip.rectTransform, new Vector2(0f, 1f), Vector2.zero, new Vector2(1920f, 86f), new Vector2(0f, 1f));
            Button exitButton = CreateButton("Exit", "<<  EXIT", new Vector2(0f, 1f), new Vector2(105f, -43f), new Vector2(170f, 58f));
            Text playerName = CreateText("Player Name", canvas.transform, "PLAYER", 27, TextAnchor.MiddleLeft, Color.white);
            playerName.fontStyle = FontStyle.Bold;
            SetRect(playerName.rectTransform, new Vector2(0f, 1f), new Vector2(220f, -43f), new Vector2(250f, 58f), new Vector2(0f, 0.5f));
            Image adPanel = CreateImage("Ad Placeholder", canvas.transform, new Color(0.35f, 0.35f, 0.37f, 0.9f));
            SetRect(adPanel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -43f), new Vector2(560f, 58f), new Vector2(0.5f, 0.5f));
            Text adText = CreateText("Ad Label", adPanel.transform, "SELECT YOUR CAR", 24, TextAnchor.MiddleCenter, new Color(0.08f, 0.08f, 0.1f));
            adText.fontStyle = FontStyle.Bold;
            Stretch(adText.rectTransform);
            Button settings = CreateButton("Settings", "*", new Vector2(1f, 1f), new Vector2(-285f, -43f), new Vector2(66f, 58f));
            Image currency = CreateImage("Currency", canvas.transform, new Color(0.015f, 0.08f, 0.15f, 0.96f));
            SetRect(currency.rectTransform, new Vector2(1f, 1f), new Vector2(-120f, -43f), new Vector2(200f, 58f), new Vector2(0.5f, 0.5f));
            Text currencyText = CreateText("Currency Text", currency.transform, "1000   ●", 27, TextAnchor.MiddleCenter, Color.white);
            currencyText.fontStyle = FontStyle.Bold;
            Stretch(currencyText.rectTransform);

            CreateCarThumbnail("CAR 1", previewTexture, new Vector2(48f, -155f), true);
            CreateCarThumbnail("CAR 2", previewTexture, new Vector2(48f, -350f), false);
            CreateCarThumbnail("CAR 3", previewTexture, new Vector2(48f, -545f), false);
            CreateCarThumbnail("CAR 4", previewTexture, new Vector2(48f, -740f), false);

            Image statsPanel = CreateImage("Performance Panel", canvas.transform, new Color(0.01f, 0.045f, 0.09f, 0.9f));
            SetRect(statsPanel.rectTransform, new Vector2(1f, 0.5f), new Vector2(-450f, 8f), new Vector2(390f, 620f), new Vector2(0f, 0.5f));
            Outline statsOutline = statsPanel.gameObject.AddComponent<Outline>();
            statsOutline.effectColor = new Color(0.05f, 0.55f, 0.8f, 0.8f);
            statsOutline.effectDistance = new Vector2(3f, -3f);

            Text carName = CreateText("Car Name", canvas.transform, "SPORT GT", 31, TextAnchor.MiddleLeft, new Color(0.1f, 0.9f, 1f));
            carName.fontStyle = FontStyle.Bold;
            SetRect(carName.rectTransform, new Vector2(1f, 0.5f), new Vector2(-420f, 255f), new Vector2(310f, 54f), new Vector2(0f, 0.5f));

            Text powerValue;
            Text accelerationValue;
            Text handlingValue;
            Text gripValue;
            Image powerFill = CreateStatBar("TOP SPEED", new Vector2(1f, 0.5f), new Vector2(-420f, 160f), out powerValue);
            Image accelerationFill = CreateStatBar("POWER", new Vector2(1f, 0.5f), new Vector2(-420f, 35f), out accelerationValue);
            Image handlingFill = CreateStatBar("GRIP", new Vector2(1f, 0.5f), new Vector2(-420f, -90f), out handlingValue);
            Image gripFill = CreateStatBar("DRIFT", new Vector2(1f, 0.5f), new Vector2(-420f, -215f), out gripValue);

            Button continueButton = CreateButton("Continue", ">  PLAY", new Vector2(1f, 0f), new Vector2(-235f, 115f), new Vector2(390f, 135f));
            SetButtonPalette(continueButton, new Color(0.12f, 1f, 0.05f, 1f));

            var controllerObject = new GameObject("Garage Controller");
            GarageUI garage = controllerObject.AddComponent<GarageUI>();
            var serialized = new SerializedObject(garage);
            serialized.FindProperty("catalog").objectReferenceValue = catalog;
            serialized.FindProperty("carNameLabel").objectReferenceValue = carName;
            serialized.FindProperty("powerFill").objectReferenceValue = powerFill;
            serialized.FindProperty("accelerationFill").objectReferenceValue = accelerationFill;
            serialized.FindProperty("handlingFill").objectReferenceValue = handlingFill;
            serialized.FindProperty("gripFill").objectReferenceValue = gripFill;
            serialized.FindProperty("powerValueLabel").objectReferenceValue = powerValue;
            serialized.FindProperty("accelerationValueLabel").objectReferenceValue = accelerationValue;
            serialized.FindProperty("handlingValueLabel").objectReferenceValue = handlingValue;
            serialized.FindProperty("gripValueLabel").objectReferenceValue = gripValue;
            serialized.FindProperty("vehiclePreviewRoot").objectReferenceValue = previewRoot;
            serialized.FindProperty("previewRotationSpeed").floatValue = 0f;
            serialized.FindProperty("previewTargetSize").floatValue = 4.7f;
            serialized.FindProperty("trackSelectionSceneName").stringValue = "Test_Race";
            serialized.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(continueButton.onClick, garage.ConfirmSelection);
        }

        private static Image CreateStatBar(string label, Vector2 anchor, Vector2 position, out Text valueLabel)
        {
            var group = CreateUIObject(label + " Stat", canvas.transform);
            RectTransform groupRect = group.GetComponent<RectTransform>();
            SetRect(groupRect, anchor, position, new Vector2(310f, 92f), new Vector2(0f, 0.5f));

            Text title = CreateText("Label", group.transform, label, 24, TextAnchor.MiddleLeft, Color.white);
            SetRect(title.rectTransform, new Vector2(0f, 1f), Vector2.zero, new Vector2(210f, 38f), new Vector2(0f, 1f));
            valueLabel = CreateText("Value", group.transform, "0 %", 20, TextAnchor.MiddleRight, new Color(0.88f, 0.9f, 0.92f));
            SetRect(valueLabel.rectTransform, new Vector2(1f, 1f), Vector2.zero, new Vector2(95f, 38f), new Vector2(1f, 1f));

            Image background = CreateImage("Bar Background", group.transform, new Color(0.025f, 0.09f, 0.14f, 1f));
            background.rectTransform.anchorMin = new Vector2(0f, 0f);
            background.rectTransform.anchorMax = new Vector2(1f, 0f);
            background.rectTransform.pivot = new Vector2(0.5f, 0f);
            background.rectTransform.anchoredPosition = new Vector2(0f, 8f);
            background.rectTransform.sizeDelta = new Vector2(0f, 24f);
            Outline barOutline = background.gameObject.AddComponent<Outline>();
            barOutline.effectColor = new Color(0.15f, 0.62f, 0.78f, 0.9f);
            barOutline.effectDistance = new Vector2(2f, -2f);

            Image fill = CreateImage("Bar Fill", background.transform, new Color(1f, 0.72f, 0.04f, 1f));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0.5f;
            Stretch(fill.rectTransform);
            return fill;
        }

        private static void CreateCarThumbnail(string label, RenderTexture texture, Vector2 position, bool selected)
        {
            Image frame = CreateImage(label, canvas.transform,
                selected ? new Color(0.05f, 0.75f, 1f, 0.98f) : new Color(0.02f, 0.18f, 0.3f, 0.95f));
            SetRect(frame.rectTransform, new Vector2(0f, 1f), position, new Vector2(205f, 155f), new Vector2(0f, 1f));
            frame.gameObject.AddComponent<Button>();
            Outline frameOutline = frame.gameObject.AddComponent<Outline>();
            frameOutline.effectColor = selected ? new Color(0.25f, 0.95f, 1f, 1f) : new Color(0.04f, 0.35f, 0.5f, 0.9f);
            frameOutline.effectDistance = new Vector2(3f, -3f);
            Text title = CreateText("Label", frame.transform, label, 20, TextAnchor.MiddleLeft, Color.white);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(8f, -5f), new Vector2(150f, 30f), new Vector2(0f, 1f));

            var previewObject = CreateUIObject("Preview", frame.transform);
            RawImage preview = previewObject.AddComponent<RawImage>();
            preview.texture = texture;
            preview.color = selected ? Color.white : new Color(0.35f, 0.42f, 0.5f, 0.75f);
            preview.raycastTarget = false;
            preview.rectTransform.anchorMin = Vector2.zero;
            preview.rectTransform.anchorMax = Vector2.one;
            preview.rectTransform.offsetMin = new Vector2(6f, 6f);
            preview.rectTransform.offsetMax = new Vector2(-6f, -34f);

            if (!selected)
            {
                Text locked = CreateText("Locked", frame.transform, "LOCKED", 18, TextAnchor.MiddleCenter, new Color(0.75f, 0.82f, 0.88f));
                locked.fontStyle = FontStyle.Bold;
                Stretch(locked.rectTransform);
            }
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

        private static Button CreateButton(string name, string label, Vector2 anchor, Vector2 position, Vector2 size)
        {
            Color normal = new Color(0.05f, 0.2f, 0.55f, 0.96f);
            Image image = CreateImage(name, canvas.transform, normal);
            Button button = image.gameObject.AddComponent<Button>();
            Outline outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.1f, 0.85f, 1f, 0.95f);
            outline.effectDistance = new Vector2(3f, -3f);
            Shadow shadow = image.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            shadow.effectDistance = new Vector2(6f, -6f);
            ColorBlock colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = new Color(0.15f, 0.95f, 1f, 1f);
            colors.pressedColor = new Color(0.95f, 0.25f, 0.85f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            SetRect(image.rectTransform, anchor, position, size, new Vector2(0.5f, 0.5f));
            Text text = CreateText("Label", image.transform, label, label.Length <= 2 ? 52 : 28, TextAnchor.MiddleCenter, Color.white);
            text.fontStyle = FontStyle.Bold;
            Shadow textShadow = text.gameObject.AddComponent<Shadow>();
            textShadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            textShadow.effectDistance = new Vector2(2f, -2f);
            Stretch(text.rectTransform);
            return button;
        }

        private static void SetButtonPalette(Button button, Color normal)
        {
            button.image.color = Color.white;
            ColorBlock colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = Color.Lerp(normal, Color.white, 0.25f);
            colors.pressedColor = Color.Lerp(normal, Color.black, 0.25f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetAnchors(RectTransform rect, Vector2 minimum, Vector2 maximum)
        {
            rect.anchorMin = minimum;
            rect.anchorMax = maximum;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Stretch(RectTransform rect)
        {
            SetAnchors(rect, Vector2.zero, Vector2.one);
        }

        private static void AddScenesToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (existing.path != ScenePath)
                {
                    scenes.Add(existing);
                }
            }

            const string testRacePath = "Assets/Scenes/Test_Race.unity";
            if (!scenes.Exists(item => item.path == testRacePath))
            {
                scenes.Add(new EditorBuildSettingsScene(testRacePath, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
