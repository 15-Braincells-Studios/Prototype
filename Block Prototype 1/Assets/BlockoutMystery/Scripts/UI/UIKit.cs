using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlockoutMystery
{
    /// <summary>Palette and small builders for the code-built blockout UI.</summary>
    public static class UIKit
    {
        // Opaque: UI blends in linear colour space, so even 97% alpha lets a fifth of a bright scene show through.
        public static readonly Color Panel = new Color(0.08f, 0.09f, 0.11f, 1f);
        public static readonly Color PanelLight = new Color(0.2f, 0.22f, 0.26f, 1f);
        public static readonly Color Text = new Color(0.94f, 0.95f, 0.96f);
        public static readonly Color TextDim = new Color(0.66f, 0.69f, 0.74f);
        public static readonly Color Accent = new Color(1f, 0.79f, 0.25f);
        public static readonly Color Good = new Color(0.36f, 0.8f, 0.45f);
        public static readonly Color Bad = new Color(0.9f, 0.32f, 0.3f);
        public static readonly Color Paper = new Color(0.95f, 0.92f, 0.83f, 0.98f);
        public static readonly Color Ink = new Color(0.14f, 0.11f, 0.09f);
        public static readonly Color InkDim = new Color(0.42f, 0.37f, 0.31f);

        static TMP_FontAsset font;
        static TMP_FontAsset Font => font ? font : font = TMP_Settings.defaultFontAsset;

        static Sprite rounded;

        /// <summary>A white rounded rectangle for sliced Images (speech bubbles), generated at runtime.</summary>
        public static Sprite RoundedSprite
        {
            get
            {
                if (rounded) return rounded;
                const int size = 64, radius = 24;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    var inner = new Vector2(Mathf.Clamp(p.x, radius, size - radius), Mathf.Clamp(p.y, radius, size - radius));
                    byte a = (byte)(Mathf.Clamp01(radius - Vector2.Distance(p, inner) + 0.5f) * 255);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
                tex.SetPixels32(pixels);
                tex.Apply();
                rounded = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                    SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
                return rounded;
            }
        }

        static Sprite disc, ring;

        /// <summary>A white filled circle (the cursor dot), generated at runtime.</summary>
        public static Sprite DiscSprite => disc ? disc : disc = CircleSprite(64, 0f);

        /// <summary>A thin white circle outline (the hover ring around the cursor dot), generated at runtime.</summary>
        public static Sprite RingSprite => ring ? ring : ring = CircleSprite(64, 0.82f);

        /// <summary>An anti-aliased circle; <paramref name="hole"/> is the inner radius as a fraction (0 = filled).</summary>
        static Sprite CircleSprite(int size, float hole)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            float outer = size * 0.5f - 1f, inner = outer * hole;
            var centre = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centre);
                float a = Mathf.Clamp01(outer - d + 0.5f);
                if (hole > 0f) a *= Mathf.Clamp01(d - inner + 0.5f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        public static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 }; // UI
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static RectTransform Anchors(this RectTransform rt, float minX, float minY, float maxX, float maxY)
        {
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size = default)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            if (size != default) rt.sizeDelta = size;
            return rt;
        }

        public static Image Image(string name, Transform parent, Color color, bool raycast = false)
        {
            var img = Rect(name, parent).gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        public static TextMeshProUGUI Label(Transform parent, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.TopLeft, FontStyles style = FontStyles.Normal)
        {
            var t = Rect("Label", parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (Font) t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.raycastTarget = false;
            t.richText = true;
            return t;
        }

        public static Button Button(Transform parent, string label, Action onClick, Color bg, Color fg, float fontSize = 26f,
            float height = 54f, float width = -1f)
        {
            var img = Image("Button " + label, parent, bg, true);
            var btn = img.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.18f, 1.18f, 1.18f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.5f);
            colors.colorMultiplier = 1.2f;
            btn.colors = colors;
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(() => onClick());

            var text = Label(img.transform, label, fontSize, fg, TextAlignmentOptions.Center, FontStyles.Bold);
            text.rectTransform.Stretch(8, 2, 8, 2);
            Size(img, height, width);
            return btn;
        }

        public static void SetLabel(this Button b, string label) => b.GetComponentInChildren<TextMeshProUGUI>().text = label;

        /// <summary>A background that hugs a single line of text.</summary>
        public static RectTransform Pill(string name, Transform parent, Color bg, out TextMeshProUGUI text, float size, Color fg,
            int padX = 18, int padY = 8)
        {
            var img = Image(name, parent, bg);
            var row = Row(img, 0, 0);
            row.padding = new RectOffset(padX, padX, padY, padY);
            var fit = img.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            text = Label(img.transform, "", size, fg, TextAlignmentOptions.Center);
            return img.rectTransform;
        }

        public static LayoutElement Size(Component c, float height = -1, float width = -1, float flexibleWidth = -1, float flexibleHeight = -1)
        {
            if (!c.TryGetComponent(out LayoutElement le)) le = c.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            le.preferredWidth = width;
            le.minWidth = width;
            le.flexibleWidth = flexibleWidth;
            le.flexibleHeight = flexibleHeight;
            return le;
        }

        public static VerticalLayoutGroup Column(Component c, float spacing, int padding)
        {
            var g = c.gameObject.AddComponent<VerticalLayoutGroup>();
            g.spacing = spacing;
            g.padding = new RectOffset(padding, padding, padding, padding);
            g.childControlWidth = g.childControlHeight = true;
            g.childForceExpandWidth = true;
            g.childForceExpandHeight = false;
            return g;
        }

        public static HorizontalLayoutGroup Row(Component c, float spacing, int padding)
        {
            var g = c.gameObject.AddComponent<HorizontalLayoutGroup>();
            g.spacing = spacing;
            g.padding = new RectOffset(padding, padding, padding, padding);
            g.childControlWidth = g.childControlHeight = true;
            g.childForceExpandWidth = g.childForceExpandHeight = false;
            g.childAlignment = TextAnchor.MiddleCenter;
            return g;
        }

        /// <summary>A vertical scrolling list; returns the content to add rows to.</summary>
        public static RectTransform ScrollList(Transform parent, float spacing)
        {
            var view = Rect("Scroll", parent);
            view.gameObject.AddComponent<RectMask2D>();
            Image("Hit Area", view, new Color(0, 0, 0, 0), true).rectTransform.Stretch(); // wheel works over empty space

            var content = Rect("Content", view);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            Column(content, spacing, 4);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = view;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            return content;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var child = t.GetChild(i);
                child.SetParent(null, false); // out of the layout this frame
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }
    }
}
