using System;
using SuperRacing.Data;
using SuperRacing.Race;
using Unity.Services.Leaderboards.Models;
using UnityEngine;
using UnityEngine.UI;

namespace SuperRacing.UI
{
    /// <summary>Scene-owned overlay; closing it invalidates in-flight UI updates.</summary>
    public sealed class GlobalLeaderboardPanel : MonoBehaviour
    {
        private TrackDefinition track;
        private Text status;
        private Text ownScore;
        private RectTransform content;
        private Button refresh;
        private int request;
        private static readonly Color Accent = new(0f, 0.8f, 0.92f);

        public static void Open(TrackDefinition track)
        {
            if (track == null || FindFirstObjectByType<GlobalLeaderboardPanel>() != null) return;
            var go = new GameObject("Global Leaderboard", typeof(RectTransform));
            var panel = go.AddComponent<GlobalLeaderboardPanel>();
            panel.track = track;
            panel.Build();
            panel.Refresh();
        }

        public static Button AddButton(Transform parent, Func<TrackDefinition> selectedTrack, Vector2 anchor, Vector2 position)
        {
            Button button = MakeButton(parent, "GLOBAL RANKING", () => Open(selectedTrack()));
            Place(button.GetComponent<RectTransform>(), anchor, anchor, position, new Vector2(300, 60));
            return button;
        }

        private void Build()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            Image backdrop = Box(transform, "Background", new Color(0.015f, 0.03f, 0.055f, 1f));
            Place(backdrop.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Text title = Label(transform, track.DisplayName.ToUpperInvariant() + " / GLOBAL", 42);
            Place(title.rectTransform, new Vector2(0.08f, 0.86f), new Vector2(0.8f, 0.97f), Vector2.zero, Vector2.zero);
            Text subtitle = Label(transform, "BEST RACE TIME   /   ALL CARS   /   TOP 100", 22);
            subtitle.color = Accent;
            Place(subtitle.rectTransform, new Vector2(0.08f, 0.81f), new Vector2(0.85f, 0.87f), Vector2.zero, Vector2.zero);

            Button close = MakeButton(transform, "CLOSE", () => Destroy(gameObject));
            Place(close.GetComponent<RectTransform>(), new Vector2(0.89f, 0.92f), new Vector2(0.89f, 0.92f), Vector2.zero, new Vector2(180, 56));
            status = Label(transform, "Loading...", 22);
            Place(status.rectTransform, new Vector2(0.08f, 0.75f), new Vector2(0.92f, 0.81f), Vector2.zero, Vector2.zero);

            Image viewport = Box(transform, "Scores", new Color(0.04f, 0.07f, 0.1f));
            Place(viewport.rectTransform, new Vector2(0.08f, 0.23f), new Vector2(0.92f, 0.74f), Vector2.zero, Vector2.zero);
            viewport.gameObject.AddComponent<RectMask2D>();
            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35;
            content = new GameObject("Rows", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewport.transform, false);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1);
            scroll.viewport = viewport.rectTransform;
            scroll.content = content;

            ownScore = Label(transform, "YOU / Loading...", 26);
            ownScore.color = Accent;
            Place(ownScore.rectTransform, new Vector2(0.08f, 0.14f), new Vector2(0.92f, 0.22f), Vector2.zero, Vector2.zero);
            refresh = MakeButton(transform, "REFRESH", Refresh);
            Place(refresh.GetComponent<RectTransform>(), new Vector2(0.82f, 0.08f), new Vector2(0.82f, 0.08f), Vector2.zero, new Vector2(240, 58));
            Text note = Label(transform, "Guest profile saved on this device", 20);
            Place(note.rectTransform, new Vector2(0.08f, 0.04f), new Vector2(0.65f, 0.12f), Vector2.zero, Vector2.zero);
        }

