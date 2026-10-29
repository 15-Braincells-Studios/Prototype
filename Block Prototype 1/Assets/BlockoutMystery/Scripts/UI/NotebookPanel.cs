using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlockoutMystery
{
    public enum JournalTab { Objectives, Evidence, Conversations, Bookmarks }

    /// <summary>
    /// The journal (Q), in four tabs: Objectives (the Butler's tasks, static), Evidence (sorted into physical evidence,
    /// discovered facts and noteworthy statements), Conversations (one expandable transcript per person) and Bookmarks
    /// (whatever the player marked in the other two). The report uses a second copy in pick mode, without Objectives,
    /// where entries can be selected as evidence (any number of them).
    /// </summary>
    public class NotebookPanel : MonoBehaviour
    {
        static readonly Color SpeakerColor = new Color(0.55f, 0.32f, 0.08f);
        static readonly Color EvidenceColor = new Color(0.12f, 0.4f, 0.5f);
        static readonly Color CardColor = new Color(1f, 1f, 1f, 0.5f);
        static readonly Color TabOff = new Color(0.14f, 0.11f, 0.09f, 0.1f);
        static readonly Color MarkOff = new Color(0.14f, 0.11f, 0.09f, 0.14f);

        RectTransform list;
        JournalTab[] shown;
        Button[] tabs;
        TextMeshProUGUI selectedLabel;
        readonly HashSet<string> expanded = new HashSet<string>();
        List<Notebook.Item> selection;
        Action selectionChanged;
        JournalTab tab;
        bool dirty;

        bool PickMode => selection != null;
        static Notebook Journal => Notebook.Instance;

        /// <param name="pick">
        /// Pick mode (the report's evidence picker): every entry gets a Select button that adds it to or removes it from
        /// this list, after which <paramref name="changed"/> is called.
        /// </param>
        public static NotebookPanel Create(RectTransform parent, List<Notebook.Item> pick = null, Action changed = null)
        {
            bool picking = pick != null;
            var panel = UIKit.Image(picking ? "Evidence Picker" : "Journal", parent, UIKit.Paper, true);
            panel.rectTransform.Anchors(0.22f, 0.07f, 0.78f, 0.93f);
            var view = panel.gameObject.AddComponent<NotebookPanel>();
            view.selection = pick;
            view.selectionChanged = changed;
            view.shown = picking
                ? new[] { JournalTab.Evidence, JournalTab.Conversations, JournalTab.Bookmarks }
                : new[] { JournalTab.Objectives, JournalTab.Evidence, JournalTab.Conversations, JournalTab.Bookmarks };
            view.tab = view.shown[0];

            var title = UIKit.Label(panel.transform, picking ? "Select evidence" : "Journal", 46, UIKit.Ink, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            title.rectTransform.Stretch(40, 28, 40, 0);
            var sub = UIKit.Label(panel.transform, picking
                ? "Select every entry from your journal that backs up your report. Irrelevant evidence won't help your case."
                : "Everything of note is written down for you. Mark what matters to keep it in Bookmarks.", 24, UIKit.InkDim);
            sub.rectTransform.Stretch(42, 88, 40, 0);
            var hint = UIKit.Label(panel.transform, picking
                ? "<b>A / D</b>  switch tab      <b>Esc</b>  back to the report"
                : "<b>A / D</b>  switch tab      <b>Q</b>  close", 22, UIKit.InkDim, TextAlignmentOptions.BottomRight);
            hint.rectTransform.Stretch(40, 0, 36, 18);

            var tabRow = UIKit.Rect("Tabs", panel.transform);
            tabRow.anchorMin = new Vector2(0, 1);
            tabRow.anchorMax = new Vector2(1, 1);
            tabRow.pivot = new Vector2(0.5f, 1);
            tabRow.offsetMin = new Vector2(36, -190);
            tabRow.offsetMax = new Vector2(-36, -134);
            UIKit.Row(tabRow, 10, 0).childAlignment = TextAnchor.MiddleLeft;
            view.tabs = new Button[view.shown.Length];
            for (int i = 0; i < view.shown.Length; i++)
            {
                var t = view.shown[i];
                view.tabs[i] = UIKit.Button(tabRow, t.ToString(), () => view.Show(t), TabOff, UIKit.Ink, 22, 52, 236);
            }

            view.list = UIKit.ScrollList(panel.transform, 10);
            ((RectTransform)view.list.parent).Stretch(36, 204, 36, picking ? 84 : 56);

            if (picking)
            {
                UIKit.Button(panel.transform, "Done", () => view.SetVisible(false), UIKit.Ink, UIKit.Paper, 24, 50, 160)
                    .GetComponent<RectTransform>().Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 20), new Vector2(160, 50));
                view.selectedLabel = UIKit.Label(panel.transform, "", 22, UIKit.InkDim, TextAlignmentOptions.Left);
                view.selectedLabel.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(216, 20), new Vector2(360, 50));
            }

            panel.gameObject.SetActive(false);
            return view;
        }

        /// <summary>Opens on the last tab used (the journal) or on Bookmarks when there are any (the evidence picker).</summary>
        public void SetVisible(bool visible)
        {
            if (visible == gameObject.activeSelf) return;
            if (visible)
            {
                if (PickMode) tab = Journal.Bookmarks.Count > 0 ? JournalTab.Bookmarks : JournalTab.Evidence;
                gameObject.SetActive(true);
                Journal.Changed += MarkDirty;
                Refresh();
            }
            else
            {
                Journal.Changed -= MarkDirty;
                gameObject.SetActive(false);
            }
        }

        void OnDestroy()
        {
            if (Journal) Journal.Changed -= MarkDirty;
        }

        public void Show(JournalTab which)
        {
            tab = which;
            Refresh();
        }

        void MarkDirty() => dirty = true;

        void Update()
        {
            // The evidence picker sits under the journal: leave the keys to the journal while it is open.
            if (!PickMode || !GameFlow.Instance.NotebookOpen)
            {
                int i = Array.IndexOf(shown, tab), n = shown.Length;
                if (GameInput.PageNext) Show(shown[(i + 1) % n]);
                else if (GameInput.PagePrevious) Show(shown[(i + n - 1) % n]);
            }
            if (dirty) Refresh();
        }

        void Refresh()
        {
            dirty = false;
            for (int i = 0; i < shown.Length; i++)
            {
                var t = shown[i];
                bool on = t == tab;
                ((Image)tabs[i].targetGraphic).color = on ? UIKit.Ink : TabOff;
                var label = tabs[i].GetComponentInChildren<TextMeshProUGUI>();
                int count = t switch
                {
                    JournalTab.Evidence => Journal.Evidence.Count,
                    JournalTab.Conversations => Journal.Conversations.Count,
                    JournalTab.Bookmarks => Journal.Bookmarks.Count,
                    _ => -1,
                };
                label.text = count < 0 ? t.ToString() : $"{t} ({count})";
                label.color = on ? UIKit.Paper : UIKit.Ink;
            }
            if (selectedLabel) selectedLabel.text = selection.Count == 1 ? "1 piece of evidence selected" : $"{selection.Count} pieces of evidence selected";

            UIKit.Clear(list);
            switch (tab)
            {
                case JournalTab.Objectives: BuildObjectives(); break;
                case JournalTab.Evidence: BuildEvidence(); break;
                case JournalTab.Conversations: BuildConversations(); break;
                default: BuildBookmarks(); break;
            }
        }

        // ---- tabs ----

        void BuildObjectives()
        {
            Tag(list, "Your tasks from the Butler.");
            foreach (var o in CaseFile.Objectives)
            {
                var card = UIKit.Image("Objective", list, CardColor);
                UIKit.Column(card, 4, 16);
                UIKit.Label(card.transform, o.title, 28, UIKit.Ink, TextAlignmentOptions.TopLeft, FontStyles.Bold);
                UIKit.Label(card.transform, o.description, 24, UIKit.Ink);
            }
        }

        void BuildEvidence()
        {
            if (Journal.Evidence.Count == 0)
            {
                Empty("No evidence yet. Look around and talk to people: anything important is written down here.");
                return;
            }
            Group(EvidenceKind.Physical, "Physical evidence");
            Group(EvidenceKind.Discovered, "Discovered facts");
            Group(EvidenceKind.Statement, "Noteworthy statements");
        }

        void Group(EvidenceKind kind, string heading)
        {
            bool any = false;
            foreach (var e in Journal.Evidence)
            {
                if (e.kind != kind) continue;
                if (!any) Heading(heading);
                any = true;
                var body = Card(Notebook.Item.Evidence(e.id), 0);
                EvidenceBody(body, e, SourceOf(e));
            }
        }

        void BuildConversations()
        {
            if (Journal.Conversations.Count == 0)
            {
                Empty("You haven't talked to anyone yet.");
                return;
            }
            foreach (var talk in Journal.Conversations)
            {
                bool open = expanded.Contains(talk.speakerId);
                ConversationHeader(talk, open);
                if (!open) continue;
                for (int i = 0; i < talk.lines.Count; i++)
                {
                    var body = Card(Notebook.Item.Line(talk.speakerId, i), 36);
                    UIKit.Label(body, $"<color=#{UIKit.Hex(SpeakerColor)}><b>{talk.speaker}:</b></color> {talk.lines[i]}", 24, UIKit.Ink);
                }
            }
        }

        void BuildBookmarks()
        {
            if (Journal.Bookmarks.Count == 0)
            {
                Empty("Nothing bookmarked yet. Press <b>Mark</b> next to a piece of evidence, or next to a line in Conversations, to keep it here.");
                return;
            }
            foreach (var item in Journal.Bookmarks)
            {
                var body = Card(item, 0);
                if (item.isEvidence && Journal.TryGetEvidence(item.id, out var e))
                    EvidenceBody(body, e, "Evidence  ·  " + SourceOf(e));
                else
                {
                    Tag(body, Journal.TitleOf(item));
                    UIKit.Label(body, Journal.TextOf(item), 24, UIKit.Ink);
                }
            }
        }

        // ---- building blocks ----

        static string SourceOf(CaseFile.Evidence e) => e.kind switch
        {
            EvidenceKind.Physical => "Found in " + e.source,
            EvidenceKind.Discovered => "Read in " + e.source,
            _ => "Said by " + e.source,
        };

        void Heading(string text) =>
            UIKit.Size(UIKit.Label(list, text, 30, UIKit.Ink, TextAlignmentOptions.BottomLeft, FontStyles.Bold), 46);

        void Empty(string text) =>
            UIKit.Label(list, text, 26, UIKit.InkDim, TextAlignmentOptions.TopLeft, FontStyles.Italic);

        static void Tag(Transform body, string text) => UIKit.Label(body, text, 20, UIKit.InkDim);

        static void EvidenceBody(Transform body, CaseFile.Evidence e, string tag)
        {
            Tag(body, tag);
            UIKit.Label(body, e.title, 26, EvidenceColor, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            UIKit.Label(body, e.description, 24, UIKit.Ink);
        }

        /// <summary>A row in the list for one item: its text goes in the returned body, the Mark (and Select) buttons sit on the right.</summary>
        RectTransform Card(Notebook.Item item, int indent)
        {
            var card = UIKit.Image("Entry", list, CardColor);
            var row = UIKit.Row(card, 14, 0);
            row.padding = new RectOffset(16 + indent, 14, 12, 12);
            row.childAlignment = TextAnchor.UpperLeft;

            var body = UIKit.Rect("Body", card.transform);
            UIKit.Column(body, 2, 0);
            UIKit.Size(body, -1, -1, 1);

            bool marked = Journal.IsBookmarked(item);
            string mark = tab == JournalTab.Bookmarks ? "Unmark" : marked ? "Marked" : "Mark";
            UIKit.Button(card.transform, mark, () => Journal.ToggleBookmark(item),
                marked && tab != JournalTab.Bookmarks ? UIKit.Accent : MarkOff, UIKit.Ink, 22, 42, 130);
            if (PickMode)
            {
                bool selected = selection.Contains(item);
                UIKit.Button(card.transform, selected ? "Selected" : "Select", () => ToggleSelected(item),
                    selected ? UIKit.Ink : MarkOff, selected ? UIKit.Paper : UIKit.Ink, 22, 42, 130);
            }
            return body;
        }

        void ToggleSelected(Notebook.Item item)
        {
            if (!selection.Remove(item)) selection.Add(item);
            selectionChanged?.Invoke();
            dirty = true; // rebuild next frame, not while the button is handling its own click
        }

        void ConversationHeader(Notebook.Conversation talk, bool open)
        {
            var header = UIKit.Image("Conversation", list, CardColor, true);
            var button = header.gameObject.AddComponent<Button>();
            button.targetGraphic = header;
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 0.9f, 0.65f);
            colors.pressedColor = new Color(0.9f, 0.78f, 0.45f);
            button.colors = colors;
            string id = talk.speakerId;
            button.onClick.AddListener(() =>
            {
                if (!expanded.Remove(id)) expanded.Add(id);
                dirty = true; // rebuild next frame, not while this button is handling its own click
            });

            var row = UIKit.Row(header, 14, 0);
            row.padding = new RectOffset(16, 18, 10, 10);
            var title = UIKit.Label(header.transform, $"<b>{(open ? "-" : "+")}</b>   {talk.Title}", 26, SpeakerColor, TextAlignmentOptions.Left, FontStyles.Bold);
            UIKit.Size(title, 36, -1, 1);
            string count = talk.lines.Count == 1 ? "1 line" : $"{talk.lines.Count} lines";
            UIKit.Size(UIKit.Label(header.transform, count, 22, UIKit.InkDim, TextAlignmentOptions.Right), 36, 120);
        }
    }
}
