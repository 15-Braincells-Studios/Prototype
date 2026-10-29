using TMPro;
using UnityEngine;

namespace BlockoutMystery
{
    /// <summary>
    /// Always-on HUD: timer, controls, the cursor dot (with a ring while it is over something you can use), the E
    /// prompt, short messages, and the inspect title and hint.
    /// </summary>
    public class HudView : MonoBehaviour
    {
        TextMeshProUGUI timer, controls, prompt, toast, inspectTitle, inspectHint;
        RectTransform root, controlsBox, promptBox, inspectTitleBox, inspectHintBox, cursor;
        GameObject cursorRing;
        CanvasGroup toastGroup;
        float toastUntil;

        public static HudView Create(RectTransform parent)
        {
            var rt = UIKit.Rect("HUD", parent).Stretch();
            var view = rt.gameObject.AddComponent<HudView>();
            view.Build(rt);
            return view;
        }

        void Build(RectTransform rt)
        {
            root = rt;
            UIKit.Pill("Timer", rt, UIKit.Panel, out timer, 30, UIKit.Text).Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -20));

            controlsBox = UIKit.Pill("Controls", rt, UIKit.Panel, out controls, 21, UIKit.TextDim)
                .Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -20));
            string k = UIKit.Hex(UIKit.Accent);
            controls.text = PrototypeVariant.Isometric
                ? $"<color=#{k}>WASD</color> walk     <color=#{k}>Wheel</color> zoom     <color=#{k}>E</color> interact     <color=#{k}>Q</color> journal"
                : $"<color=#{k}>WASD</color> walk     <color=#{k}>Mouse</color> look     <color=#{k}>E</color> interact     <color=#{k}>Q</color> journal     <color=#{k}>V</color> first / third person";

            UIKit.Pill("Variant", rt, new Color(0f, 0f, 0f, 0.35f), out var variant, 18, UIKit.TextDim, 12, 4)
                .Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(16, 12));
            variant.text = "Variant: " + PrototypeVariant.Title;

            promptBox = UIKit.Pill("Prompt", rt, UIKit.Panel, out prompt, 28, UIKit.Text)
                .Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 170));
            promptBox.gameObject.SetActive(false);

            var toastBox = UIKit.Pill("Toast", rt, UIKit.Accent, out toast, 28, UIKit.Ink, 24, 12)
                .Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -110));
            toast.fontStyle = FontStyles.Bold;
            toastGroup = toastBox.gameObject.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            toastGroup.blocksRaycasts = false;

            inspectTitleBox = UIKit.Pill("Inspect Title", rt, UIKit.Panel, out inspectTitle, 34, UIKit.Accent)
                .Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30));
            inspectTitle.fontStyle = FontStyles.Bold;
            inspectHintBox = UIKit.Pill("Inspect Hint", rt, UIKit.Panel, out inspectHint, 24, UIKit.Text)
                .Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 170));
            HideInspect();

            // The cursor: a small dot with a dark edge so it shows on bright and dark walls, and a ring for hovering.
            cursor = UIKit.Rect("Cursor", rt).Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 40));
            Dot(cursor, new Color(0f, 0f, 0f, 0.55f), 10f);
            Dot(cursor, new Color(1f, 1f, 1f, 0.95f), 6f);
            var ring = UIKit.Image("Hover Ring", cursor, new Color(1f, 1f, 1f, 0.9f));
            ring.sprite = UIKit.RingSprite;
            ring.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30, 30));
            cursorRing = ring.gameObject;
            cursorRing.SetActive(false);
            cursor.gameObject.SetActive(false);
        }

        static void Dot(RectTransform parent, Color color, float size)
        {
            var dot = UIKit.Image("Dot", parent, color);
            dot.sprite = UIKit.DiscSprite;
            dot.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
        }

        /// <summary>
        /// Shows the cursor dot at a screen position (the centre of the screen when the mouse looks around), with the
        /// ring when it is over something you can interact with. Hidden when <paramref name="visible"/> is false.
        /// </summary>
        public void SetCursor(bool visible, bool hovering, Vector2 screenPosition)
        {
            if (cursor.gameObject.activeSelf != visible) cursor.gameObject.SetActive(visible);
            if (!visible) return;
            if (cursorRing.activeSelf != hovering) cursorRing.SetActive(hovering);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPosition, null, out var local);
            cursor.anchoredPosition = local + root.rect.size * (root.pivot - new Vector2(0.5f, 0.5f));
        }

        void Update()
        {
            var flow = GameFlow.Instance;
            var clock = GameTimer.Instance;
            string time = "Time  " + GameTimer.Format(clock ? clock.Elapsed : 0f);
            if (timer.text != time) timer.text = time;

            bool explore = flow && flow.CanMove;
            if (controlsBox.gameObject.activeSelf != explore) controlsBox.gameObject.SetActive(explore);

            toastGroup.alpha = Mathf.Clamp01((toastUntil - Time.unscaledTime) / 0.4f);
        }

        public void SetPrompt(string text)
        {
            bool on = !string.IsNullOrEmpty(text);
            if (promptBox.gameObject.activeSelf != on) promptBox.gameObject.SetActive(on);
            if (!on) return;
            string full = $"<color=#{UIKit.Hex(UIKit.Accent)}><b>E</b></color>   {text}";
            if (prompt.text != full) prompt.text = full;
        }

        public void Toast(string message, float seconds = 3f)
        {
            toast.text = message;
            toastUntil = Time.unscaledTime + seconds;
        }

        public void ClearToast() => toastUntil = 0f;

        public void ShowInspect(string title, string hint)
        {
            inspectTitle.text = title;
            inspectHint.text = hint;
            inspectTitleBox.gameObject.SetActive(true);
            inspectHintBox.gameObject.SetActive(true);
        }

        public void HideInspect()
        {
            inspectTitleBox.gameObject.SetActive(false);
            inspectHintBox.gameObject.SetActive(false);
        }
    }
}
