using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AssassinMystery
{
    /// <summary>The detective's notebook: Suspects (with personal marks), Evidence and Conversations.</summary>
    public class NotebookView
    {
        enum Tab { Suspects, Evidence, Conversations }

        readonly RectTransform root;
        readonly RectTransform list;
        readonly ScrollRect scroll;
        readonly Dictionary<Tab, Button> tabButtons = new Dictionary<Tab, Button>();
        Tab tab = Tab.Suspects;

        static readonly Color TabIdle = new Color(0.72f, 0.63f, 0.46f);

        public NotebookView(RectTransform canvas)
        {
            root = UIFactory.Panel("Notebook", canvas, UIFactory.Parchment, true).rectTransform.Anchors(0.07f, 0.04f, 0.93f, 0.87f);

            var title = UIFactory.Text(root, "Investigation Notebook", 40, UIFactory.Ink, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            title.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -22), new Vector2(800, 50));
            var sub = UIFactory.Text(root, "Your own notes. Marking a suspect never changes who is guilty.", 19, UIFactory.InkFaded,
                TextAlignmentOptions.TopLeft, FontStyles.Italic);
            sub.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(38, -72), new Vector2(900, 28));

            float x = 36;
            foreach (Tab t in System.Enum.GetValues(typeof(Tab)))
            {
                var tt = t;
                var b = UIFactory.Button(root, t.ToString(), () => { tab = tt; Refresh(); scroll.verticalNormalizedPosition = 1f; },
                    TabIdle, UIFactory.Ink, 24, 50);
                b.GetComponent<RectTransform>().Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -112), new Vector2(230, 50));
                tabButtons[t] = b;
                x += 240;
            }

            var close = UIFactory.Button(root, "Close  [Tab]", () => UIManager.Instance.CloseActivePanel(), UIFactory.Leather, UIFactory.Parchment, 22, 50);
            close.GetComponent<RectTransform>().Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-36, -24), new Vector2(200, 50));
            var accuse = UIFactory.Button(root, "Make an Accusation", () => UIManager.Instance.OpenAccusation(), UIFactory.Blood, UIFactory.Parchment, 24, 50);
            accuse.GetComponent<RectTransform>().Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-36, -112), new Vector2(300, 50));
            accuse.gameObject.AddComponent<Outline>().effectColor = UIFactory.Gold;

            var area = UIFactory.Rect("Pages", root).Stretch(30, 176, 30, 26);
            var line = UIFactory.Panel("Rule", root, UIFactory.InkFaded);
            line.rectTransform.anchorMin = new Vector2(0, 1);
            line.rectTransform.anchorMax = new Vector2(1, 1);
            line.rectTransform.offsetMin = new Vector2(36, -172);
            line.rectTransform.offsetMax = new Vector2(-36, -170);
            list = UIFactory.ScrollList(area, 10, out scroll);
            scroll.GetComponent<RectTransform>().Stretch();

            root.gameObject.SetActive(false);
        }

        public void Show()
        {
            root.gameObject.SetActive(true);
            Refresh();
        }

        public void Hide() => root.gameObject.SetActive(false);

        public void Refresh()
        {
            if (!root.gameObject.activeSelf) return;
            foreach (var kv in tabButtons)
                kv.Value.GetComponent<Image>().color = kv.Key == tab ? UIFactory.Gold : TabIdle;

            UIFactory.Clear(list);
            switch (tab)
            {
                case Tab.Suspects: BuildSuspects(); break;
                case Tab.Evidence: BuildEvidence(); break;
                case Tab.Conversations: BuildConversations(); break;
            }
        }

        static Color MarkColor(SuspectMark m) => m switch
        {
            SuspectMark.Suspicious => new Color(0.85f, 0.58f, 0.2f),
            SuspectMark.PrimarySuspect => UIFactory.Blood,
            SuspectMark.Cleared => UIFactory.Moss,
            _ => new Color(0.5f, 0.42f, 0.32f)
        };

        void BuildSuspects()
        {
            var inv = InvestigationManager.Instance;
            var mystery = GameManager.Instance.Mystery;
            foreach (var s in mystery.suspects)
            {
                var suspect = s;
                var mark = inv.GetMark(s);
                var row = UIFactory.Panel("Suspect", list, new Color(0.86f, 0.78f, 0.6f));
                UIFactory.Horizontal(row, 16, 12);

                var swatch = UIFactory.Panel("Swatch", row.transform, s.coatColor);
                UIFactory.Layout(swatch, 86, 16);

                var info = UIFactory.Rect("Info", row.transform);
                UIFactory.Vertical(info, 2);
                UIFactory.Layout(info, -1, 300, 1);
                UIFactory.Text(info, $"<b>{s.displayName}</b>  <i><color=#{UIFactory.Hex(UIFactory.InkFaded)}>{s.role}</color></i>", 24, UIFactory.Ink);
                UIFactory.Text(info, s.profile, 18, UIFactory.Ink);
                bool spoken = inv.HasSpokenTo(s);
                UIFactory.Text(info, spoken ? "Questioned" : "Not yet questioned", 16,
                    spoken ? UIFactory.Moss : UIFactory.InkFaded, TextAlignmentOptions.TopLeft, FontStyles.Italic);

                var marks = UIFactory.Rect("Marks", row.transform);
                UIFactory.Horizontal(marks, 6);
                foreach (SuspectMark m in System.Enum.GetValues(typeof(SuspectMark)))
                {
                    var mm = m;
                    bool on = m == mark;
                    var b = UIFactory.Button(marks, InvestigationManager.MarkLabel(m), () => { inv.SetMark(suspect, mm); },
                        on ? MarkColor(m) : new Color(0.78f, 0.7f, 0.54f), on ? UIFactory.Parchment : UIFactory.InkFaded, 17, 42);
                    UIFactory.Layout(b, 42, 150);
                }
            }
        }

        void BuildEvidence()
        {
            var inv = InvestigationManager.Instance;
            if (inv.Clues.Count == 0)
            {
                UIFactory.Text(list, "No evidence yet. Search furniture and question the household.", 22, UIFactory.InkFaded,
                    TextAlignmentOptions.TopLeft, FontStyles.Italic);
                return;
            }
            foreach (var r in inv.Clues)
                EvidenceRow(list, r, null);
        }

        /// <summary>Shared with the accusation screen.</summary>
        public static Image EvidenceRow(Transform parent, ClueRecord r, string prefix)
        {
            var row = UIFactory.Panel("Clue", parent, new Color(0.86f, 0.78f, 0.6f));
            UIFactory.Vertical(row, 3, 14);
            var c = r.clue;
            UIFactory.Text(row.transform,
                $"{prefix}<b>{c.title}</b>   <size=17><color=#{UIFactory.Hex(UIFactory.Blood)}>{c.category.ToString().ToUpper()}</color></size>",
                24, UIFactory.Ink);
            UIFactory.Text(row.transform, $"Found: {r.location}   (with {GameTimer.Format(r.timeRemaining)} left)", 16, UIFactory.InkFaded,
                TextAlignmentOptions.TopLeft, FontStyles.Italic);
            UIFactory.Text(row.transform, c.description, 19, UIFactory.Ink);
            string related = c.relatedSuspects.Count > 0 ? string.Join(", ", c.relatedSuspects.Where(s => s).Select(s => s.displayName)) : "Unknown";
            UIFactory.Text(row.transform, $"<b>Relates to:</b> {related}", 17, UIFactory.InkFaded);
            return row;
        }

        void BuildConversations()
        {
            var inv = InvestigationManager.Instance;
            if (inv.Conversations.Count == 0)
            {
                UIFactory.Text(list, "No conversations recorded yet. Press E near someone to question them.", 22, UIFactory.InkFaded,
                    TextAlignmentOptions.TopLeft, FontStyles.Italic);
                return;
            }
            foreach (var group in inv.Conversations.GroupBy(c => c.speaker))
            {
                var block = UIFactory.Panel("Speaker", list, new Color(0.86f, 0.78f, 0.6f));
                UIFactory.Vertical(block, 6, 14);
                UIFactory.Text(block.transform,
                    $"<b>{group.Key.displayName}</b>  <i><color=#{UIFactory.Hex(UIFactory.InkFaded)}>{group.Key.role}</color></i>", 25, UIFactory.Ink);
                foreach (var e in group)
                {
                    UIFactory.Text(block.transform, $"<b>You:</b> {e.question}", 18, UIFactory.InkFaded, TextAlignmentOptions.TopLeft, FontStyles.Italic);
                    UIFactory.Text(block.transform, e.answer, 19, UIFactory.Ink);
                }
            }
        }
    }
}
