using System.Collections;
using SuperRacing.Data;
using SuperRacing.Economy;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SuperRacing.UI
{
    [DisallowMultipleComponent]
    public sealed class GarageUI : MonoBehaviour
    {
        private const int GarageTargetFrameRate = 120;
        [SerializeField] private GameCatalog catalog;
        [SerializeField] private Text carNameLabel;
        [SerializeField] private Text secondaryCarNameLabel;
        [SerializeField] private Text statsLabel;
        [SerializeField] private Image previewImage;
        [SerializeField] private Image powerFill;
        [SerializeField] private Image accelerationFill;
        [SerializeField] private Image handlingFill;
        [SerializeField] private Image gripFill;
        [SerializeField] private Text powerValueLabel;
        [SerializeField] private Text accelerationValueLabel;
        [SerializeField] private Text handlingValueLabel;
        [SerializeField] private Text gripValueLabel;
        [SerializeField] private Transform vehiclePreviewRoot;
        [SerializeField, Min(0.1f)] private float previewTargetSize = 3.25f;
        [SerializeField] private Vector3 vehiclePositionOffset = new Vector3(0f, -0.96f, 0f);
        [SerializeField] private Vector3 vehicleRotationEuler = new Vector3(0f, 8f, 0f);
        [SerializeField] private string trackSelectionSceneName = "TrackSelection";
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private static readonly Color SelectedCardColor = new(0f, 0.82f, 1f, 1f);
        private static readonly Color UnselectedCardColor = new(0.36f, 0.72f, 0.9f, 0.5f);
        private static readonly Color SelectedOverlayColor = new(0.0f, 0.18f, 0.28f, 0.3f);
        private static readonly Color UnselectedOverlayColor = new(0.02f, 0.05f, 0.1f, 0.82f);

        private int selectedIndex;
        private GameObject previewVehicle;
        private Button[] carCardButtons;
        private Image[] carCardBackgrounds;
        private Image[] carCardOpaqueLayers;
        private Text[] carCardActiveLabels;
        private int previousTargetFrameRate;
        private Button continueButton;
        private Text continueButtonLabel;
        private readonly Button[] upgradeButtons = new Button[4];
        private GameObject paintStrip;
        private GameObject confirmationOverlay;

        private void Awake()
        {
            previousTargetFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = Mathf.Max(previousTargetFrameRate, GarageTargetFrameRate);
        }

        private void Start()
        {
            if (!ValidateConfiguration())
            {
                enabled = false;
                return;
            }

            selectedIndex = FindSelectedCarIndex();
            ResolveShopControls();
            CreateInlineShopControls();
            RefreshView();
            StartCoroutine(FreezeThumbnailCamerasAfterFirstFrame());
        }

        public void SelectCar(int index)
        {
            if (catalog == null || catalog.Cars.Count == 0)
            {
                return;
            }

            selectedIndex = WrapIndex(index, catalog.Cars.Count);
            RefreshView();
        }

        public void SelectPrevious()
        {
            selectedIndex = WrapIndex(selectedIndex - 1, catalog.Cars.Count);
            RefreshView();
        }

        public void SelectNext()
        {
            selectedIndex = WrapIndex(selectedIndex + 1, catalog.Cars.Count);
            RefreshView();
        }

        public void ConfirmSelection()
        {
            if (catalog == null || catalog.Cars.Count == 0)
            {
                return;
            }

            CarDefinition selectedCar = catalog.Cars[selectedIndex];
            if (!CarOwnership.IsOwned(selectedCar))
            {
                ShowPurchaseConfirmation(
                    $"BUY {selectedCar.DisplayName.ToUpperInvariant()}?",
                    selectedCar.PurchasePrice,
                    () =>
                    {
                        CarOwnership.TryPurchase(selectedCar);
                        RefreshView();
                    });
                return;
            }

            GameSelection.SelectCar(selectedCar);
            SceneManager.LoadScene(trackSelectionSceneName);
        }

        public void ReturnToMainMenu()
        {
            if (catalog != null && catalog.Cars.Count > 0)
            {
                GameSelection.SelectCar(catalog.Cars[selectedIndex]);
            }

            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void OnDestroy()
        {
            Application.targetFrameRate = previousTargetFrameRate;
        }

        public bool ValidateConfiguration()
        {
            if (catalog == null || catalog.Cars.Count == 0 || carNameLabel == null)
            {
                Debug.LogError("GarageUI requires a catalog with at least one car and a car name label.", this);
                return false;
            }

            return true;
        }

        public static int WrapIndex(int index, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            return (index % count + count) % count;
        }

        private int FindSelectedCarIndex()
        {
            if (!GameSelection.HasCar)
            {
                return 0;
            }

            for (int index = 0; index < catalog.Cars.Count; index++)
            {
                if (catalog.Cars[index] == GameSelection.SelectedCar)
                {
                    return index;
                }
            }

            return 0;
        }

        private void RefreshView()
        {
            CarDefinition car = catalog.Cars[selectedIndex];
            carNameLabel.text = car.DisplayName;
            if (secondaryCarNameLabel != null)
            {
                secondaryCarNameLabel.text = car.DisplayName;
            }

            if (statsLabel != null)
            {
                statsLabel.text =
                    $"Top Speed  {car.MaxSpeedPercent:0}%\n" +
                    $"Acceleration  {car.AccelerationPercent:0}%\n" +
                    $"Steering  {car.SteeringPercent:0}%\n" +
                    $"Grip  {car.GripPercent:0}%";
            }

            SetStat(powerFill, powerValueLabel, car.MaxSpeedPercent / 100f);
            SetStat(accelerationFill, accelerationValueLabel, car.AccelerationPercent / 100f);
            SetStat(handlingFill, handlingValueLabel, car.SteeringPercent / 100f);
            SetStat(gripFill, gripValueLabel, car.GripPercent / 100f);

            if (previewImage != null)
            {
                previewImage.sprite = car.PreviewSprite;
                previewImage.enabled = car.PreviewSprite != null;
            }

            RefreshCardSelection();
            RefreshVehiclePreview(car);
            RefreshShopControls(car);
            RefreshInlineShopControls(car);
        }

        private void ResolveShopControls()
        {
            GameObject buttonObject = GameObject.Find("Continue");
            continueButton = buttonObject != null ? buttonObject.GetComponent<Button>() : null;
            continueButtonLabel = buttonObject != null ? buttonObject.GetComponentInChildren<Text>(true) : null;
        }

        private void RefreshShopControls(CarDefinition car)
        {
            if (continueButtonLabel == null)
            {
                ResolveShopControls();
            }

            bool owned = CarOwnership.IsOwned(car);
            bool selected = GameSelection.SelectedCar == car;
            if (continueButtonLabel != null)
            {
                continueButtonLabel.text = owned
                    ? selected ? "SELECTED" : "SELECT   ▶"
                    : $"BUY  {car.PurchasePrice:N0} ◆";
            }

            if (continueButton != null)
            {
                continueButton.interactable = owned || CurrencyWallet.Balance >= car.PurchasePrice;
            }

        }

        private void CreateInlineShopControls()
        {
            CreateInlineUpgradeButton(powerValueLabel, CarUpgradeType.TopSpeed, 0);
            CreateInlineUpgradeButton(accelerationValueLabel, CarUpgradeType.Acceleration, 1);
            CreateInlineUpgradeButton(handlingValueLabel, CarUpgradeType.Steering, 2);
            CreateInlineUpgradeButton(gripValueLabel, CarUpgradeType.Grip, 3);

            Canvas canvas = ResolveCanvas();
            if (canvas == null) return;
            paintStrip = CreateRuntimeObject("Inline Paint Strip", canvas.transform);
            SetUiRect(paintStrip.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-240f, 168f), new Vector2(410f, 72f));
            Image background = paintStrip.AddComponent<Image>();
            background.color = new Color(0.02f, 0.07f, 0.12f, 0.88f);
        }

        private void CreateInlineUpgradeButton(Text valueLabel, CarUpgradeType type, int slot)
        {
            if (valueLabel == null || valueLabel.transform.parent == null) return;
            RectTransform valueRect = valueLabel.rectTransform;
            valueRect.anchoredPosition += new Vector2(-42f, 0f);

            Button button = CreateRuntimeButton($"Upgrade {type}", valueLabel.transform.parent, "+", new Color(0.04f, 0.62f, 0.76f, 1f));
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(34f, 28f);
            button.onClick.AddListener(() => RequestUpgrade(type));
            upgradeButtons[slot] = button;
        }

        private void RefreshInlineShopControls(CarDefinition car)
        {
            CarUpgradeType[] types = { CarUpgradeType.TopSpeed, CarUpgradeType.Acceleration, CarUpgradeType.Steering, CarUpgradeType.Grip };
            bool ownedCar = CarOwnership.IsOwned(car);
            for (int index = 0; index < upgradeButtons.Length; index++)
            {
                Button button = upgradeButtons[index];
                if (button == null) continue;
                int price = CarProgression.GetUpgradePrice(car, types[index]);
                bool canUpgrade = CarProgression.CanUpgrade(car, types[index]);
                button.interactable = ownedCar && canUpgrade && CurrencyWallet.Balance >= price;
                Text text = button.GetComponentInChildren<Text>();
                if (text != null) text.text = canUpgrade ? "+" : "MAX";
            }

            RebuildPaintStrip(car, ownedCar);
        }

        private void RebuildPaintStrip(CarDefinition car, bool ownedCar)
        {
            if (paintStrip == null) return;
            for (int index = paintStrip.transform.childCount - 1; index >= 0; index--)
            {
                Destroy(paintStrip.transform.GetChild(index).gameObject);
            }

            Text heading = CreateRuntimeText("Paint Label", paintStrip.transform, "PAINT", 13, FontStyle.Bold);
            heading.alignment = TextAnchor.MiddleLeft;
            SetUiRect(heading.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(42f, 0f), new Vector2(70f, 52f));

            int equipped = CarProgression.GetEquippedPaint(car);
            for (int index = 0; index < CarProgression.PaintCount; index++)
            {
                int paintIndex = index;
                bool paintOwned = CarProgression.IsPaintOwned(car, index);
                string caption = index == 0 ? (index == equipped ? "OEM ✓" : "OEM") : index == equipped ? "✓" : paintOwned ? "" : "◆";
                Button swatch = CreateRuntimeButton($"Paint {index}", paintStrip.transform, caption, CarProgression.GetPaintColor(index));
                SetUiRect(swatch.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(100f + index * 51f, 0f), new Vector2(40f, 40f));
                swatch.interactable = ownedCar && (paintOwned || CurrencyWallet.Balance >= CarProgression.GetPaintPrice(index));
                Text swatchText = swatch.GetComponentInChildren<Text>();
                if (swatchText != null)
                {
                    swatchText.fontSize = index == 0 ? 10 : 16;
                    Color swatchColor = CarProgression.GetPaintColor(index);
                    float luminance = swatchColor.r * 0.299f + swatchColor.g * 0.587f + swatchColor.b * 0.114f;
                    swatchText.color = luminance > 0.62f ? new Color(0.03f, 0.05f, 0.08f, 1f) : Color.white;
                }
                swatch.onClick.AddListener(() => RequestPaint(paintIndex));
            }
        }

        private void RequestUpgrade(CarUpgradeType type)
        {
            CarDefinition car = catalog.Cars[selectedIndex];
            int price = CarProgression.GetUpgradePrice(car, type);
            int nextLevel = CarProgression.GetUpgradeLevel(car, type) + 1;
            ShowPurchaseConfirmation(
                $"UPGRADE {type.ToString().ToUpperInvariant()} TO LV {nextLevel}?",
                price,
                () =>
                {
                    CarProgression.TryUpgrade(car, type);
                    RefreshView();
                });
        }

        private void RequestPaint(int paintIndex)
        {
            CarDefinition car = catalog.Cars[selectedIndex];
            if (CarProgression.IsPaintOwned(car, paintIndex))
            {
                CarProgression.TryBuyAndEquipPaint(car, paintIndex);
                RefreshView();
                return;
            }

            ShowPurchaseConfirmation(
                "BUY THIS PAINT?",
                CarProgression.GetPaintPrice(paintIndex),
                () =>
                {
                    CarProgression.TryBuyAndEquipPaint(car, paintIndex);
                    RefreshView();
                });
        }

        private void ShowPurchaseConfirmation(string titleText, int price, System.Action confirmAction)
        {
            CloseConfirmation();
            Canvas canvas = ResolveCanvas();
            if (canvas == null) return;

            confirmationOverlay = CreateRuntimeObject("Purchase Confirmation", canvas.transform);
            Stretch(confirmationOverlay.GetComponent<RectTransform>());
            Image dim = confirmationOverlay.AddComponent<Image>();
            dim.color = new Color(0f, 0.01f, 0.025f, 0.78f);

            GameObject panel = CreateRuntimeObject("Confirmation Panel", confirmationOverlay.transform);
            SetUiRect(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(470f, 250f));
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.025f, 0.08f, 0.14f, 0.99f);

            Text title = CreateRuntimeText("Title", panel.transform, titleText, 22, FontStyle.Bold);
            title.color = new Color(0.2f, 0.9f, 1f, 1f);
            SetUiRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(410f, 50f));
            Text cost = CreateRuntimeText("Cost", panel.transform, $"PRICE  ◆ {price:N0}", 19, FontStyle.Bold);
            SetUiRect(cost.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -108f), new Vector2(390f, 38f));

            Button cancel = CreateRuntimeButton("Cancel", panel.transform, "CANCEL", new Color(0.2f, 0.25f, 0.31f, 1f));
            SetUiRect(cancel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-108f, 48f), new Vector2(180f, 50f));
            cancel.onClick.AddListener(CloseConfirmation);
            Button confirm = CreateRuntimeButton("Confirm", panel.transform, "CONFIRM", new Color(0.03f, 0.55f, 0.7f, 1f));
            SetUiRect(confirm.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(108f, 48f), new Vector2(180f, 50f));
            confirm.onClick.AddListener(() =>
            {
                confirmAction?.Invoke();
                CloseConfirmation();
            });
        }

        private void CloseConfirmation()
        {
            if (confirmationOverlay != null) Destroy(confirmationOverlay);
            confirmationOverlay = null;
        }

        private static Button CreateRuntimeButton(string name, Transform parent, string caption, Color color)
        {
            GameObject buttonObject = CreateRuntimeObject(name, parent);
            Image image = buttonObject.AddComponent<Image>();
            image.color = color;
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            Text text = CreateRuntimeText("Text", buttonObject.transform, caption, 16, FontStyle.Bold);
            text.color = Color.white;
            return button;
        }

        private static Text CreateRuntimeText(string name, Transform parent, string caption, int fontSize, FontStyle style)
        {
            GameObject textObject = CreateRuntimeObject(name, parent);
            Text text = textObject.AddComponent<Text>();
            text.font = ResolveFont();
            text.text = caption;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            Stretch(text.rectTransform);
            return text;
        }

        private static GameObject CreateRuntimeObject(string name, Transform parent)
        {
            GameObject result = new(name, typeof(RectTransform));
            result.layer = parent.gameObject.layer;
            result.transform.SetParent(parent, false);
            return result;
        }

        private static Canvas ResolveCanvas()
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int index = 0; index < canvases.Length; index++)
            {
                if (canvases[index] != null && canvases[index].isRootCanvas)
                {
                    return canvases[index];
                }
            }

            return canvases.Length > 0 ? canvases[0] : null;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetUiRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetStat(Image fill, Text valueLabel, float normalizedValue)
        {
            float value = Mathf.Clamp01(normalizedValue);
            if (fill != null)
            {
                fill.fillAmount = value;
            }

            if (valueLabel != null)
            {
                valueLabel.text = $"{value * 100f:0} %";
            }
        }

        private void RefreshVehiclePreview(CarDefinition car)
        {
            if (previewVehicle != null)
            {
                Destroy(previewVehicle);
            }

            if (vehiclePreviewRoot == null || car.VehiclePrefab == null)
            {
                return;
            }

            vehiclePreviewRoot.rotation = Quaternion.identity;
            previewVehicle = Instantiate(car.VehiclePrefab, vehiclePreviewRoot, false);
            previewVehicle.name = $"{car.DisplayName} Preview";
            previewVehicle.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            CarProgression.ApplyPaint(previewVehicle, car);

            SetLayerRecursively(previewVehicle, vehiclePreviewRoot.gameObject.layer);
            DisableVehicleBehaviour(previewVehicle);
            FitPreviewVehicle(previewVehicle);
        }


        private static IEnumerator FreezeThumbnailCamerasAfterFirstFrame()
        {
            yield return null;

            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Camera camera in cameras)
            {
                if (camera.targetTexture != null && camera.name.StartsWith("Thumbnail Camera -"))
                {
                    camera.enabled = false;
                }
            }
        }
        private void FitPreviewVehicle(GameObject vehicle)
        {
            Renderer[] renderers = vehicle.GetComponentsInChildren<Renderer>();
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
                vehicle.transform.localScale *= previewTargetSize / largestSize;
            }

            bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            // Align horizontally to center, align bottom of wheels to ground level + offset
            Vector3 targetPosition = vehiclePreviewRoot.position;
            targetPosition.x -= bounds.center.x;
            targetPosition.z -= bounds.center.z;
            targetPosition.y -= bounds.min.y;
            targetPosition += vehiclePositionOffset;

            vehicle.transform.position = targetPosition;
            vehicle.transform.localRotation = Quaternion.Euler(vehicleRotationEuler);
        }

        private static void DisableVehicleBehaviour(GameObject vehicle)
        {
            foreach (MonoBehaviour behaviour in vehicle.GetComponentsInChildren<MonoBehaviour>(true))
            {
                behaviour.enabled = false;
            }

            foreach (Rigidbody body in vehicle.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.useGravity = false;
            }

            foreach (Collider collider in vehicle.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
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

        private void RefreshCardSelection()
        {
            EnsureCarCardReferences();
            if (carCardButtons == null)
            {
                return;
            }

            for (int index = 0; index < carCardButtons.Length; index++)
            {
                bool selected = index == selectedIndex;

                if (carCardBackgrounds[index] != null)
                {
                    carCardBackgrounds[index].color = selected ? SelectedCardColor : UnselectedCardColor;
                }

                if (carCardOpaqueLayers[index] != null)
                {
                    carCardOpaqueLayers[index].color = selected ? SelectedOverlayColor : UnselectedOverlayColor;
                }

                if (carCardActiveLabels[index] != null)
                {
                    carCardActiveLabels[index].gameObject.SetActive(selected);
                }

                if (carCardButtons[index] != null)
                {
                    ColorBlock colors = carCardButtons[index].colors;
                    colors.normalColor = selected ? Color.white : new Color(0.72f, 0.84f, 0.9f, 0.88f);
                    colors.selectedColor = colors.normalColor;
                    colors.highlightedColor = selected ? Color.white : new Color(0.88f, 0.98f, 1f, 1f);
                    carCardButtons[index].colors = colors;
                }
            }
        }

        private void EnsureCarCardReferences()
        {
            int cardCount = catalog != null && catalog.Cars.Count > 0 ? catalog.Cars.Count : 3;
            if (carCardButtons != null && carCardButtons.Length == cardCount)
            {
                return;
            }

            carCardButtons = new Button[cardCount];
            carCardBackgrounds = new Image[cardCount];
            carCardOpaqueLayers = new Image[cardCount];
            carCardActiveLabels = new Text[cardCount];

            for (int index = 0; index < cardCount; index++)
            {
                GameObject cardObject = GameObject.Find($"CAR {index + 1:00} Card");
                if (cardObject == null)
                {
                    continue;
                }

                carCardButtons[index] = cardObject.GetComponent<Button>();
                carCardBackgrounds[index] = cardObject.GetComponent<Image>();
                carCardOpaqueLayers[index] = FindChildImage(cardObject.transform, "Card Opaque Layer");
                carCardActiveLabels[index] = FindActiveBadge(cardObject.transform);
            }
        }

        private static Image FindChildImage(Transform root, string childName)
        {
            Transform child = FindChildByName(root, childName);
            return child != null ? child.GetComponent<Image>() : null;
        }

        private static Text FindActiveBadge(Transform card)
        {
            Transform badge = FindChildByName(card, "Active Badge");
            if (badge != null && badge.TryGetComponent(out Text existingText))
            {
                return existingText;
            }

            GameObject badgeObject = new("Active Badge");
            badgeObject.transform.SetParent(card, false);
            Text badgeText = badgeObject.AddComponent<Text>();
            badgeText.font = ResolveFont();
            badgeText.text = "ACTIVE";
            badgeText.fontSize = 12;
            badgeText.fontStyle = FontStyle.Bold;
            badgeText.alignment = TextAnchor.MiddleRight;
            badgeText.color = SelectedCardColor;
            badgeText.raycastTarget = false;

            RectTransform rect = badgeText.rectTransform;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-14f, -8f);
            rect.sizeDelta = new Vector2(70f, 26f);
            return badgeText;
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            for (int index = 0; index < root.childCount; index++)
            {
                Transform child = root.GetChild(index);
                if (child.name == childName)
                {
                    return child;
                }

                Transform result = FindChildByName(child, childName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static Font ResolveFont()
        {
            Text existingText = FindFirstObjectByType<Text>(FindObjectsInactive.Include);
            if (existingText != null && existingText.font != null)
            {
                return existingText.font;
            }

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
