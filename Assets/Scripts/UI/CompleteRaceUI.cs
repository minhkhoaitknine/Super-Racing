using SuperRacing.Race;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SuperRacing.UI
{
    [DisallowMultipleComponent]
    public sealed class CompleteRaceUI : MonoBehaviour
    {
        [SerializeField] private string garageSceneName = "Garage";
        private Text globalStatus;

        public static void Show()
        {
            if (FindFirstObjectByType<CompleteRaceUI>() == null)
                new GameObject("Race Complete Overlay").AddComponent<CompleteRaceUI>();
        }

        private void Awake() => BuildUi();

        private void Update()
        {
            if (globalStatus != null && GlobalLeaderboardService.Instance != null)
                globalStatus.text = GlobalLeaderboardService.Instance.Status;
        }

        public void ReturnToGarage() => Navigate(garageSceneName);
        private void RestartRace() => Navigate(RaceCompletionState.Track != null ? RaceCompletionState.Track.SceneName : "Race");

        private void Navigate(string scene)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(scene);
        }

        private void BuildUi()
        {
            var canvas = new GameObject("Complete Race Canvas", typeof(RectTransform)).AddComponent<Canvas>();
            canvas.transform.SetParent(transform, false);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;
            var scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            canvas.gameObject.AddComponent<GraphicRaycaster>();

            var dim = RacingUIStyle.Box(canvas.transform, "Translucent Race Backdrop", new Color(.012f, .025f, .045f, .85f));
            RacingUIStyle.Place(dim.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var sheet = RacingUIStyle.Box(canvas.transform, "Results Card", new Color(.025f, .055f, .085f, .38f));
            RacingUIStyle.Place(sheet.rectTransform, Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, new Vector2(1080, 840));
            var stripe = RacingUIStyle.Box(sheet.transform, "Accent", RacingUIStyle.Accent);
            RacingUIStyle.Place(stripe.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(0, -2), new Vector2(0, 4));
            Label(sheet.transform, "RACE COMPLETE", 44, -64, Color.white, FontStyle.Bold, 72);
            Label(sheet.transform, RaceCompletionState.TrackName.ToUpperInvariant() + "   /   " + RaceCompletionState.CarName.ToUpperInvariant(), 22, -115, RacingUIStyle.Muted);
            Label(sheet.transform, RaceCompletionState.SetNewRecord ? "NEW PERSONAL BEST" : "FINAL RACE TIME", 18, -177,
                RaceCompletionState.SetNewRecord ? new Color(1f, .8f, .35f) : RacingUIStyle.Accent);
            Label(sheet.transform, RaceHUD.FormatTime(RaceCompletionState.FinalTimeSeconds), 80, -239, Color.white, FontStyle.Bold, 100);

            var rewards = RaceCompletionState.Rewards;
            Reward(sheet.transform, "RACE FINISH", rewards.CompletionReward, -330);
            Reward(sheet.transform, "RECORD BONUS", rewards.NewRecordBonus, -385);
            Reward(sheet.transform, "CLEAN DRIFT", rewards.CleanDriftBonus, -440);
            Reward(sheet.transform, "TOTAL EARNED", rewards.Total, -510, true);
            Label(sheet.transform, $"WALLET BALANCE   {RaceCompletionState.WalletBalance:N0} CREDITS", 19, -578, RacingUIStyle.Muted);
            globalStatus = Label(sheet.transform, "", 18, -628, RacingUIStyle.Accent);

            var retry = RacingUIStyle.Button(sheet.transform, "RACE AGAIN", RestartRace, true);
            var garage = RacingUIStyle.Button(sheet.transform, "GARAGE", ReturnToGarage);
            var ranking = GlobalLeaderboardPanel.AddButton(sheet.transform, () => RaceCompletionState.Track,
                new Vector2(.5f, 0), new Vector2(305, 100));
            RacingUIStyle.Place(retry.GetComponent<RectTransform>(), new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-305, 100), new Vector2(270, 64));
            RacingUIStyle.Place(garage.GetComponent<RectTransform>(), new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 100), new Vector2(270, 64));
            ranking.GetComponent<RectTransform>().sizeDelta = new Vector2(270, 64);
        }

        private static Text Label(Transform parent, string value, int size, float y, Color color, FontStyle style = FontStyle.Normal, float height = 46)
        {
            var label = RacingUIStyle.Label(parent, value, size, color);
            label.alignment = TextAnchor.MiddleCenter;
            label.fontStyle = style;
            RacingUIStyle.Place(label.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(0, y), new Vector2(-80, height));
            return label;
        }

        private static void Reward(Transform parent, string title, int value, float y, bool total = false)
        {
            var row = RacingUIStyle.Box(parent, title, total ? new Color(.04f, .23f, .27f, .7f) : new Color(.065f, .11f, .16f, .5f));
            RacingUIStyle.Place(row.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, y), new Vector2(880, total ? 74 : 48));
            var label = RacingUIStyle.Label(row.transform, title, total ? 22 : 20, total ? RacingUIStyle.Accent : RacingUIStyle.Muted);
            RacingUIStyle.Place(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(-170, 0), new Vector2(-400, 0));
            var amount = RacingUIStyle.Label(row.transform, total ? $"+{value:N0} CREDITS" : $"+{value:N0}", total ? 30 : 24);
            amount.alignment = TextAnchor.MiddleRight;
            RacingUIStyle.Place(amount.rectTransform, Vector2.zero, Vector2.one, new Vector2(170, 0), new Vector2(-400, 0));
        }
    }
}
