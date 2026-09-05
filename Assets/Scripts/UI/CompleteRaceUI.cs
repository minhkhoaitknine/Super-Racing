using SuperRacing.Race;
using SuperRacing.Economy;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SuperRacing.UI
{
    [DisallowMultipleComponent]
    public sealed class CompleteRaceUI : MonoBehaviour
    {
        [SerializeField] private string garageSceneName = "Garage";

        private void Awake()
        {
            Time.timeScale = 1f;
            BuildUi();
        }

        public void ReturnToGarage()
        {
            SceneManager.LoadScene(garageSceneName);
        }

        private void BuildUi()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Canvas canvas = new GameObject("Complete Race Canvas").AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvas.gameObject.AddComponent<GraphicRaycaster>();

            Image background = new GameObject("Placeholder Background").AddComponent<Image>();
            background.transform.SetParent(canvas.transform, false);
            background.color = new Color(0.02f, 0.08f, 0.12f, 1f);
            RectTransform backgroundRect = background.rectTransform;
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            Text title = CreateLabel("Title", canvas.transform, font, 72, TextAnchor.MiddleCenter);
            title.text = "RACE COMPLETE";
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -230f), new Vector2(900f, 110f));

            Text time = CreateLabel("Final Time", canvas.transform, font, 42, TextAnchor.MiddleCenter);
            time.text = $"TIME  {RaceHUD.FormatTime(RaceCompletionState.FinalTimeSeconds)}";
            SetRect(time.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -350f), new Vector2(700f, 78f));

            Text details = CreateLabel("Details", canvas.transform, font, 26, TextAnchor.MiddleCenter);
            string record = RaceCompletionState.SetNewRecord ? "NEW RECORD" : $"{RaceCompletionState.TrackName}  /  {RaceCompletionState.CarName}";
            details.text = record;
            SetRect(details.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -430f), new Vector2(760f, 54f));

            RaceRewardSummary rewards = RaceCompletionState.Rewards;
            Text rewardDetails = CreateLabel("Rewards", canvas.transform, font, 24, TextAnchor.MiddleCenter);
            rewardDetails.text =
                $"FINISH  +{rewards.CompletionReward:N0} ◆\n" +
                $"NEW RECORD  +{rewards.NewRecordBonus:N0} ◆\n" +
                $"CLEAN DRIFT  +{rewards.CleanDriftBonus:N0} ◆\n" +
                $"TOTAL  +{rewards.Total:N0} ◆     BALANCE  {RaceCompletionState.WalletBalance:N0} ◆";
            rewardDetails.color = new Color(1f, 0.84f, 0.2f);
            SetRect(rewardDetails.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -560f), new Vector2(880f, 150f));

            Button garageButton = CreateButton("Garage Button", canvas.transform, font, "GARAGE");
            SetRect(garageButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 220f), new Vector2(320f, 72f));
            garageButton.onClick.AddListener(ReturnToGarage);
        }

        private static Text CreateLabel(string name, Transform parent, Font font, int size, TextAnchor alignment)
        {
            GameObject labelObject = new(name);
            labelObject.transform.SetParent(parent, false);
            Text label = labelObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static Button CreateButton(string name, Transform parent, Font font, string label)
        {
            GameObject buttonObject = new(name);
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.0f, 0.78f, 0.92f, 1f);

            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.18f, 0.9f, 1f, 1f);
            colors.pressedColor = new Color(0.0f, 0.55f, 0.7f, 1f);
            button.colors = colors;

            Text text = CreateLabel("Label", buttonObject.transform, font, 24, TextAnchor.MiddleCenter);
            text.text = label;
            text.color = new Color(0.01f, 0.05f, 0.08f, 1f);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }
    }
}
