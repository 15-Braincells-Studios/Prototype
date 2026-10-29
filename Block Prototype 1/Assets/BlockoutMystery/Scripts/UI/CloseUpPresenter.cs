using System.Collections.Generic;
using UnityEngine;

namespace BlockoutMystery
{
    /// <summary>
    /// Close-up conversation: the camera moves in on the speaker's face, letterbox bars slide in, and the line is
    /// shown as a standard subtitle over the bottom bar.
    /// </summary>
    public class CloseUpPresenter : MonoBehaviour, IDialoguePresenter
    {
        [SerializeField] float barHeight = 120f;
        [SerializeField] float barSpeed = 2.5f;

        DialogueBox subtitles;
        RectTransform top, bottom;
        float bars, barsTarget;

        public bool IsTyping => subtitles.IsTyping;

        /// <summary>Creates the letterbox bars. Create it before the dialogue box so the subtitle draws on top.</summary>
        public static CloseUpPresenter Create(RectTransform parent)
        {
            var root = UIKit.Rect("Close-up", parent).Stretch();
            var p = root.gameObject.AddComponent<CloseUpPresenter>();
            p.top = Bar(root, "Letterbox Top", 1f);
            p.bottom = Bar(root, "Letterbox Bottom", 0f);
            return p;
        }

        static RectTransform Bar(RectTransform parent, string name, float y)
        {
            var bar = UIKit.Image(name, parent, Color.black).rectTransform;
            bar.anchorMin = new Vector2(0f, y);
            bar.anchorMax = new Vector2(1f, y);
            bar.pivot = new Vector2(0.5f, y);
            bar.anchoredPosition = Vector2.zero;
            bar.sizeDelta = Vector2.zero;
            return bar;
        }

        public CloseUpPresenter Bind(DialogueBox box)
        {
            subtitles = box;
            subtitles.SetPlacement(DialogueBox.Placement.Low);
            return this;
        }

        public List<string> Paginate(string line) => subtitles.Paginate(line);

        public void Show(Npc who, string text, bool more)
        {
            barsTarget = 1f;
            var cam = FollowCam();
            if (cam && who) cam.BeginShot(who.transform);
            subtitles.Show(who, text, more);
        }

        public void Finish() => subtitles.Finish();

        public void Hide()
        {
            barsTarget = 0f;
            var cam = FollowCam();
            if (cam) cam.EndShot();
            subtitles.Hide();
        }

        void Update()
        {
            bars = Mathf.MoveTowards(bars, barsTarget, Time.unscaledDeltaTime * barSpeed);
            float h = Mathf.SmoothStep(0f, 1f, bars) * barHeight;
            top.sizeDelta = bottom.sizeDelta = new Vector2(0f, h);
        }

        static FollowCamera FollowCam() => Camera.main ? Camera.main.GetComponent<FollowCamera>() : null;
    }
}
