using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlockoutMystery
{
    /// <summary>
    /// The butler's report table: one row per person, tick exactly one Suspect and one Victim, select the evidence that
    /// backs it up (any number of pieces, in a copy of the journal that opens on Bookmarks when there are any), then submit.
    /// </summary>
    public class ReportPanel : MonoBehaviour
    {
        readonly Dictionary<string, Button> suspectButtons = new Dictionary<string, Button>();
        readonly Dictionary<string, Button> victimButtons = new Dictionary<string, Button>();
        TextMeshProUGUI status, evidenceLabel;
        Button submit;
        string suspect, victim;
        readonly List<Notebook.Item> evidence = new List<Notebook.Item>();
        NotebookPanel picker;

        public static ReportPanel Create(RectTransform parent)
        {
            var panel = UIKit.Image("Report", parent, UIKit.Panel, true);
            panel.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 0));
            UIKit.Column(panel, 6, 36);
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var view = panel.gameObject.AddComponent<ReportPanel>();

            UIKit.Label(panel.transform, "Report to the Butler", 40, UIKit.Accent, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            UIKit.Label(panel.transform, "“Who is involved, and how do you know?”  Tick one suspect and one victim, and select the evidence that backs it up.", 24, UIKit.TextDim);

            var header = Row(panel.transform, 40);
            var nameHeader = UIKit.Label(header, "Name", 22, UIKit.TextDim, TextAlignmentOptions.Left, FontStyles.Bold);
            UIKit.Size(nameHeader, 40, -1, 1);
            UIKit.Size(UIKit.Label(header, "Suspect", 22, UIKit.TextDim, TextAlignmentOptions.Center, FontStyles.Bold), 40, 150);
            UIKit.Size(UIKit.Label(header, "Victim", 22, UIKit.TextDim, TextAlignmentOptions.Center, FontStyles.Bold), 40, 150);

            foreach (var person in CaseFile.Cast)
            {
                if (person.id == CaseFile.Butler) continue;
                string id = person.id;
                var row = Row(panel.transform, 46);
                UIKit.Size(UIKit.Label(row, person.name, 26, UIKit.Text, TextAlignmentOptions.Left), 46, -1, 1);
                view.suspectButtons[id] = UIKit.Button(row, "", () => view.ChooseSuspect(id), UIKit.PanelLight, UIKit.Ink, 26, 42, 150);
                view.victimButtons[id] = UIKit.Button(row, "", () => view.ChooseVictim(id), UIKit.PanelLight, UIKit.Ink, 26, 42, 150);
            }

            var evidenceRow = Row(panel.transform, 56);
            UIKit.Size(UIKit.Label(evidenceRow, "Evidence", 26, UIKit.TextDim, TextAlignmentOptions.Left, FontStyles.Bold), 46, 150);
            view.evidenceLabel = UIKit.Label(evidenceRow, "", 24, UIKit.Text, TextAlignmentOptions.Left);
            view.evidenceLabel.textWrappingMode = TextWrappingModes.NoWrap;
            view.evidenceLabel.overflowMode = TextOverflowModes.Ellipsis;
            UIKit.Size(view.evidenceLabel, 46, -1, 1);
            UIKit.Button(evidenceRow, "Select evidence", view.OpenEvidencePicker, UIKit.PanelLight, UIKit.Text, 24, 50, 240);

            view.status = UIKit.Label(panel.transform, "", 22, UIKit.TextDim, TextAlignmentOptions.Center);
            UIKit.Size(view.status, 40);

            var buttons = Row(panel.transform, 56);
            buttons.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleRight;
            UIKit.Button(buttons, "Cancel", () => ReportSystem.Instance.Cancel(), UIKit.PanelLight, UIKit.Text, 26, 54, 200);
            view.submit = UIKit.Button(buttons, "Submit report", view.Submit, UIKit.Accent, UIKit.Ink, 26, 54, 260);

            panel.gameObject.SetActive(false);
            view.picker = NotebookPanel.Create(parent, view.evidence, view.Refresh);
            return view;
        }

        public void OpenEvidencePicker() => picker.SetVisible(true);

        /// <summary>Closes the evidence overview if it is open. False if it was not (so Escape cancels the report instead).</summary>
        public bool CloseEvidencePicker()
        {
            if (!picker.gameObject.activeSelf) return false;
            picker.SetVisible(false);
            return true;
        }

        static RectTransform Row(Transform parent, float height)
        {
            var row = UIKit.Rect("Row", parent);
            UIKit.Row(row, 12, 0);
            UIKit.Size(row, height);
            return row;
        }

        public void Open()
        {
            suspect = victim = null;
            evidence.Clear();
            gameObject.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            CloseEvidencePicker();
            gameObject.SetActive(false);
        }

        public void ChooseSuspect(string id)
        {
            suspect = id;
            Refresh();
        }

        public void ChooseVictim(string id)
        {
            victim = id;
            Refresh();
        }

        void Submit()
        {
            if (suspect != null && victim != null && evidence.Count > 0) ReportSystem.Instance.Submit(suspect, victim, evidence.ToArray());
        }

        public void Refresh()
        {
            foreach (var pair in suspectButtons) Style(pair.Value, pair.Key == suspect);
            foreach (var pair in victimButtons) Style(pair.Value, pair.Key == victim);
            if (evidence.Count == 0) evidenceLabel.text = "<i>None selected yet</i>";
            else
            {
                var names = new List<string>();
                foreach (var item in evidence) names.Add(Notebook.Instance.NameOf(item));
                evidenceLabel.text = $"<b>{evidence.Count}:</b> " + string.Join(";  ", names);
            }
            evidenceLabel.color = evidence.Count > 0 ? UIKit.Text : UIKit.TextDim;
            submit.interactable = suspect != null && victim != null && evidence.Count > 0;
            status.text = submit.interactable
                ? $"Suspect: <b>{CaseFile.NameOf(suspect)}</b>     Victim: <b>{CaseFile.NameOf(victim)}</b>     Evidence: <b>{evidence.Count}</b>"
                : "Pick one suspect, one victim and at least one piece of evidence.";
        }

        static void Style(Button b, bool on)
        {
            ((Image)b.targetGraphic).color = on ? UIKit.Accent : UIKit.PanelLight;
            b.SetLabel(on ? "X" : "");
        }
    }
}
