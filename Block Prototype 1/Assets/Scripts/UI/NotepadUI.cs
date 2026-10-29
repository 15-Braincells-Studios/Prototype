using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PreventTheMurder
{
    /// <summary>
    /// The detective's notepad. It writes itself: every OnClueDiscovered adds the clue's summary as an entry,
    /// newest at the bottom. Read-only for now.
    /// Extension point: player-written notes, pinning entries, linking clues to suspects. Add new entry
    /// sources as further event listeners rather than calling into this class from gameplay code.
    /// </summary>
    public class NotepadUI : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] ScrollRect scroll;
        [SerializeField] RectTransform entryContainer;
        [SerializeField] NotepadEntryUI entryTemplate;
        [SerializeField] GameObject emptyHint;
        [Tooltip("HUD dot shown while there is an unread entry and the notepad is closed.")]
        [SerializeField] GameObject newEntryDot;
        [SerializeField] float highlightSeconds = 2.5f;

        readonly List<NotepadEntryUI> entries = new List<NotepadEntryUI>();
        NotepadEntryUI newest;
        bool hasUnread;

        public bool IsOpen => root.activeSelf;

        void Awake()
        {
            root.SetActive(false);
            entryTemplate.gameObject.SetActive(false);
            newEntryDot.SetActive(false);
        }

        void Start()
        {
            ClueManager.Instance.OnClueDiscovered += AddEntry;
            foreach (var found in ClueManager.Instance.Discovered) AddEntry(found);
        }

        void OnDestroy()
        {
            if (ClueManager.Instance) ClueManager.Instance.OnClueDiscovered -= AddEntry;
        }

        void AddEntry(DiscoveredClue found)
        {
            var entry = Instantiate(entryTemplate, entryContainer);
            entry.gameObject.SetActive(true);
            entry.Set(found.clue.summary, FormatTag(found));
            entries.Add(entry);
            newest = entry;
            emptyHint.SetActive(false);

            if (IsOpen)
            {
                ScrollToBottom();
                entry.Highlight(highlightSeconds);
            }
            else
            {
                hasUnread = true;
                newEntryDot.SetActive(true);
            }
        }

        static string FormatTag(DiscoveredClue found)
        {
            return string.IsNullOrEmpty(found.room)
                ? $"[{found.clue.category}]"
                : $"[{found.clue.category} • {found.room}]";
        }

        public void Open()
        {
            if (IsOpen) return;
            root.SetActive(true);
            UIManager.Instance.PushModal();
            ScrollToBottom();
            if (hasUnread && newest) newest.Highlight(highlightSeconds);
            hasUnread = false;
            newEntryDot.SetActive(false);
        }

        public void Close()
        {
            if (!IsOpen) return;
            root.SetActive(false);
            UIManager.Instance.PopModal();
        }

        void ScrollToBottom()
        {
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 0f;
        }
    }
}
