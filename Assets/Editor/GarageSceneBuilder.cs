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
            cameraObject.transform.position = new Vector3(0f, 2.2f, 7.5f);
            cameraObject.transform.rotation = Quaternion.LookRotation(
                new Vector3(0f, 0.65f, 0f) - cameraObject.transform.position);
            Camera previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = Color.clear;
            previewCamera.cullingMask = 1 << layer;
            previewCamera.fieldOfView = 38f;
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

            Image shade = CreateImage("Readability Shade", canvas.transform, new Color(0.01f, 0.02f, 0.08f, 0.12f));
            shade.raycastTarget = false;
            Stretch(shade.rectTransform);

            var previewObject = CreateUIObject("3D Car Preview", canvas.transform);
            RawImage preview = previewObject.AddComponent<RawImage>();
            preview.texture = previewTexture;
            preview.raycastTarget = false;
            SetAnchors(preview.rectTransform, new Vector2(0.18f, 0.12f), new Vector2(0.82f, 0.88f));

            Image topBar = CreateImage("Top Bar", canvas.transform, new Color(0.015f, 0.1f, 0.17f, 0.97f));
            SetRect(topBar.rectTransform, new Vector2(0f, 1f), Vector2.zero, new Vector2(1920f, 88f), new Vector2(0f, 1f));
            Image topAccent = CreateImage("Top Accent", topBar.transform, new Color(1f, 0.55f, 0.08f, 1f));
            topAccent.rectTransform.anchorMin = Vector2.zero;
            topAccent.rectTransform.anchorMax = new Vector2(1f, 0f);
            topAccent.rectTransform.pivot = new Vector2(0.5f, 0f);
            topAccent.rectTransform.sizeDelta = new Vector2(0f, 5f);
            Text title = CreateText("Garage Title", topBar.transform, "Garage", 42, TextAnchor.MiddleLeft, Color.white);
            SetRect(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(460f, 72f), new Vector2(0f, 0.5f));

            Text carName = CreateText("Car Name", canvas.transform, "SPORT GT", 52, TextAnchor.MiddleLeft, new Color(0.65f, 1f, 0.08f));
            carName.fontStyle = FontStyle.Bold;
            SetRect(carName.rectTransform, new Vector2(0f, 0.5f), new Vector2(210f, 175f), new Vector2(520f, 82f), new Vector2(0f, 0.5f));

            Text powerValue;
            Text accelerationValue;
            Text handlingValue;
            Text gripValue;
            Image powerFill = CreateStatBar("Power", new Vector2(0f, 0.5f), new Vector2(210f, 20f), out powerValue);
            Image handlingFill = CreateStatBar("Handling", new Vector2(0f, 0.5f), new Vector2(210f, -135f), out handlingValue);
            Image accelerationFill = CreateStatBar("Acceleration", new Vector2(1f, 0.5f), new Vector2(-640f, 20f), out accelerationValue);
            Image gripFill = CreateStatBar("Road Grip", new Vector2(1f, 0.5f), new Vector2(-640f, -135f), out gripValue);

            Button previous = CreateButton("Previous Car", "<", new Vector2(0f, 0.5f), new Vector2(70f, 20f), new Vector2(76f, 76f));
            Button next = CreateButton("Next Car", ">", new Vector2(1f, 0.5f), new Vector2(-70f, 20f), new Vector2(76f, 76f));

            Image bottomBar = CreateImage("Bottom Bar", canvas.transform, new Color(0.025f, 0.12f, 0.2f, 0.98f));
            SetRect(bottomBar.rectTransform, Vector2.zero, Vector2.zero, new Vector2(1920f, 132f), Vector2.zero);
            Text bottomName = CreateText("Selected Car Name", bottomBar.transform, "SPORT GT", 30, TextAnchor.MiddleLeft, Color.white);
            bottomName.fontStyle = FontStyle.Bold;
            SetRect(bottomName.rectTransform, new Vector2(0f, 0.5f), new Vector2(30f, 20f), new Vector2(500f, 48f), new Vector2(0f, 0.5f));
            Text rating = CreateText("Car Rating", bottomBar.transform, "★★★", 32, TextAnchor.MiddleLeft, new Color(1f, 0.78f, 0.08f));
            SetRect(rating.rectTransform, new Vector2(0f, 0.5f), new Vector2(30f, -28f), new Vector2(300f, 46f), new Vector2(0f, 0.5f));
            Button continueButton = CreateButton("Continue", "GO   >", new Vector2(1f, 0f), new Vector2(-145f, 66f), new Vector2(250f, 88f));

            var controllerObject = new GameObject("Garage Controller");
            GarageUI garage = controllerObject.AddComponent<GarageUI>();
            var serialized = new SerializedObject(garage);
            serialized.FindProperty("catalog").objectReferenceValue = catalog;
            serialized.FindProperty("carNameLabel").objectReferenceValue = carName;
            serialized.FindProperty("secondaryCarNameLabel").objectReferenceValue = bottomName;
            serialized.FindProperty("powerFill").objectReferenceValue = powerFill;
            serialized.FindProperty("accelerationFill").objectReferenceValue = accelerationFill;
            serialized.FindProperty("handlingFill").objectReferenceValue = handlingFill;
            serialized.FindProperty("gripFill").objectReferenceValue = gripFill;
            serialized.FindProperty("powerValueLabel").objectReferenceValue = powerValue;
            serialized.FindProperty("accelerationValueLabel").objectReferenceValue = accelerationValue;
            serialized.FindProperty("handlingValueLabel").objectReferenceValue = handlingValue;
            serialized.FindProperty("gripValueLabel").objectReferenceValue = gripValue;
            serialized.FindProperty("vehiclePreviewRoot").objectReferenceValue = previewRoot;
            serialized.FindProperty("previewRotationSpeed").floatValue = 10f;
            serialized.FindProperty("previewTargetSize").floatValue = 4.7f;
            serialized.FindProperty("trackSelectionSceneName").stringValue = "Test_Race";
            serialized.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(previous.onClick, garage.SelectPrevious);
            UnityEventTools.AddPersistentListener(next.onClick, garage.SelectNext);
            UnityEventTools.AddPersistentListener(continueButton.onClick, garage.ConfirmSelection);
        }

        private static Image CreateStatBar(string label, Vector2 anchor, Vector2 position, out Text valueLabel)
        {
            var group = CreateUIObject(label + " Stat", canvas.transform);
            RectTransform groupRect = group.GetComponent<RectTransform>();
            SetRect(groupRect, anchor, position, new Vector2(430f, 100f), new Vector2(0f, 0.5f));

            Text title = CreateText("Label", group.transform, label, 27, TextAnchor.MiddleLeft, Color.white);
            SetRect(title.rectTransform, new Vector2(0f, 1f), Vector2.zero, new Vector2(280f, 42f), new Vector2(0f, 1f));
            valueLabel = CreateText("Value", group.transform, "0 %", 22, TextAnchor.MiddleRight, new Color(0.88f, 0.9f, 0.92f));
            SetRect(valueLabel.rectTransform, new Vector2(1f, 1f), Vector2.zero, new Vector2(130f, 42f), new Vector2(1f, 1f));

            Image background = CreateImage("Bar Background", group.transform, new Color(0.15f, 0.19f, 0.22f, 0.94f));
            background.rectTransform.anchorMin = new Vector2(0f, 0f);
            background.rectTransform.anchorMax = new Vector2(1f, 0f);
            background.rectTransform.pivot = new Vector2(0.5f, 0f);
            background.rectTransform.anchoredPosition = new Vector2(0f, 8f);
            background.rectTransform.sizeDelta = new Vector2(0f, 28f);

            Image fill = CreateImage("Bar Fill", background.transform, new Color(0.58f, 1f, 0.05f, 1f));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0.5f;
            Stretch(fill.rectTransform);
            return fill;
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
            ColorBlock colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = new Color(0.15f, 0.95f, 1f, 1f);
            colors.pressedColor = new Color(0.95f, 0.25f, 0.85f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            SetRect(image.rectTransform, anchor, position, size, new Vector2(0.5f, 0.5f));
            Text text = CreateText("Label", image.transform, label, label.Length <= 2 ? 52 : 28, TextAnchor.MiddleCenter, Color.white);
            text.fontStyle = FontStyle.Bold;
            Stretch(text.rectTransform);
            return button;
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
