using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AssassinMystery
{
    /// <summary>Palette and small builders for the code-built medieval UI.</summary>
    public static class UIFactory
    {
        public static readonly Color Parchment = new Color(0.91f, 0.84f, 0.68f, 0.98f);
        public static readonly Color ParchmentDark = new Color(0.80f, 0.71f, 0.53f, 1f);
        public static readonly Color Ink = new Color(0.20f, 0.13f, 0.07f);
        public static readonly Color InkFaded = new Color(0.42f, 0.32f, 0.21f);
        public static readonly Color Gold = new Color(0.88f, 0.71f, 0.33f);
        public static readonly Color Dark = new Color(0.08f, 0.065f, 0.05f, 0.92f);
        public static readonly Color Blood = new Color(0.55f, 0.11f, 0.08f);
        public static readonly Color Leather = new Color(0.33f, 0.21f, 0.12f);
        public static readonly Color Moss = new Color(0.27f, 0.38f, 0.2f);

        static TMP_FontAsset font;
        public static TMP_FontAsset Font => font ? font : font = TMP_Settings.defaultFontAsset;

        public static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
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

        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Panel(string name, Transform parent, Color color, bool goldBorder = false)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (goldBorder)
            {
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = Gold;
                o.effectDistance = new Vector2(3f, -3f);
            }
            return img;
        }

        public static TextMeshProUGUI Text(Transform parent, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.TopLeft, FontStyles style = FontStyles.Normal)
        {
            var rt = Rect("Text", parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
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

        public static Button Button(Transform parent, string label, Action onClick, Color bg, Color fg, float fontSize = 24f, float height = 48f)
        {
            var img = Panel("Button: " + label, parent, bg);
            var btn = img.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.1f, 1f);
            colors.pressedColor = new Color(0.8f, 0.75f, 0.7f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
            colors.colorMultiplier = 1.2f;
            btn.colors = colors;
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(() => onClick());

            var text = Text(img.transform, label, fontSize, fg, TextAlignmentOptions.Center);
            text.rectTransform.Stretch(10, 4, 10, 4);
            text.name = "Label";

            var le = img.gameObject.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            return btn;
        }

        public static void SetLabel(this Button b, string label) => b.GetComponentInChildren<TextMeshProUGUI>().text = label;

        public static LayoutElement Layout(Component c, float preferredHeight = -1, float preferredWidth = -1, float flexibleWidth = -1)
        {
            if (!c.TryGetComponent(out LayoutElement le)) le = c.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = preferredHeight;
            le.preferredWidth = preferredWidth;
            le.flexibleWidth = flexibleWidth;
            return le;
        }

        public static VerticalLayoutGroup Vertical(Component c, float spacing, int padding = 0)
        {
            var g = c.gameObject.AddComponent<VerticalLayoutGroup>();
            g.spacing = spacing;
            g.padding = new RectOffset(padding, padding, padding, padding);
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.childForceExpandWidth = true;
            g.childForceExpandHeight = false;
            return g;
        }

        public static HorizontalLayoutGroup Horizontal(Component c, float spacing, int padding = 0)
        {
            var g = c.gameObject.AddComponent<HorizontalLayoutGroup>();
            g.spacing = spacing;
            g.padding = new RectOffset(padding, padding, padding, padding);
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.childForceExpandWidth = false;
            g.childForceExpandHeight = false;
            g.childAlignment = TextAnchor.MiddleLeft;
            return g;
        }

        /// <summary>A vertical scrolling list; returns the content to add rows to.</summary>
        public static RectTransform ScrollList(Transform parent, float spacing, out ScrollRect scroll)
        {
            var view = Rect("Scroll", parent);
            view.gameObject.AddComponent<RectMask2D>();
            var hit = view.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0); // catches wheel events over empty space

            var content = Rect("Content", view);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var vlg = Vertical(content, spacing, 6);
            vlg.padding = new RectOffset(6, 14, 6, 6);
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll = view.gameObject.AddComponent<ScrollRect>();
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

        public static Image Divider(Transform parent, Color color, float height = 2f)
        {
            var d = Panel("Divider", parent, color);
            Layout(d, height);
            return d;
        }
    }
}
