using System.Collections.Generic;
using System.IO;
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
        private const string TexturesFolder = "Assets/UI/Garage";

        private static Canvas canvas;
        private static Font font;

        private static Sprite glassPanelBg;
        private static Sprite glassPanelSelected;
        private static Sprite glassButtonNormal;
        private static Sprite glassButtonPrimary;
        private static Sprite statBarTrack;
        private static Sprite statBarFill;
        private static Sprite carShadowSprite;

        [MenuItem("Super Racing/Build Garage Scene")]
        public static void Build()
        {
            EnsureTexturesFolder();
            GenerateAllUiSprites();

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
            List<RenderTexture> thumbnailTextures = CreateThumbnailStages(catalog, previewLayer);
            CreateInterface(catalog, previewRoot, previewTexture, thumbnailTextures);
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddScenesToBuildSettings();
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = canvas.gameObject;
            Debug.Log("Garage scene created successfully with modernized glassmorphism UI.");
        }

        private static void EnsureTexturesFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/UI"))
            {
                AssetDatabase.CreateFolder("Assets", "UI");
            }
            if (!AssetDatabase.IsValidFolder(TexturesFolder))
            {
                AssetDatabase.CreateFolder("Assets/UI", "Garage");
            }
        }

        private static void GenerateAllUiSprites()
        {
            glassPanelBg = CreateOrLoadRoundedSprite(
                $"{TexturesFolder}/GlassPanel_Bg.png",
                128, 128, 20, 2,
                new Color(0.025f, 0.055f, 0.12f, 0.90f),
                new Color(0.0f, 0.78f, 1.0f, 0.72f),
                new Vector4(24f, 24f, 24f, 24f)
            );

            glassPanelSelected = CreateOrLoadRoundedSprite(
                $"{TexturesFolder}/GlassPanel_Selected.png",
                128, 128, 20, 3,
                new Color(0.0f, 0.30f, 0.58f, 0.78f),
                new Color(0.0f, 0.92f, 1.0f, 0.95f),
                new Vector4(24f, 24f, 24f, 24f)
            );

            glassButtonNormal = CreateOrLoadRoundedSprite(
                $"{TexturesFolder}/GlassButton_Normal.png",
                128, 64, 14, 2,
                new Color(0.05f, 0.12f, 0.24f, 0.75f),
                new Color(0.0f, 0.75f, 1.0f, 0.65f),
                new Vector4(18f, 18f, 18f, 18f)
            );

            glassButtonPrimary = CreateOrLoadRoundedSprite(
                $"{TexturesFolder}/GlassButton_Primary.png",
                128, 64, 16, 3,
                new Color(0.0f, 0.82f, 0.45f, 0.88f),
                new Color(0.45f, 1.0f, 0.75f, 0.98f),
                new Vector4(20f, 20f, 20f, 20f)
            );

            statBarTrack = CreateOrLoadRoundedSprite(
                $"{TexturesFolder}/StatBar_Track.png",
                64, 20, 8, 1,
                new Color(0.06f, 0.11f, 0.20f, 0.85f),
                new Color(0.0f, 0.65f, 0.95f, 0.35f),
                new Vector4(10f, 10f, 10f, 10f)
            );

            statBarFill = CreateOrLoadRoundedSprite(
                $"{TexturesFolder}/StatBar_Fill.png",
                64, 20, 8, 0,
                new Color(0.0f, 0.90f, 1.0f, 1.0f),
                Color.clear,
                new Vector4(10f, 10f, 10f, 10f)
            );

            carShadowSprite = CreateOrLoadShadowSprite($"{TexturesFolder}/CarShadow.png", 256, 256);
        }

        private static Sprite CreateOrLoadRoundedSprite(string path, int width, int height, int radius, int borderWidth, Color fill, Color border, Vector4 spriteBorder)
        {
            if (!File.Exists(path))
            {
                Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                Color[] colors = new Color[width * height];
                float halfW = (width - 1) * 0.5f;
                float halfH = (height - 1) * 0.5f;
                float innerW = halfW - radius;
                float innerH = halfH - radius;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float px = Mathf.Abs(x - halfW) - innerW;
                        float py = Mathf.Abs(y - halfH) - innerH;
                        float dist = (px <= 0f && py <= 0f) ? Mathf.Max(px, py) : Mathf.Sqrt(Mathf.Max(px, 0f) * Mathf.Max(px, 0f) + Mathf.Max(py, 0f) * Mathf.Max(py, 0f));
                        dist -= radius;

                        Color pixelColor;
                        if (dist > 0.5f)
                        {
                            pixelColor = Color.clear;
                        }
                        else
                        {
                            float outerAlpha = Mathf.Clamp01(0.5f - dist);
                            if (borderWidth > 0 && dist >= -borderWidth - 0.5f)
                            {
                                float borderT = Mathf.Clamp01((dist + borderWidth + 0.5f) / 1.0f);
                                pixelColor = Color.Lerp(fill, border, borderT);
                            }
                            else
                            {
                                float yNorm = (float)y / height;
                                Color gradFill = Color.Lerp(fill * 0.85f, fill * 1.15f, yNorm);
                                gradFill.a = fill.a;
                                pixelColor = gradFill;
                            }
                            pixelColor.a *= outerAlpha;
                        }
                        colors[y * width + x] = pixelColor;
                    }
                }

                texture.SetPixels(colors);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            }

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = spriteBorder;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite CreateOrLoadShadowSprite(string path, int width, int height)
        {
            if (!File.Exists(path))
            {
                Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                Color[] colors = new Color[width * height];
                float cx = (width - 1) * 0.5f;
                float cy = (height - 1) * 0.5f;
                float rx = cx * 0.95f;
                float ry = cy * 0.95f;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float dx = (x - cx) / rx;
                        float dy = (y - cy) / ry;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float alpha = Mathf.Clamp01(1f - d);
                        alpha = Mathf.Pow(alpha, 1.7f) * 0.70f;
                        colors[y * width + x] = new Color(0f, 0f, 0f, alpha);
                    }
                }

                texture.SetPixels(colors);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                }
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
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
            string[] preferredCarPaths =
            {
                "Assets/Data/ControlCar.asset",
                "Assets/Data/BalancedCar.asset",
                "Assets/Data/PrototypeCar.asset",
                "Assets/Data/SpeedsterCar.asset"
            };

            var definitions = new List<CarDefinition>();
            var visualSignatures = new HashSet<string>();
            foreach (string path in preferredCarPaths)
            {
                CarDefinition definition = AssetDatabase.LoadAssetAtPath<CarDefinition>(path);
                AddUniqueVisual(definitions, visualSignatures, definition);
            }

            foreach (string guid in AssetDatabase.FindAssets("t:CarDefinition", new[] { "Assets/Data" }))
            {
                CarDefinition definition = AssetDatabase.LoadAssetAtPath<CarDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                AddUniqueVisual(definitions, visualSignatures, definition);
            }

            if (definitions.Count == 0 && car != null)
            {
                definitions.Add(car);
            }

            cars.arraySize = definitions.Count;
            for (int index = 0; index < definitions.Count; index++)
            {
                cars.GetArrayElementAtIndex(index).objectReferenceValue = definitions[index];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void AddUniqueVisual(List<CarDefinition> definitions, HashSet<string> signatures, CarDefinition definition)
        {
            if (definition == null || definition.VehiclePrefab == null || definitions.Contains(definition))
            {
                return;
            }

            string signature = GetVisualSignature(definition.VehiclePrefab);
            if (signatures.Add(signature))
            {
                definitions.Add(definition);
            }
        }

        private static string GetVisualSignature(GameObject prefab)
        {
            var meshIds = new List<string>();
            foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh != null)
                {
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(filter.sharedMesh, out string guid, out long localId);
                    meshIds.Add($"{guid}:{localId}");
                }
            }

            foreach (SkinnedMeshRenderer renderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.sharedMesh != null)
                {
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(renderer.sharedMesh, out string guid, out long localId);
                    meshIds.Add($"{guid}:{localId}");
                }
            }

            meshIds.Sort();
            return meshIds.Count > 0 ? string.Join("|", meshIds) : AssetDatabase.GetAssetPath(prefab);
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

            var shadowObject = new GameObject("Vehicle Contact Shadow");
            shadowObject.layer = layer;
            shadowObject.transform.position = new Vector3(0f, -0.475f, 0f);
            shadowObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            shadowObject.transform.localScale = new Vector3(2.8f, 5.0f, 1f);
            SpriteRenderer shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = carShadowSprite;
            shadowRenderer.color = new Color(0f, 0f, 0f, 0.75f);

            var cameraObject = new GameObject("Garage Preview Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 1.45f, 6.2f);
            cameraObject.transform.rotation = Quaternion.LookRotation(
                new Vector3(0f, 0.05f, 0f) - cameraObject.transform.position);
            Camera previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = Color.clear;
            previewCamera.cullingMask = 1 << layer;
            previewCamera.fieldOfView = 35f;
            previewCamera.nearClipPlane = 0.1f;
            previewCamera.farClipPlane = 50f;
            previewCamera.targetTexture = texture;

            CreateLight("Garage Key Light", layer, 2.5f, new Color(0.85f, 0.95f, 1.0f), new Vector3(35f, -30f, 0f));
            CreateLight("Garage Fill Light", layer, 1.4f, new Color(1.0f, 0.35f, 0.75f), new Vector3(20f, 150f, 0f));
            CreateLight("Garage Rim Light", layer, 2.0f, new Color(0.1f, 0.85f, 1.0f), new Vector3(-20f, 10f, 0f));
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

        private static List<RenderTexture> CreateThumbnailStages(GameCatalog catalog, int layer)
        {
            const string folder = "Assets/UI/Garage/Thumbnails";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/UI/Garage", "Thumbnails");
            }

            var textures = new List<RenderTexture>();
            for (int index = 0; index < catalog.Cars.Count; index++)
            {
                CarDefinition car = catalog.Cars[index];
                string path = $"{folder}/{car.CarId}.renderTexture";
                RenderTexture texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
                if (texture == null)
                {
                    texture = new RenderTexture(384, 216, 24, RenderTextureFormat.ARGB32)
                    {
                        name = $"{car.CarId}_Thumbnail",
                        antiAliasing = 2,
                        useMipMap = false
                    };
                    AssetDatabase.CreateAsset(texture, path);
                }

                textures.Add(texture);
                CreateThumbnailStage(car, texture, layer, index);
            }

            return textures;
        }

        private static void CreateThumbnailStage(CarDefinition car, RenderTexture texture, int layer, int index)
        {
            Vector3 stagePosition = new Vector3(100f + index * 100f, 0f, 0f);
            var root = new GameObject($"Thumbnail Stage - {car.DisplayName}");
            root.layer = layer;
            root.transform.position = stagePosition;

            GameObject vehicle = (GameObject)PrefabUtility.InstantiatePrefab(car.VehiclePrefab);
            vehicle.name = $"{car.DisplayName} Thumbnail Vehicle";
            vehicle.transform.SetParent(root.transform, false);
            SetLayerRecursively(vehicle, layer);
            DisableVehicleBehaviour(vehicle);
            FitThumbnailVehicle(vehicle, stagePosition, 3.2f);
            vehicle.transform.rotation = Quaternion.Euler(0f, -28f, 0f);

            var cameraObject = new GameObject($"Thumbnail Camera - {car.DisplayName}");
            cameraObject.transform.position = stagePosition + new Vector3(0f, 1.15f, 3.8f);
            cameraObject.transform.LookAt(stagePosition + new Vector3(0f, 0.8f, 0f));
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.cullingMask = 1 << layer;
            camera.fieldOfView = 30f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 30f;
            camera.targetTexture = texture;
        }

        private static void FitThumbnailVehicle(GameObject vehicle, Vector3 stagePosition, float targetSize)
        {
            Renderer[] renderers = vehicle.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            float largestSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (largestSize > 0.001f)
            {
                vehicle.transform.localScale *= targetSize / largestSize;
            }

            bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            vehicle.transform.position += stagePosition + new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
        }

        private static void DisableVehicleBehaviour(GameObject vehicle)
        {
            foreach (MonoBehaviour behaviour in vehicle.GetComponentsInChildren<MonoBehaviour>(true))
            {
                behaviour.enabled = false;
            }

            foreach (Collider collider in vehicle.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            foreach (Rigidbody body in vehicle.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.useGravity = false;
            }
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;
            foreach (Transform child in target.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private static void CreateInterface(GameCatalog catalog, Transform previewRoot, RenderTexture previewTexture, IReadOnlyList<RenderTexture> thumbnailTextures)
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

            // 1. Background Image
            Image background = CreateImage("Garage Background", canvas.transform, Color.white);
            background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Garage/Garage_Background.png");
            background.raycastTarget = false;
            Stretch(background.rectTransform);

            // 2. Subtle Dark Glass Atmosphere Overlay
            Image shade = CreateImage("Dark Garage Overlay", canvas.transform, new Color(0.01f, 0.02f, 0.05f, 0.40f));
            shade.raycastTarget = false;
            Stretch(shade.rectTransform);

            // 3. 3D Car Preview RawImage (full canvas matching 16:9 perspective)
            var previewObject = CreateUIObject("3D Car Preview", canvas.transform);
            RawImage preview = previewObject.AddComponent<RawImage>();
            preview.texture = previewTexture;
            preview.raycastTarget = true;
            Stretch(preview.rectTransform);
            GaragePreviewRotator rotator = previewObject.AddComponent<GaragePreviewRotator>();
            rotator.SetTarget(previewRoot);

            // 4. Top Navigation Bar (Translucent Glass Strip)
            Image topStrip = CreateImage("Top Controls", canvas.transform, new Color(0.025f, 0.06f, 0.12f, 0.65f));
            topStrip.sprite = glassPanelBg;
            topStrip.type = Image.Type.Sliced;
            SetRect(topStrip.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(1920f, 76f), new Vector2(0f, 1f));

            // Cyan Bottom Glow Line for Top Bar
            Image topGlowLine = CreateImage("Top Glow Line", topStrip.transform, new Color(0.0f, 0.82f, 1.0f, 0.55f));
            topGlowLine.raycastTarget = false;
            SetRect(topGlowLine.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(1920f, 2f), new Vector2(0f, 0f));

            // Player Badge
            Image playerBadge = CreateImage("Player Badge", canvas.transform, new Color(0.04f, 0.09f, 0.18f, 0.65f));
            playerBadge.sprite = glassPanelBg;
            playerBadge.type = Image.Type.Sliced;
            SetRect(playerBadge.rectTransform, new Vector2(0f, 1f), new Vector2(275f, -38f), new Vector2(160f, 48f), new Vector2(0.5f, 0.5f));
            Text playerName = CreateText("Player Name", playerBadge.transform, "●  PLAYER", 19, TextAnchor.MiddleCenter, Color.white);
            playerName.fontStyle = FontStyle.Bold;
            Stretch(playerName.rectTransform);

            // Title Badge (Center)
            Image titleBadge = CreateImage("Title Badge", canvas.transform, new Color(0.04f, 0.09f, 0.18f, 0.65f));
            titleBadge.sprite = glassPanelBg;
            titleBadge.type = Image.Type.Sliced;
            SetRect(titleBadge.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(380f, 48f), new Vector2(0.5f, 0.5f));
            Text titleText = CreateText("Title Label", titleBadge.transform, "SELECT YOUR CAR", 20, TextAnchor.MiddleCenter, new Color(0.0f, 0.90f, 1.0f));
            titleText.fontStyle = FontStyle.Bold;
            Stretch(titleText.rectTransform);

            // Settings Button
            Button settings = CreateGlassButton("Settings", "⚙", new Vector2(1f, 1f), new Vector2(-245f, -38f), new Vector2(48f, 48f), 24, false);

            // Currency Badge
            Image currency = CreateImage("Currency", canvas.transform, new Color(0.04f, 0.09f, 0.18f, 0.70f));
            currency.sprite = glassPanelBg;
            currency.type = Image.Type.Sliced;
            SetRect(currency.rectTransform, new Vector2(1f, 1f), new Vector2(-125f, -38f), new Vector2(175f, 48f), new Vector2(0.5f, 0.5f));
            Text currencyText = CreateText("Currency Text", currency.transform, "1,000   ◆", 20, TextAnchor.MiddleCenter, new Color(1.0f, 0.85f, 0.2f));
            currencyText.fontStyle = FontStyle.Bold;
            Stretch(currencyText.rectTransform);

            // 5. Left Car Selection Cards
            var carButtons = new List<Button>();
            for (int index = 0; index < catalog.Cars.Count; index++)
            {
                CarDefinition car = catalog.Cars[index];
                carButtons.Add(CreateCarCard(
                    $"CAR {index + 1:00}",
                    thumbnailTextures[index],
                    new Vector2(36f, -100f - index * 145f),
                    index == 0,
                    car.DisplayName));
            }

            // 6. Right Vehicle Stats Panel (Glassmorphism)
            Image statsPanel = CreateImage("Performance Panel", canvas.transform, new Color(0.32f, 0.48f, 0.68f, 1f));
            statsPanel.sprite = glassPanelBg;
            statsPanel.type = Image.Type.Sliced;
            SetRect(statsPanel.rectTransform, new Vector2(1f, 0.5f), new Vector2(-240f, 40f), new Vector2(410f, 580f), new Vector2(0.5f, 0.5f));
            AddOpacityLayer(statsPanel.transform, "Performance Opaque Layer");

            // Tier/Category Tag
            Text tierTag = CreateText("Tier Tag", statsPanel.transform, "TIER S+  //  HYPERCAR", 14, TextAnchor.MiddleLeft, new Color(0.0f, 0.85f, 1.0f, 0.85f));
            tierTag.fontStyle = FontStyle.Bold;
            SetRect(tierTag.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -32f), new Vector2(350f, 24f), new Vector2(0f, 0.5f));

            // Car Name Header
            Text carName = CreateText("Car Name", statsPanel.transform, "SPORT GT", 32, TextAnchor.MiddleLeft, Color.white);
            carName.fontStyle = FontStyle.Bold;
            SetRect(carName.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -65f), new Vector2(350f, 44f), new Vector2(0f, 0.5f));
            Shadow nameShadow = carName.gameObject.AddComponent<Shadow>();
            nameShadow.effectColor = new Color(0.0f, 0.75f, 1.0f, 0.45f);
            nameShadow.effectDistance = new Vector2(0f, 0f);

            // Divider Line
            Image divider = CreateImage("Divider", statsPanel.transform, new Color(0.0f, 0.8f, 1.0f, 0.35f));
            divider.raycastTarget = false;
            SetRect(divider.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -95f), new Vector2(354f, 2f), new Vector2(0f, 0.5f));

            // Stat Bars
            Text powerValue;
            Text accelerationValue;
            Text handlingValue;
            Text gripValue;
            Image powerFill = CreateStatBarRow(statsPanel.transform, "TOP SPEED", new Vector2(28f, -165f), new Color(0.0f, 0.90f, 1.0f), out powerValue);
            Image accelerationFill = CreateStatBarRow(statsPanel.transform, "POWER", new Vector2(28f, -255f), new Color(0.0f, 0.90f, 1.0f), out accelerationValue);
            Image handlingFill = CreateStatBarRow(statsPanel.transform, "HANDLING", new Vector2(28f, -345f), new Color(1.0f, 0.75f, 0.1f), out handlingValue);
            Image gripFill = CreateStatBarRow(statsPanel.transform, "GRIP", new Vector2(28f, -435f), new Color(1.0f, 0.75f, 0.1f), out gripValue);

            // 7. Bottom-Right Action Button (Play / Race)
            Button continueButton = CreateGlassButton("Continue", "SELECT   ▶", new Vector2(1f, 0f), new Vector2(-240f, 85f), new Vector2(410f, 74f), 24, true);

            // 8. Garage Controller setup
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
            serialized.FindProperty("previewTargetSize").floatValue = 3.25f;
            serialized.FindProperty("vehiclePositionOffset").vector3Value = new Vector3(0f, -0.96f, 0f);
            serialized.FindProperty("vehicleRotationEuler").vector3Value = new Vector3(0f, 8f, 0f);
            serialized.FindProperty("mainMenuSceneName").stringValue = "MainMenu";
            serialized.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(continueButton.onClick, garage.ReturnToMainMenu);
            for (int index = 0; index < carButtons.Count; index++)
            {
                UnityEventTools.AddIntPersistentListener(carButtons[index].onClick, garage.SelectCar, index);
            }
        }

        private static Image CreateStatBarRow(Transform parent, string label, Vector2 position, Color fillColor, out Text valueLabel)
        {
            var row = CreateUIObject(label + " Row", parent);
            RectTransform rowRect = row.GetComponent<RectTransform>();
            SetRect(rowRect, new Vector2(0f, 1f), position, new Vector2(354f, 64f), new Vector2(0f, 0.5f));

            Text title = CreateText("Label", row.transform, label, 17, TextAnchor.MiddleLeft, Color.white);
            title.fontStyle = FontStyle.Bold;
            SetRect(title.rectTransform, new Vector2(0f, 1f), Vector2.zero, new Vector2(220f, 28f), new Vector2(0f, 1f));

            valueLabel = CreateText("Value", row.transform, "0 %", 17, TextAnchor.MiddleRight, fillColor);
            valueLabel.fontStyle = FontStyle.Bold;
            SetRect(valueLabel.rectTransform, new Vector2(1f, 1f), Vector2.zero, new Vector2(100f, 28f), new Vector2(1f, 1f));

            Image track = CreateImage("Bar Track", row.transform, Color.white);
            track.sprite = statBarTrack;
            track.type = Image.Type.Sliced;
            track.rectTransform.anchorMin = new Vector2(0f, 0f);
            track.rectTransform.anchorMax = new Vector2(1f, 0f);
            track.rectTransform.pivot = new Vector2(0.5f, 0f);
            track.rectTransform.anchoredPosition = new Vector2(0f, 4f);
            track.rectTransform.sizeDelta = new Vector2(0f, 16f);

            Image fill = CreateImage("Bar Fill", track.transform, fillColor);
            fill.sprite = statBarFill;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0.7f;
            Stretch(fill.rectTransform);
            return fill;
        }

        private static Button CreateCarCard(string label, RenderTexture texture, Vector2 position, bool selected, string carSubname)
        {
            Image card = CreateImage(label + " Card", canvas.transform, Color.white);
            card.sprite = selected ? glassPanelSelected : glassPanelBg;
            card.type = Image.Type.Sliced;
            SetRect(card.rectTransform, new Vector2(0f, 1f), position, new Vector2(215f, 130f), new Vector2(0f, 1f));
            Button button = card.gameObject.AddComponent<Button>();
            AddOpacityLayer(card.transform, "Card Opaque Layer");

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;

            // Card Header Label
            Text title = CreateText("Label", card.transform, label, 17, TextAnchor.MiddleLeft, Color.white);
            title.fontStyle = FontStyle.Bold;
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(14f, -8f), new Vector2(120f, 26f), new Vector2(0f, 1f));

            if (selected)
            {
                Text activeBadge = CreateText("Active Badge", card.transform, "ACTIVE", 12, TextAnchor.MiddleRight, new Color(0.0f, 0.95f, 1.0f));
                activeBadge.fontStyle = FontStyle.Bold;
                SetRect(activeBadge.rectTransform, new Vector2(1f, 1f), new Vector2(-14f, -8f), new Vector2(70f, 26f), new Vector2(1f, 1f));
            }

            // Thumbnail Preview Container
            var previewObject = CreateUIObject("Preview", card.transform);
            RawImage preview = previewObject.AddComponent<RawImage>();
            preview.texture = texture;
            preview.color = Color.white;
            preview.raycastTarget = false;
            preview.rectTransform.anchorMin = Vector2.zero;
            preview.rectTransform.anchorMax = Vector2.one;
            preview.rectTransform.offsetMin = new Vector2(10f, 10f);
            preview.rectTransform.offsetMax = new Vector2(-10f, -34f);

            Text subname = CreateText("Car Name", card.transform, carSubname, 14, TextAnchor.LowerLeft, Color.white);
            subname.fontStyle = FontStyle.Bold;
            SetRect(subname.rectTransform, new Vector2(0f, 0f), new Vector2(14f, 8f), new Vector2(180f, 24f), new Vector2(0f, 0f));

            return button;
        }

        private static void AddOpacityLayer(Transform parent, string name)
        {
            Image layer = CreateImage(name, parent, new Color(0.035f, 0.075f, 0.15f, 0.82f));
            layer.sprite = glassPanelBg;
            layer.type = Image.Type.Sliced;
            layer.raycastTarget = false;
            Stretch(layer.rectTransform);
            layer.transform.SetAsFirstSibling();
        }

        private static Button CreateGlassButton(string name, string label, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, bool isPrimary)
        {
            Image image = CreateImage(name, canvas.transform, Color.white);
            image.sprite = isPrimary ? glassButtonPrimary : glassButtonNormal;
            image.type = Image.Type.Sliced;
            SetRect(image.rectTransform, anchor, position, size, new Vector2(0.5f, 0.5f));

            Button button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.90f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.72f, 0.82f, 0.88f, 1f);
            colors.selectedColor = colors.normalColor;
            button.colors = colors;

            Shadow shadow = image.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.60f);
            shadow.effectDistance = new Vector2(2f, -3f);

            Text text = CreateText("Label", image.transform, label, fontSize, TextAnchor.MiddleCenter, Color.white);
            text.fontStyle = FontStyle.Bold;
            Shadow textShadow = text.gameObject.AddComponent<Shadow>();
            textShadow.effectColor = isPrimary ? new Color(0f, 0.3f, 0.15f, 0.8f) : new Color(0f, 0f, 0f, 0.8f);
            textShadow.effectDistance = new Vector2(1f, -1f);
            Stretch(text.rectTransform);

            return button;
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