        private async void Refresh()
        {
            int current = ++request;
            refresh.interactable = false;
            status.text = "Loading global ranking...";
            ownScore.text = "YOU / Loading...";
            foreach (Transform row in content) { row.gameObject.SetActive(false); Destroy(row.gameObject); }
            content.sizeDelta = Vector2.zero;
            try
            {
                var service = GlobalLeaderboardService.Instance;
                if (service == null) throw new InvalidOperationException("Online service is not running.");
                await service.FlushAsync();
                LeaderboardScoresPage page = await service.GetTopAsync(track.TrackId);
                if (this == null || current != request) return;
                content.sizeDelta = new Vector2(0, page.Results.Count * 58);
                content.anchoredPosition = Vector2.zero;
                for (int i = 0; i < page.Results.Count; i++)
                {
                    LeaderboardEntry entry = page.Results[i];
                    bool own = entry.PlayerId == service.PlayerId;
                    Image row = Box(content, "Rank " + (entry.Rank + 1), own ? new Color(0f, 0.22f, 0.28f) :
                        (i % 2 == 0 ? new Color(0.06f, 0.1f, 0.14f) : new Color(0.04f, 0.07f, 0.1f)));
                    Place(row.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(0, -i * 58 - 28), new Vector2(0, 56));
                    Text rank = Label(row.transform, "#" + (entry.Rank + 1), 26);
                    Place(rank.rectTransform, new Vector2(0.02f, 0), new Vector2(0.13f, 1), Vector2.zero, Vector2.zero);
                    Text name = Label(row.transform, GlobalLeaderboardService.DisplayName(entry) + (own ? " (YOU)" : ""), 25);
                    Place(name.rectTransform, new Vector2(0.15f, 0), new Vector2(0.74f, 1), Vector2.zero, Vector2.zero);
                    Text time = Label(row.transform, FormatScore(entry.Score), 27);
                    time.alignment = TextAnchor.MiddleRight;
                    Place(time.rectTransform, new Vector2(0.76f, 0), new Vector2(0.97f, 1), Vector2.zero, Vector2.zero);
                }
                status.text = page.Results.Count == 0 ? "No records yet. Finish a race to set the first time!" : "Fastest times worldwide";
                if (service.HasPendingScores) status.text += " / Your record is waiting to sync.";
                try
                {
                    LeaderboardEntry own = await service.GetOwnAsync(track.TrackId);
                    if (this == null || current != request) return;
                    ownScore.text = own == null ? "YOU / No global record for this map yet" :
                        $"YOU / #{own.Rank + 1}   {GlobalLeaderboardService.DisplayName(own)}   {FormatScore(own.Score)}";
                }
                catch (Exception) { if (this != null && current == request) ownScore.text = "YOU / Rank unavailable. Please refresh."; }
            }
            catch (Exception ex)
            {
                if (this == null || current != request) return;
                status.text = GlobalLeaderboardService.FriendlyError(ex);
                ownScore.text = "YOU / Global rank unavailable";
            }
            finally { if (this != null && current == request) refresh.interactable = true; }
        }

        private void OnDestroy() => request++;

        public static string FormatScore(double milliseconds)
        {
            if (double.IsNaN(milliseconds) || double.IsInfinity(milliseconds) || milliseconds < 0 || milliseconds > 86400000)
                return "--:--.---";
            long ms = (long)Math.Round(milliseconds);
            return $"{ms / 60000:00}:{ms / 1000 % 60:00}.{ms % 1000:000}";
        }

        private static Image Box(Transform parent, string name, Color color)
        {
            Image image = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            return image;
        }

        private static Text Label(Transform parent, string value, int size)
        {
            Text text = new GameObject("Label", typeof(RectTransform)).AddComponent<Text>();
            text.transform.SetParent(parent, false);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.text = value;
            text.supportRichText = false;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static Button MakeButton(Transform parent, string title, UnityEngine.Events.UnityAction action)
        {
            Image image = Box(parent, title, Accent);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            Text text = Label(image.transform, title, 23);
            text.color = new Color(0.01f, 0.04f, 0.07f);
            text.alignment = TextAnchor.MiddleCenter;
            Place(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private static void Place(RectTransform rect, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }
    }
}
