using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace SuperRacing.UI
{
    public static class RacingUIStyle
    {
        public static readonly Color Accent = new Color(.12f, .84f, .94f);
        public static readonly Color Muted = new Color(.57f, .69f, .78f);
        public static readonly Color Panel = new Color(.035f, .065f, .10f);

        public static Image Box(Transform parent, string name, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            return image;
        }

        public static void Place(RectTransform rect, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
        {
            rect.anchorMin = min; rect.anchorMax = max;
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size; rect.anchoredPosition = position;
        }

        public static Text Label(Transform parent, string value, int size, Color? color = null)
        {
            var text = new GameObject("Label", typeof(RectTransform)).AddComponent<Text>();
            text.transform.SetParent(parent, false);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size; text.text = value; text.color = color ?? Color.white;
            text.supportRichText = false; text.raycastTarget = false;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            return text;
        }

        public static Button Button(Transform parent, string title, UnityAction action, bool primary = false)
        {
            var image = Box(parent, title, primary ? Accent : new Color(.065f, .14f, .19f));
            var outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.18f, .72f, .83f, .6f);
            outline.effectDistance = new Vector2(1, -1);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(.75f, 1f, 1f);
            colors.pressedColor = new Color(.5f, .8f, .85f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = .1f; button.colors = colors;
            button.onClick.AddListener(action);
            var text = Label(image.transform, title, 22, primary ? Panel : Color.white);
            text.fontStyle = FontStyle.Bold; text.alignment = TextAnchor.MiddleCenter;
            Place(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        public static void StyleButton(Button button, bool primary = false)
        {
            var image = button.GetComponent<Image>();
            if (image == null) return;
            image.sprite = null;
            image.color = primary ? Accent : new Color(.045f, .11f, .16f, .96f);
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            var outline = button.GetComponent<Outline>() ?? button.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.18f, .72f, .83f, .7f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(.75f, 1f, 1f);
            colors.pressedColor = new Color(.5f, .8f, .85f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = .1f; button.colors = colors;
            foreach (var label in button.GetComponentsInChildren<Text>(true))
            {
                label.color = primary ? Panel : Color.white;
                label.fontStyle = FontStyle.Bold;
                label.raycastTarget = false;
            }
        }

        public static RectTransform Modal(Transform root, string title, string subtitle, Vector2 size, UnityAction close)
        {
            var dim = Box(root, "Dim", new Color(.005f, .012f, .024f, .72f));
            Place(dim.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var dismiss = dim.gameObject.AddComponent<Button>();
            dismiss.targetGraphic = dim; dismiss.transition = Selectable.Transition.None;
            dismiss.onClick.AddListener(close);
            var panel = Box(root, "Dialog", Panel);
            Place(panel.rectTransform, Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, size);
            var border = panel.gameObject.AddComponent<Outline>();
            border.effectColor = new Color(.18f, .53f, .64f); border.effectDistance = new Vector2(2, -2);
            var line = Box(panel.transform, "Accent", Accent);
            line.raycastTarget = false;
            Place(line.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(0, -2), new Vector2(0, 4));
            var heading = Label(panel.transform, title, 32); heading.fontStyle = FontStyle.Bold;
            Place(heading.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(-20, -48), new Vector2(-104, 44));
            var sub = Label(panel.transform, subtitle, 19, Muted);
            Place(sub.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -90), new Vector2(-64, 32));
            var exit = Button(panel.transform, "X", close);
            Place(exit.GetComponent<RectTransform>(), Vector2.one, Vector2.one, new Vector2(-40, -44), new Vector2(44, 44));
            return panel.rectTransform;
        }

        // Vector podium: readable without depending on a font's emoji support.
        public static void RankingIcon(Transform parent, Vector2 position)
        {
            var root = new GameObject("Ranking Icon", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(parent, false);
            Place(root, new Vector2(0, .5f), new Vector2(0, .5f), position, new Vector2(34, 30));
            for (int i = 0; i < 3; i++)
            {
                float height = i == 1 ? 27 : i == 0 ? 18 : 12;
                var bar = Box(root, "Podium", Accent); bar.raycastTarget = false;
                Place(bar.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(5 + i * 12, height / 2), new Vector2(9, height));
            }
        }
    }
}
