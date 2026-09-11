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
            Button button = MakeButton(parent, "RANKING", () => Open(selectedTrack()));
            RacingUIStyle.RankingIcon(button.transform, new Vector2(34, -1));
            Text caption = button.GetComponentInChildren<Text>();
            caption.rectTransform.offsetMin = new Vector2(44, 0);
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
            scaler.matchWidthOrHeight = .5f;
            gameObject.AddComponent<GraphicRaycaster>();

            RectTransform dialog = RacingUIStyle.Modal(transform, "GLOBAL RANKING",
                track.DisplayName.ToUpperInvariant() + "   /   BEST RACE TIME   /   ALL CARS",
                new Vector2(1040, 780), () => Destroy(gameObject));
            status = Label(dialog, "Loading global ranking...", 19);
            status.color = RacingUIStyle.Muted;
            Place(status.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(0, -132), new Vector2(-64, 36));

            Image header = Box(dialog, "Table Header", new Color(.065f, .13f, .18f));
            Place(header.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(0, -182), new Vector2(-64, 42));
            Column(header.transform, "RANK", .02f, .15f, 17, TextAnchor.MiddleLeft);
            Column(header.transform, "DRIVER", .17f, .73f, 17, TextAnchor.MiddleLeft);
            Column(header.transform, "RACE TIME", .75f, .97f, 17, TextAnchor.MiddleRight);

            Image viewport = Box(dialog, "Scores", new Color(.022f, .043f, .066f));
            Place(viewport.rectTransform, new Vector2(0, 0), Vector2.one, new Vector2(0, -26), new Vector2(-64, -360));
            viewport.gameObject.AddComponent<RectMask2D>();
            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35;
            content = new GameObject("Rows", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewport.transform, false);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1);
            scroll.viewport = viewport.rectTransform;
            scroll.content = content;

            Image personal = Box(dialog, "Your Record", new Color(.035f, .17f, .21f));
            Place(personal.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(0, 109), new Vector2(-64, 56));
            ownScore = Label(personal.transform, "YOU / Loading...", 21);
            ownScore.color = Accent;
            Place(ownScore.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-28, 0));
            refresh = MakeButton(dialog, "REFRESH", Refresh);
            Place(refresh.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-137, 42), new Vector2(210, 46));
            Text note = Label(dialog, "TOP 100  /  Scroll to explore", 17);
            note.color = RacingUIStyle.Muted;
            Place(note.rectTransform, Vector2.zero, Vector2.zero, new Vector2(280, 42), new Vector2(496, 40));
        }

        private static Text Column(Transform parent, string text, float left, float right, int size, TextAnchor alignment)
        {
            Text label = Label(parent, text, size);
            label.alignment = alignment;
            Place(label.rectTransform, new Vector2(left, 0), new Vector2(right, 1), Vector2.zero, Vector2.zero);
            return label;
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
                    rank.color = entry.Rank == 0 ? new Color(1f, .79f, .32f) : entry.Rank == 1 ? new Color(.78f, .86f, .92f) : entry.Rank == 2 ? new Color(.82f, .57f, .38f) : RacingUIStyle.Muted;
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
            return RacingUIStyle.Button(parent, title, action);
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
