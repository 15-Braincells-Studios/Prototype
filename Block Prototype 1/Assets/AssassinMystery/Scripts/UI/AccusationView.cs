using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AssassinMystery
{
    /// <summary>Pick a suspect, choose the evidence to present against them, confirm.</summary>
    public class AccusationView
    {
        readonly RectTransform root;
        readonly RectTransform suspectList;
        readonly RectTransform evidenceList;
        readonly ScrollRect evidenceScroll;
        readonly TextMeshProUGUI caseTitle;
        readonly TextMeshProUGUI warning;
        readonly Button confirm;

        SuspectData selected;
        readonly HashSet<ClueData> presented = new HashSet<ClueData>();

        public AccusationView(RectTransform canvas)
        {
            root = UIFactory.Panel("Accusation", canvas, UIFactory.Parchment, true).rectTransform.Anchors(0.07f, 0.04f, 0.93f, 0.87f);

            var title = UIFactory.Text(root, "Name the Assassin", 40, UIFactory.Blood, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            title.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -22), new Vector2(800, 50));
            var sub = UIFactory.Text(root,
                "Choose the suspect, then present the evidence that proves motive, means and opportunity. Captain Hale will only act on a convincing case.",
                19, UIFactory.InkFaded, TextAlignmentOptions.TopLeft, FontStyles.Italic);
            sub.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(38, -74), new Vector2(1400, 56));

            var back = UIFactory.Button(root, "Back  [Esc]", () => UIManager.Instance.CloseActivePanel(), UIFactory.Leather, UIFactory.Parchment, 22, 50);
            back.GetComponent<RectTransform>().Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-36, -24), new Vector2(200, 50));

            // Left: suspects.
            var left = UIFactory.Rect("Suspects", root);
            left.anchorMin = Vector2.zero;
            left.anchorMax = new Vector2(0.3f, 1);
            left.offsetMin = new Vector2(30, 30);
            left.offsetMax = new Vector2(-10, -140);
            suspectList = UIFactory.ScrollList(left, 6, out var s1);
            s1.GetComponent<RectTransform>().Stretch();

            // Right: the case.
            var right = UIFactory.Rect("Case", root);
            right.anchorMin = new Vector2(0.3f, 0);
            right.anchorMax = Vector2.one;
            right.offsetMin = new Vector2(10, 110);
            right.offsetMax = new Vector2(-30, -140);
            caseTitle = UIFactory.Text(right, "", 26, UIFactory.Ink, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            caseTitle.rectTransform.anchorMin = new Vector2(0, 1);
            caseTitle.rectTransform.anchorMax = Vector2.one;
            caseTitle.rectTransform.offsetMin = new Vector2(6, -40);
            caseTitle.rectTransform.offsetMax = Vector2.zero;
            var listArea = UIFactory.Rect("Evidence", right).Stretch(0, 46, 0, 0);
            evidenceList = UIFactory.ScrollList(listArea, 8, out evidenceScroll);
            evidenceScroll.GetComponent<RectTransform>().Stretch();

            warning = UIFactory.Text(root, "", 19, UIFactory.InkFaded, TextAlignmentOptions.MidlineLeft, FontStyles.Italic);
            warning.rectTransform.anchorMin = new Vector2(0.3f, 0);
            warning.rectTransform.anchorMax = new Vector2(1, 0);
            warning.rectTransform.offsetMin = new Vector2(16, 30);
            warning.rectTransform.offsetMax = new Vector2(-420, 90);

            confirm = UIFactory.Button(root, "Accuse", Confirm, UIFactory.Blood, UIFactory.Parchment, 26, 60);
            confirm.GetComponent<RectTransform>().Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-36, 30), new Vector2(380, 60));
            confirm.gameObject.AddComponent<Outline>().effectColor = UIFactory.Gold;
            var confirmLabel = confirm.GetComponentInChildren<TextMeshProUGUI>();
            confirmLabel.enableAutoSizing = true;
            confirmLabel.fontSizeMin = 16;
            confirmLabel.fontSizeMax = 26;

            root.gameObject.SetActive(false);
        }

        public void Show()
        {
            presented.Clear();
            selected = null;
            root.gameObject.SetActive(true);
            Refresh();
        }

        public void Hide() => root.gameObject.SetActive(false);

        void Refresh()
        {
            var inv = InvestigationManager.Instance;
            var am = AccusationManager.Instance;

            UIFactory.Clear(suspectList);
            foreach (var s in GameManager.Instance.Mystery.suspects)
            {
                var suspect = s;
                bool on = s == selected;
                string mark = InvestigationManager.MarkLabel(inv.GetMark(s));
                var b = UIFactory.Button(suspectList, $"<b>{s.displayName}</b>\n<size=16><i>{s.role} - {mark}</i></size>",
                    () => { selected = suspect; Refresh(); },
                    on ? UIFactory.Blood : UIFactory.ParchmentDark, on ? UIFactory.Parchment : UIFactory.Ink, 21, 66);
                b.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
            }

            UIFactory.Clear(evidenceList);
            if (!selected)
            {
                caseTitle.text = "Select a suspect on the left.";
            }
            else
            {
                caseTitle.text = $"Evidence to present against {selected.displayName}:";
                if (inv.Clues.Count == 0)
                    UIFactory.Text(evidenceList, "You have no evidence. An accusation without proof is only a guess.", 21, UIFactory.InkFaded,
                        TextAlignmentOptions.TopLeft, FontStyles.Italic);

                // Clues that visibly mention the suspect first.
                var ordered = inv.Clues.OrderByDescending(r => r.clue.relatedSuspects.Contains(selected)).ToList();
                foreach (var r in ordered)
                {
                    var clue = r.clue;
                    bool on = presented.Contains(clue);
                    var row = NotebookView.EvidenceRow(evidenceList, r, on ? "<color=#8B1C14>[PRESENTED]</color>  " : "[  ]  ");
                    row.color = on ? new Color(0.95f, 0.82f, 0.62f) : new Color(0.84f, 0.76f, 0.59f);
                    var btn = row.gameObject.AddComponent<Button>();
                    btn.targetGraphic = row;
                    btn.onClick.AddListener(() =>
                    {
                        if (!presented.Remove(clue)) presented.Add(clue);
                        float pos = evidenceScroll.verticalNormalizedPosition;
                        Refresh();
                        evidenceScroll.verticalNormalizedPosition = pos;
                    });
                }
            }

            confirm.interactable = selected && presented.Count > 0;
            confirm.SetLabel(selected ? $"Accuse {selected.displayName}" : "Accuse");
            int left = am.AccusationsLeft;
            warning.text = $"Evidence presented: {presented.Count}.  A failed accusation costs {am.Penalty:0} seconds. " +
                           $"Accusations remaining: {left}{(left == 1 ? " (fail this one and the assassin strikes)" : "")}.";
        }

        void Confirm()
        {
            if (!selected || presented.Count == 0) return;
            AccusationManager.Instance.Accuse(selected, presented.ToList());
        }
    }
}
