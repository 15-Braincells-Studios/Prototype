using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlockoutMystery
{
    /// <summary>
    /// A speech bubble above the speaker's head that follows them on screen. Bubbles only fit a few words, so long
    /// lines are split into short parts; E shows the next one.
    /// </summary>
    public class SpeechBubbleView : MonoBehaviour, IDialoguePresenter
    {
        [Tooltip("Longest text in one bubble before the line is split.")]
        [SerializeField] int maxCharacters = 56;
        [SerializeField] float maxTextWidth = 430f;
        [Tooltip("Keep the bubble this far from the screen edges (canvas units).")]
        [SerializeField] float margin = 20f;

        RectTransform area, bubble;
        TextMeshProUGUI body, hint;
        LayoutElement bodySize;
        Typewriter typer;
        CanvasGroup group;
        Npc speaker;
        bool more;

        public bool IsTyping => typer.IsTyping;

        public static SpeechBubbleView Create(RectTransform parent)
        {
            var area = UIKit.Rect("Speech Bubbles", parent).Stretch();
            var view = area.gameObject.AddComponent<SpeechBubbleView>();
            view.area = area;
            view.group = area.gameObject.AddComponent<CanvasGroup>();
            view.group.blocksRaycasts = false;
            view.group.interactable = false;

            var img = UIKit.Image("Bubble", area, Color.white);
            img.sprite = UIKit.RoundedSprite;
            img.type = Image.Type.Sliced;
            view.bubble = img.rectTransform;
            view.bubble.anchorMin = view.bubble.anchorMax = new Vector2(0.5f, 0.5f);
            view.bubble.pivot = new Vector2(0.5f, 0f);
            var column = UIKit.Column(img, 2, 0);
            column.padding = new RectOffset(26, 26, 16, 12);
            column.childAlignment = TextAnchor.UpperCenter;
            var fit = img.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            view.body = UIKit.Label(img.transform, "", 28, UIKit.Ink, TextAlignmentOptions.Center);
            view.bodySize = UIKit.Size(view.body, -1, view.maxTextWidth);
            view.typer = view.body.gameObject.AddComponent<Typewriter>();
            view.hint = UIKit.Label(img.transform, "", 18, UIKit.InkDim, TextAlignmentOptions.Right);

            // The tail: a white diamond half-hidden behind the bottom edge.
            var tail = UIKit.Image("Tail", img.transform, Color.white).rectTransform;
            tail.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            tail.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(26f, 26f));
            tail.localRotation = Quaternion.Euler(0, 0, 45f);
            tail.SetAsFirstSibling();

            area.gameObject.SetActive(false);
            return view;
        }

        public List<string> Paginate(string line) => Split(line, maxCharacters);

        /// <summary>
        /// Cuts a line into bubble-sized parts of about equal length, preferring to break after a comma or the end of a
        /// sentence so each bubble reads as a phrase.
        /// </summary>
        public static List<string> Split(string line, int maxCharacters)
        {
            var parts = new List<string>();
            string rest = line.Trim();
            while (rest.Length > maxCharacters)
            {
                int count = Mathf.CeilToInt(rest.Length / (float)maxCharacters);
                float target = rest.Length / (float)count;
                int best = -1;
                float bestCost = float.MaxValue;
                for (int i = rest.IndexOf(' '); i > 0 && i <= maxCharacters; i = rest.IndexOf(' ', i + 1))
                {
                    char last = rest[i - 1];
                    bool phraseEnd = last == '.' || last == ',' || last == '!' || last == '?';
                    float cost = Mathf.Abs(i - target) - (phraseEnd ? 20f : 0f);
                    if (cost < bestCost)
                    {
                        bestCost = cost;
                        best = i;
                    }
                }
                if (best < 0) break; // a single word longer than a bubble
                parts.Add(rest.Substring(0, best));
                rest = rest.Substring(best + 1).TrimStart();
            }
            if (rest.Length > 0) parts.Add(rest);
            return parts;
        }

        public void Show(Npc who, string text, bool morePages)
        {
            speaker = who;
            more = morePages;
            gameObject.SetActive(true);
            hint.text = "";
            bodySize.preferredWidth = bodySize.minWidth = Mathf.Min(body.GetPreferredValues(text).x + 2f, maxTextWidth);
            typer.Play(text);
            LayoutRebuilder.ForceRebuildLayoutImmediate(bubble);
            Follow();
        }

        public void Finish() => typer.Finish();

        public void Hide()
        {
            typer.Finish();
            speaker = null;
            gameObject.SetActive(false);
        }

        void Update()
        {
            string h = typer.IsTyping ? "" : Typewriter.Hint(more);
            if (hint.text != h) hint.text = h;
        }

        void LateUpdate() => Follow();

        /// <summary>Keeps the bubble above the speaker's head, inside the screen.</summary>
        void Follow()
        {
            var cam = Camera.main;
            if (!speaker || !cam) return;

            Vector3 screen = cam.WorldToScreenPoint(speaker.BubbleAnchor);
            group.alpha = screen.z > 0f ? 1f : 0f; // behind the camera
            if (screen.z <= 0f) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, null, out var local);
            Vector2 half = area.rect.size * 0.5f, size = bubble.rect.size;
            local.x = Mathf.Clamp(local.x, -half.x + size.x * 0.5f + margin, half.x - size.x * 0.5f - margin);
            local.y = Mathf.Clamp(local.y, -half.y + 150f, half.y - size.y - 80f); // clear of the inventory bar and timer
            bubble.anchoredPosition = local;
        }
    }
}
