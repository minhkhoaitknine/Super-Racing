using UnityEngine;
using UnityEngine.UI;

namespace SuperRacing.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RaceHUD))]
    public sealed class RaceHUDLayout : MonoBehaviour
    {
        private static readonly Color PanelColor = new(0.035f, 0.035f, 0.03f, 0.94f);
        private static readonly Color Ivory = new(0.92f, 0.86f, 0.7f, 1f);
        private static readonly Color Amber = new(0.95f, 0.55f, 0.12f, 1f);
        private static readonly Color NeedleRed = new(0.82f, 0.08f, 0.035f, 1f);
        private static Sprite circleSprite;
        private static Sprite timingSprite;
        private static Sprite speedometerSprite;
        private static Sprite minimapSprite;

        private void Awake()
        {
            LoadHudAssets();

            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = 50;
            }

            Text speed = FindText("SpeedLabel");
            Text lap = FindText("LapLabel");
            Text time = FindText("TimeLabel");

            RectTransform timePanel = CreatePanel("Timing Panel", transform, new Vector2(350f, 176f), timingSprite);
            Anchor(timePanel, new Vector2(0f, 1f), new Vector2(28f, -28f));
            AddCaption(timePanel, "RACE TIME", new Vector2(64f, -20f), 15, Amber);
            ConfigureText(time, timePanel, new Vector2(64f, -42f), new Vector2(255f, 40f), 30, TextAnchor.MiddleLeft);
            AddCaption(timePanel, "CURRENT LAP", new Vector2(64f, -98f), 14, Ivory);
            ConfigureText(lap, timePanel, new Vector2(64f, -119f), new Vector2(255f, 32f), 20, TextAnchor.MiddleLeft);

            RectTransform speedPanel = CreatePanel("Speedometer", transform, new Vector2(270f, 270f), speedometerSprite);
            Anchor(speedPanel, new Vector2(1f, 0f), new Vector2(-30f, 30f));
            if (speed != null)
            {
                speed.gameObject.SetActive(false);
            }
            Text speedValue = CreateLabel("Speed Value", speedPanel, "0\n<size=18>KM/H</size>", 42, Ivory, TextAnchor.MiddleCenter);
            RectTransform speedRect = speedValue.rectTransform;
            speedRect.anchorMin = speedRect.anchorMax = speedRect.pivot = new Vector2(0.5f, 0.5f);
            speedRect.anchoredPosition = new Vector2(0f, 34f);
            speedRect.sizeDelta = new Vector2(150f, 86f);
            GetComponent<RaceHUD>().SetSpeedLabel(speedValue);

            Image needle = CreateImage("Speed Needle", speedPanel, new Vector2(8f, 86f), NeedleRed);
            RectTransform needleRect = needle.rectTransform;
            needleRect.anchorMin = needleRect.anchorMax = new Vector2(0.5f, 0.5f);
            needleRect.pivot = new Vector2(0.5f, 0.08f);
            needleRect.anchoredPosition = new Vector2(0f, -5f);
            needleRect.localEulerAngles = new Vector3(0f, 0f, 135f);
            GetComponent<RaceHUD>().SetSpeedNeedle(needleRect);
            Image hub = CreateImage("Needle Hub", speedPanel, new Vector2(20f, 20f), new Color(0.72f, 0.72f, 0.67f, 1f));
            Center(hub.rectTransform);
            hub.rectTransform.anchoredPosition = new Vector2(0f, -5f);
            hub.sprite = GetCircleSprite();

            RectTransform mapPanel = CreatePanel("Minimap Panel", transform, new Vector2(320f, 220f), minimapSprite);
            Anchor(mapPanel, new Vector2(1f, 1f), new Vector2(-28f, -28f));
            RawImage map = CreateRawImage("Minimap", mapPanel, new Vector2(272f, 158f));
            Center(map.rectTransform);
            map.rectTransform.anchoredPosition = new Vector2(0f, -3f);
            map.gameObject.AddComponent<RaceMinimap>();
        }

        private Text FindText(string objectName)
        {
            Transform child = transform.Find(objectName);
            return child != null ? child.GetComponent<Text>() : null;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Vector2 size, Sprite sprite = null)
        {
            Image image = CreateImage(name, parent, size, PanelColor);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
            }
            image.rectTransform.SetAsFirstSibling();
            return image.rectTransform;
        }

        private static void LoadHudAssets()
        {
            timingSprite ??= LoadSprite("HUD/race-info-classic");
            speedometerSprite ??= LoadSprite("HUD/speedometer-classic");
            minimapSprite ??= LoadSprite("HUD/minimap-frame-classic");
        }

        private static Sprite LoadSprite(string resourcePath)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            return texture == null
                ? null
                : Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Image CreateImage(string name, Transform parent, Vector2 size, Color color)
        {
            GameObject gameObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.sizeDelta = size;
            Image image = gameObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static RawImage CreateRawImage(string name, Transform parent, Vector2 size)
        {
            GameObject gameObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.sizeDelta = size;
            RawImage image = gameObject.GetComponent<RawImage>();
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private static void AddAccent(Transform parent, Vector2 size, Color color, Vector2 position)
        {
            Image accent = CreateImage("Accent", parent, size, color);
            accent.rectTransform.anchorMin = accent.rectTransform.anchorMax = new Vector2(0f, 1f);
            accent.rectTransform.pivot = new Vector2(0f, 1f);
            accent.rectTransform.anchoredPosition = position;
        }

        private static void AddCaption(Transform parent, string value, Vector2 position, int size, Color color,
            TextAnchor alignment = TextAnchor.MiddleLeft, Vector2? dimensions = null)
        {
            Text text = CreateLabel(value, parent, value, size, color, alignment);
            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions ?? new Vector2(240f, 28f);
        }

        private static Text CreateLabel(string name, Transform parent, string value, int size, Color color, TextAnchor alignment)
        {
            GameObject gameObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            Text text = gameObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        private static Sprite GetCircleSprite()
        {
            if (circleSprite != null)
            {
                return circleSprite;
            }

            const int size = 128;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                name = "HUD Circle",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            Color32[] pixels = new Color32[size * size];
            Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.49f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(radius - distance) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            circleSprite.name = "HUD Circle";
            return circleSprite;
        }

        private static void ConfigureText(Text text, Transform parent, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
        {
            if (text == null)
            {
                return;
            }

            text.transform.SetParent(parent, false);
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.alignment = alignment;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(anchor.x, anchor.y);
            rect.anchoredPosition = position;
        }

        private static void Center(RectTransform rect)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
        }
    }
}
