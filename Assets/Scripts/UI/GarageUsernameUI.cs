using System;
using SuperRacing.Race;
using UnityEngine;
using UnityEngine.UI;

namespace SuperRacing.UI
{
    public sealed class GarageUsernameUI : MonoBehaviour
    {
        private Text label;
        private GlobalLeaderboardService service;
        private GameObject dialog;
        private InputField input;
        private Text message;
        private Button save;
        private Button cancel;
        private bool saving;

        public static void Install(Canvas canvas)
        {
            if (canvas == null) return;
            foreach (Text text in canvas.GetComponentsInChildren<Text>(true))
            {
                if (text.name != "Player Name") continue;
                if (text.GetComponent<GarageUsernameUI>() == null)
                    text.gameObject.AddComponent<GarageUsernameUI>().Configure(text);
                return;
            }
        }

        private async void Configure(Text text)
        {
            label = text;
            label.supportRichText = false;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = 19;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            Image badge = text.transform.parent.GetComponent<Image>();
            badge.raycastTarget = true;
            label.raycastTarget = false;
            Button button = badge.GetComponent<Button>() ?? badge.gameObject.AddComponent<Button>();
            button.targetGraphic = badge;
            button.onClick.AddListener(Open);
            service = GlobalLeaderboardService.Instance;
            if (service == null) return;
            service.UsernameChanged += RefreshLabel;
            RefreshLabel();
            try { await service.LoadUsernameAsync(); }
            catch (Exception) { /* The cached name remains visible offline. Saving offers retry. */ }
        }

        private void RefreshLabel()
        {
            if (label != null) label.text = service.Username;
        }

        public void Open()
        {
            if (dialog != null) return;
            dialog = new GameObject("Edit Username Dialog", typeof(RectTransform));
            Canvas canvas = dialog.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 300;
            CanvasScaler scaler = dialog.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            dialog.AddComponent<GraphicRaycaster>();
            Image dim = ImageUnder(dialog.transform, "Dim", new Color(0f, 0.015f, 0.03f, 0.86f));
            Stretch(dim.rectTransform);
            Image panel = ImageUnder(dialog.transform, "Panel", new Color(0.025f, 0.065f, 0.11f));
            Place(panel.rectTransform, 0, 0, 660, 370);
            Text heading = TextUnder(panel.transform, "EDIT USERNAME", 30);
            Place(heading.rectTransform, 0, 125, 580, 48);
            Text hint = TextUnder(panel.transform, "Shown in your garage and global rankings", 20);
            Place(hint.rectTransform, 0, 76, 580, 35);
            Image field = ImageUnder(panel.transform, "Username Input", new Color(0.09f, 0.15f, 0.21f));
            Place(field.rectTransform, 0, 16, 550, 60);
            input = field.gameObject.AddComponent<InputField>();
            input.targetGraphic = field;
            input.characterLimit = 24;
            input.lineType = InputField.LineType.SingleLine;
            Text inputText = TextUnder(field.transform, "", 26);
            Stretch(inputText.rectTransform, 16);
            inputText.alignment = TextAnchor.MiddleLeft;
            input.textComponent = inputText;
            input.text = service != null && service.Username != "PLAYER" ? service.Username : "";
            message = TextUnder(panel.transform, "1–24 characters, no spaces", 19);
            Place(message.rectTransform, 0, -49, 590, 54);
            cancel = ButtonUnder(panel.transform, "CANCEL", -145, Close);
            save = ButtonUnder(panel.transform, "SAVE", 145, Save);
            input.onValueChanged.AddListener(_ => { message.text = "1–24 characters, no spaces"; });
            input.ActivateInputField();
        }

        private async void Save()
        {
            if (saving) return;
            string error = GlobalLeaderboardService.ValidateUsername(input.text);
            if (error != null) { message.text = error; return; }
            saving = true;
            save.interactable = cancel.interactable = input.interactable = false;
            message.text = "Saving...";
            try
            {
                if (service == null) throw new InvalidOperationException();
                await service.ChangeUsernameAsync(input.text);
                if (this == null) return;
                saving = false;
                Close();
            }
            catch (Exception)
            {
                if (this == null || dialog == null) return;
                message.text = "Could not save. Check your connection or try another name.";
            }
            finally
            {
                if (this != null)
                {
                    saving = false;
                    if (dialog != null) save.interactable = cancel.interactable = input.interactable = true;
                }
            }
        }

        private void Close()
        {
            if (saving) return;
            if (dialog != null) Destroy(dialog);
            dialog = null;
        }

        private void OnDestroy()
        {
            if (service != null) service.UsernameChanged -= RefreshLabel;
            if (dialog != null) Destroy(dialog);
        }

        private static Image ImageUnder(Transform parent, string name, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            return image;
        }

        private static Text TextUnder(Transform parent, string value, int size)
        {
            var text = new GameObject("Text", typeof(RectTransform)).AddComponent<Text>();
            text.transform.SetParent(parent, false);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.color = Color.white;
            text.text = value;
            text.supportRichText = false;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }

        private static Button ButtonUnder(Transform parent, string title, float x, UnityEngine.Events.UnityAction action)
        {
            var image = ImageUnder(parent, title, title == "SAVE" ? new Color(0f, 0.65f, 0.8f) : new Color(0.14f, 0.22f, 0.3f));
            Place(image.rectTransform, x, -125, 260, 54);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            Stretch(TextUnder(image.transform, title, 22).rectTransform);
            return button;
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect, float padding = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, 0);
            rect.offsetMax = new Vector2(-padding, 0);
        }
    }
}
