using SuperRacing.Economy;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SuperRacing.UI
{
    [DisallowMultipleComponent]
    public sealed class CurrencyDisplay : MonoBehaviour
    {
        private Text label;
        private GameObject topUpOverlay;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            AttachToKnownLabels();
        }

        private static void AttachToKnownLabels()
        {
            foreach (Text text in FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (text.name == "Currency Text" || text.name == "Money Label")
                {
                    if (text.GetComponent<CurrencyDisplay>() == null)
                    {
                        text.gameObject.AddComponent<CurrencyDisplay>();
                    }
                }
            }
        }

        private void Awake()
        {
            label = GetComponent<Text>();
            CreateTopUpButton();
            Refresh(CurrencyWallet.Balance);
        }

        private void OnEnable() => CurrencyWallet.BalanceChanged += Refresh;
        private void OnDisable() => CurrencyWallet.BalanceChanged -= Refresh;

        private void Refresh(int balance)
        {
            if (label != null) label.text = $"◆  {balance:N0}";
        }

        private void CreateTopUpButton()
        {
            if (transform.Find("Top Up Button") != null) return;

            GameObject buttonObject = CreateUiObject("Top Up Button", transform);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(8f, 0f);
            rect.sizeDelta = new Vector2(34f, 34f);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.08f, 0.72f, 0.88f, 0.95f);
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(OpenTopUpPanel);

            Text plus = CreateText("Plus", buttonObject.transform, "+", 25, FontStyle.Bold);
            plus.color = Color.white;
        }

        private void OpenTopUpPanel()
        {
            if (topUpOverlay != null)
            {
                topUpOverlay.SetActive(true);
                topUpOverlay.transform.SetAsLastSibling();
                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            topUpOverlay = CreateUiObject("Top Up Overlay", canvas.transform);
            Stretch(topUpOverlay.GetComponent<RectTransform>());
            Image dim = topUpOverlay.AddComponent<Image>();
            dim.color = new Color(0.01f, 0.025f, 0.05f, 0.78f);

            Button dismiss = topUpOverlay.AddComponent<Button>();
            dismiss.targetGraphic = dim;
            dismiss.onClick.AddListener(CloseTopUpPanel);

            GameObject panel = CreateUiObject("Top Up Panel", topUpOverlay.transform);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(460f, 430f);
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.025f, 0.07f, 0.12f, 0.98f);

            Text title = CreateText("Title", panel.transform, "NẠP TIỀN", 30, FontStyle.Bold);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -45f), new Vector2(380f, 48f));

            Text note = CreateText("Note", panel.transform, "Chọn gói để nhận tiền ngay", 17, FontStyle.Normal);
            note.color = new Color(0.65f, 0.78f, 0.88f, 1f);
            SetRect(note.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(380f, 34f));

            CreatePackageButton(panel.transform, 1000, -155f);
            CreatePackageButton(panel.transform, 5000, -235f);
            CreatePackageButton(panel.transform, 20000, -315f);

            Button close = CreateButton("Close Button", panel.transform, "ĐÓNG", new Color(0.18f, 0.23f, 0.29f, 1f));
            SetRect(close.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -382f), new Vector2(150f, 42f));
            close.onClick.AddListener(CloseTopUpPanel);
        }

        private void CreatePackageButton(Transform parent, int amount, float y)
        {
            Button button = CreateButton($"Top Up {amount}", parent, $"◆  {amount:N0}", new Color(0.04f, 0.45f, 0.62f, 1f));
            SetRect(button.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(340f, 60f));
            button.onClick.AddListener(() =>
            {
                CurrencyWallet.Add(amount);
                CloseTopUpPanel();
            });
        }

        private void CloseTopUpPanel()
        {
            if (topUpOverlay != null) topUpOverlay.SetActive(false);
        }

        private static Button CreateButton(string name, Transform parent, string caption, Color color)
        {
            GameObject buttonObject = CreateUiObject(name, parent);
            Image image = buttonObject.AddComponent<Image>();
            image.color = color;
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            Text text = CreateText("Text", buttonObject.transform, caption, 22, FontStyle.Bold);
            text.color = Color.white;
            return button;
        }

        private static Text CreateText(string name, Transform parent, string value, int size, FontStyle style)
        {
            GameObject textObject = CreateUiObject(name, parent);
            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            Stretch(text.rectTransform);
            return text;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject result = new GameObject(name, typeof(RectTransform));
            result.layer = parent.gameObject.layer;
            result.transform.SetParent(parent, false);
            return result;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
