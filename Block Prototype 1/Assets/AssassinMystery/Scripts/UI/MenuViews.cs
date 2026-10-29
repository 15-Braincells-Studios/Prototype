using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AssassinMystery
{
    /// <summary>Full-screen menus: title/briefing (the main menu), pause, and the victory / failure screens.</summary>
    public class MenuViews
    {
        readonly RectTransform briefing, pause, end;
        readonly TextMeshProUGUI briefTitle, briefSub, briefBody;
        readonly TextMeshProUGUI endTitle, endBody;
        readonly Image endBackdrop;

        public MenuViews(RectTransform canvas)
        {
            // Briefing / main menu.
            briefing = UIFactory.Panel("Briefing", canvas, new Color(0.05f, 0.04f, 0.03f, 0.97f)).rectTransform.Stretch();
            var card = UIFactory.Panel("Card", briefing, UIFactory.Parchment, true).rectTransform.Anchors(0.24f, 0.12f, 0.76f, 0.88f);
            var col = UIFactory.Rect("Column", card).Stretch(56, 44, 56, 120);
            UIFactory.Vertical(col, 14);
            briefTitle = UIFactory.Text(col, "", 60, UIFactory.Ink, TextAlignmentOptions.Top, FontStyles.Bold);
            briefSub = UIFactory.Text(col, "", 24, UIFactory.Blood, TextAlignmentOptions.Top, FontStyles.Italic);
            UIFactory.Divider(col, UIFactory.InkFaded);
            briefBody = UIFactory.Text(col, "", 22, UIFactory.Ink);
            UIFactory.Divider(col, UIFactory.InkFaded);
            UIFactory.Text(col,
                "<b>WASD</b> move  -  <b>Mouse</b> look  -  <b>Shift</b> hurry  -  <b>E</b> talk / search  -  <b>Tab</b> notebook  -  <b>Esc</b> pause",
                19, UIFactory.InkFaded, TextAlignmentOptions.Top);
            var begin = UIFactory.Button(card, "Begin the Investigation", () => GameManager.Instance.BeginInvestigation(),
                UIFactory.Blood, UIFactory.Parchment, 28, 66);
            begin.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(440, 66));
            begin.gameObject.AddComponent<Outline>().effectColor = UIFactory.Gold;
            briefing.gameObject.SetActive(false);

            // Pause.
            pause = UIFactory.Panel("Pause", canvas, new Color(0, 0, 0, 0.6f)).rectTransform.Stretch();
            var pcard = UIFactory.Panel("Card", pause, UIFactory.Parchment, true).rectTransform
                .Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460, 420));
            var pcol = UIFactory.Rect("Column", pcard).Stretch(50, 40, 50, 40);
            UIFactory.Vertical(pcol, 16);
            UIFactory.Text(pcol, "Paused", 48, UIFactory.Ink, TextAlignmentOptions.Top, FontStyles.Bold);
            UIFactory.Text(pcol, "The bell waits for no one... except you.", 19, UIFactory.InkFaded, TextAlignmentOptions.Top, FontStyles.Italic);
            UIFactory.Button(pcol, "Resume", () => GameManager.Instance.Resume(), UIFactory.Leather, UIFactory.Parchment, 26, 60);
            UIFactory.Button(pcol, "Restart", () => GameManager.Instance.Restart(), UIFactory.Leather, UIFactory.Parchment, 26, 60);
            UIFactory.Button(pcol, "Main Menu", () => GameManager.Instance.ReturnToMainMenu(), UIFactory.Leather, UIFactory.Parchment, 26, 60);
            pause.gameObject.SetActive(false);

            // Ending.
            endBackdrop = UIFactory.Panel("End", canvas, new Color(0.04f, 0.03f, 0.02f, 0.94f));
            end = endBackdrop.rectTransform.Stretch();
            var ecard = UIFactory.Panel("Card", end, UIFactory.Parchment, true).rectTransform.Anchors(0.2f, 0.07f, 0.8f, 0.93f);
            var ecol = UIFactory.Rect("Column", ecard).Stretch(56, 44, 56, 120);
            UIFactory.Vertical(ecol, 16);
            endTitle = UIFactory.Text(ecol, "", 62, UIFactory.Ink, TextAlignmentOptions.Top, FontStyles.Bold);
            UIFactory.Divider(ecol, UIFactory.InkFaded);
            endBody = UIFactory.Text(ecol, "", 22, UIFactory.Ink);

            var row = UIFactory.Rect("Buttons", ecard).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(720, 64));
            var h = UIFactory.Horizontal(row, 24);
            h.childForceExpandWidth = true;
            h.childAlignment = TextAnchor.MiddleCenter;
            UIFactory.Button(row, "Play Again", () => GameManager.Instance.Restart(), UIFactory.Blood, UIFactory.Parchment, 26, 64);
            UIFactory.Button(row, "Main Menu", () => GameManager.Instance.ReturnToMainMenu(), UIFactory.Leather, UIFactory.Parchment, 26, 64);
            end.gameObject.SetActive(false);
        }

        public void ShowBriefing(MysteryData m)
        {
            briefTitle.text = m ? m.title : "Mystery";
            briefSub.text = m ? m.subtitle : "";
            briefBody.text = m ? m.briefing : "";
            briefing.gameObject.SetActive(true);
        }

        public void HideBriefing() => briefing.gameObject.SetActive(false);

        public void ShowPause(bool on)
        {
            pause.gameObject.SetActive(on);
            if (on) pause.SetAsLastSibling();
        }

        public void ShowEnd(bool won, string title, string body)
        {
            endTitle.text = title;
            endTitle.color = won ? UIFactory.Moss : UIFactory.Blood;
            endBody.text = body;
            end.gameObject.SetActive(true);
        }
    }
}
