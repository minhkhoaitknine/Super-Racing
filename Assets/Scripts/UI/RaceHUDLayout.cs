using UnityEngine;
using UnityEngine.UI;

namespace SuperRacing.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RaceHUD))]
    public sealed class RaceHUDLayout : MonoBehaviour
    {
        private static readonly Color PanelColor = new(0.015f, 0.055f, 0.085f, 0.88f);
        private static readonly Color Cyan = new(0f, 0.9f, 1f, 1f);
        private static readonly Color Orange = new(1f, 0.48f, 0.08f, 1f);
        private static Sprite circleSprite;

        private void Awake()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = 50;
            }

            Text speed = FindText("SpeedLabel");
            Text lap = FindText("LapLabel");
            Text time = FindText("TimeLabel");

            RectTransform timePanel = CreatePanel("Timing Panel", transform, new Vector2(330f, 132f));
            Anchor(timePanel, new Vector2(0f, 1f), new Vector2(28f, -28f));
            AddAccent(timePanel, new Vector2(7f, 112f), Cyan, new Vector2(11f, -10f));
            AddCaption(timePanel, "RACE TIME", new Vector2(28f, -16f), 18, Cyan);
            ConfigureText(time, timePanel, new Vector2(28f, -42f), new Vector2(275f, 48f), 34, TextAnchor.MiddleLeft);
            ConfigureText(lap, timePanel, new Vector2(28f, -88f), new Vector2(275f, 30f), 20, TextAnchor.MiddleLeft);

            RectTransform speedPanel = CreatePanel("Speedometer", transform, new Vector2(255f, 255f));
            Anchor(speedPanel, new Vector2(1f, 0f), new Vector2(-30f, 30f));
            Image outerRing = CreateImage("Outer Ring", speedPanel, new Vector2(218f, 218f), new Color(0.05f, 0.2f, 0.25f, 0.9f));
            Center(outerRing.rectTransform);
            outerRing.sprite = GetCircleSprite();
            outerRing.type = Image.Type.Filled;
            outerRing.fillMethod = Image.FillMethod.Radial360;
            outerRing.fillAmount = 0.82f;
            outerRing.fillOrigin = 2;

            Image speedFill = CreateImage("Speed Fill", speedPanel, new Vector2(218f, 218f), Orange);
            Center(speedFill.rectTransform);
            speedFill.sprite = GetCircleSprite();
            speedFill.type = Image.Type.Filled;
            speedFill.fillMethod = Image.FillMethod.Radial360;
            speedFill.fillOrigin = 2;
            speedFill.fillClockwise = true;
            speedFill.fillAmount = 0f;
            GetComponent<RaceHUD>().SetSpeedFill(speedFill);

            Image inner = CreateImage("Dial Face", speedPanel, new Vector2(174f, 174f), new Color(0.01f, 0.025f, 0.04f, 0.96f));
            Center(inner.rectTransform);
            inner.sprite = GetCircleSprite();
            if (speed != null)
            {
                speed.gameObject.SetActive(false);
            }
            Text speedValue = CreateLabel("Speed Value", speedPanel, "0\n<size=22>KM/H</size>", 48, Color.white, TextAnchor.MiddleCenter);
            RectTransform speedRect = speedValue.rectTransform;
            speedRect.anchorMin = speedRect.anchorMax = speedRect.pivot = new Vector2(0.5f, 0.5f);
            speedRect.anchoredPosition = new Vector2(0f, -4f);
            speedRect.sizeDelta = new Vector2(180f, 112f);
            GetComponent<RaceHUD>().SetSpeedLabel(speedValue);
            AddCaption(speedPanel, "SPEED", new Vector2(0f, -48f), 16, Cyan, TextAnchor.MiddleCenter, new Vector2(150f, 24f));

            RectTransform mapPanel = CreatePanel("Minimap Panel", transform, new Vector2(236f, 236f));
            Anchor(mapPanel, new Vector2(1f, 1f), new Vector2(-28f, -28f));
            AddAccent(mapPanel, new Vector2(216f, 5f), Cyan, new Vector2(10f, -10f));
            AddCaption(mapPanel, "TRACK MAP", new Vector2(16f, -16f), 16, Cyan);
            RawImage map = CreateRawImage("Minimap", mapPanel, new Vector2(204f, 178f));
            map.rectTransform.anchorMin = map.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            map.rectTransform.anchoredPosition = new Vector2(0f, 15f);
            map.gameObject.AddComponent<RaceMinimap>();
        }

        private Text FindText(string objectName)
        {
            Transform child = transform.Find(objectName);
            return child != null ? child.GetComponent<Text>() : null;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Vector2 size)
        {
            Image image = CreateImage(name, parent, size, PanelColor);
            image.rectTransform.SetAsFirstSibling();
            Outline outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0.75f, 0.9f, 0.65f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return image.rectTransform;
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
