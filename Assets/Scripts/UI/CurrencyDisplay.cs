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
        private Text balanceLabel;

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
            if (balanceLabel != null) balanceLabel.text = $"CURRENT BALANCE   {balance:N0} CREDITS";
        }

        private void CreateTopUpButton()
        {
            if (transform.Find("Top Up Button") != null) return;

            GameObject buttonObject = CreateUiObject("Top Up Button", transform);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 0f);
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
            RectTransform panel = RacingUIStyle.Modal(topUpOverlay.transform, "ADD CREDITS",
                "Choose a credit pack for your next garage upgrade.",
                new Vector2(920f, 550f), CloseTopUpPanel);
            Text balance = RacingUIStyle.Label(panel, $"CURRENT BALANCE   {CurrencyWallet.Balance:N0} CREDITS", 20, RacingUIStyle.Accent);
            balanceLabel = balance;
            RacingUIStyle.Place(balance.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(0, -140), new Vector2(-64, 40));
            CreatePackageButton(panel, 1000, -286f, "STARTER");
            CreatePackageButton(panel, 5000, 0f, "BOOST");
            CreatePackageButton(panel, 20000, 286f, "PRO");
            Text note = RacingUIStyle.Label(panel, "Credits are added instantly. No payment required.", 18, RacingUIStyle.Muted);
            note.alignment = TextAnchor.MiddleCenter;
            RacingUIStyle.Place(note.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(0, 82), new Vector2(-64, 36));
            Button close = RacingUIStyle.Button(panel, "BACK TO GARAGE", CloseTopUpPanel);
            RacingUIStyle.Place(close.GetComponent<RectTransform>(), new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 37), new Vector2(260, 44));
        }

        private void CreatePackageButton(Transform parent, int amount, float x, string title)
        {
            Image card = RacingUIStyle.Box(parent, title + " Pack", new Color(.055f, .105f, .15f));
            RacingUIStyle.Place(card.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(x, -286), new Vector2(264, 226));
            Text heading = RacingUIStyle.Label(card.transform, title, 17, RacingUIStyle.Muted);
            heading.alignment = TextAnchor.MiddleCenter;
            RacingUIStyle.Place(heading.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(0, -29), new Vector2(0, 30));
            Text value = RacingUIStyle.Label(card.transform, amount.ToString("N0"), 38);
            value.fontStyle = FontStyle.Bold; value.alignment = TextAnchor.MiddleCenter;
            RacingUIStyle.Place(value.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(0, -82), new Vector2(0, 50));
            Text unit = RacingUIStyle.Label(card.transform, "CREDITS", 17, RacingUIStyle.Accent);
            unit.alignment = TextAnchor.MiddleCenter;
            RacingUIStyle.Place(unit.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(0, -123), new Vector2(0, 30));
            Button button = RacingUIStyle.Button(card.transform, "ADD CREDITS", () =>
            {
                CurrencyWallet.Add(amount);
                CloseTopUpPanel();
            }, true);
            button.name = $"Top Up {amount}";
            RacingUIStyle.Place(button.GetComponent<RectTransform>(), new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 40), new Vector2(224, 48));
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
