using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlockoutMystery
{
    /// <summary>
    /// The journal, filled in automatically. Every line the player hears goes into a conversation transcript (one per
    /// person); whatever the game thinks is important (physical clues, discovered facts, noteworthy statements) also
    /// goes into the evidence list. The player can bookmark single lines and pieces of evidence.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class Notebook : MonoBehaviour
    {
        /// <summary>Everything one person has said to the player, in the order they heard it.</summary>
        public class Conversation
        {
            public readonly string speakerId, speaker;
            public readonly List<string> lines = new List<string>();
            public Conversation(string speakerId, string speaker) { this.speakerId = speakerId; this.speaker = speaker; }
            public string Title => "Talked to the " + speaker;
        }

        /// <summary>What can be bookmarked or handed in with the report: a piece of evidence, or one line of a conversation.</summary>
        public readonly struct Item : IEquatable<Item>
        {
            public readonly bool isEvidence;
            /// <summary>Evidence id, or the speaker id for a line.</summary>
            public readonly string id;
            /// <summary>Index into the conversation's lines (lines are never removed, so this stays valid).</summary>
            public readonly int line;

            Item(bool isEvidence, string id, int line) { this.isEvidence = isEvidence; this.id = id; this.line = line; }
            public static Item Evidence(string evidenceId) => new Item(true, evidenceId, -1);
            public static Item Line(string speakerId, int index) => new Item(false, speakerId, index);

            public bool Equals(Item other) => isEvidence == other.isEvidence && id == other.id && line == other.line;
            public override bool Equals(object obj) => obj is Item other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(isEvidence, id, line);
        }

        public static Notebook Instance { get; private set; }

        readonly List<CaseFile.Evidence> evidence = new List<CaseFile.Evidence>();
        readonly List<Conversation> conversations = new List<Conversation>();
        readonly List<Item> bookmarks = new List<Item>();

        public IReadOnlyList<CaseFile.Evidence> Evidence => evidence;
        public IReadOnlyList<Conversation> Conversations => conversations;
        public IReadOnlyList<Item> Bookmarks => bookmarks;
        public bool IsEmpty => evidence.Count == 0 && conversations.Count == 0;
        public event Action Changed;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Adds a line the player just heard to that person's transcript (once), and its evidence if it is noteworthy.</summary>
        public void Record(string speakerId, string speaker, string line)
        {
            var talk = ConversationWith(speakerId);
            if (talk == null)
            {
                talk = new Conversation(speakerId, speaker);
                conversations.Add(talk);
            }
            if (talk.lines.Contains(line)) return;
            talk.lines.Add(line);

            var noteworthy = CaseFile.NoteworthyIn(speakerId, line);
            if (noteworthy.HasValue) RecordEvidence(noteworthy.Value);
            Changed?.Invoke();
        }

        /// <summary>Adds a piece of evidence (once). True if it is new. <paramref name="announce"/> tells the player in a toast.</summary>
        public bool RecordEvidence(CaseFile.Evidence item, bool announce = true)
        {
            if (HasEvidence(item.id)) return false;
            evidence.Add(item);
            if (announce) UIRoot.Toast($"Added to your journal: {item.title}");
            Changed?.Invoke();
            return true;
        }

        public bool HasEvidence(string evidenceId) => TryGetEvidence(evidenceId, out _);

        public bool TryGetEvidence(string evidenceId, out CaseFile.Evidence found)
        {
            foreach (var e in evidence)
                if (e.id == evidenceId)
                {
                    found = e;
                    return true;
                }
            found = default;
            return false;
        }

        public Conversation ConversationWith(string speakerId)
        {
            foreach (var c in conversations) if (c.speakerId == speakerId) return c;
            return null;
        }

        public bool HasSpokenTo(string speakerId) => ConversationWith(speakerId) != null;

        // ---- bookmarks ----

        public bool IsBookmarked(Item item) => bookmarks.Contains(item);

        public void SetBookmarked(Item item, bool on)
        {
            if (on == IsBookmarked(item)) return;
            if (on) bookmarks.Add(item);
            else bookmarks.Remove(item);
            Changed?.Invoke();
        }

        public void ToggleBookmark(Item item) => SetBookmarked(item, !IsBookmarked(item));

        // ---- describing items ----

        /// <summary>The heading for an item: the evidence's title, or the conversation it is from.</summary>
        public string TitleOf(Item item)
        {
            if (item.isEvidence) return TryGetEvidence(item.id, out var e) ? e.title : item.id;
            var talk = ConversationWith(item.id);
            return talk != null ? talk.Title : CaseFile.NameOf(item.id);
        }

        /// <summary>The body of an item: the evidence's description, or the line itself ("Chef: ...").</summary>
        public string TextOf(Item item)
        {
            if (item.isEvidence) return TryGetEvidence(item.id, out var e) ? e.description : "";
            var talk = ConversationWith(item.id);
            if (talk == null || item.line < 0 || item.line >= talk.lines.Count) return "";
            return $"{talk.speaker}: “{talk.lines[item.line]}”";
        }

        /// <summary>
        /// The evidence an item stands for: its own id, or for a line the noteworthy statement it was summarised as (so
        /// the guard's line and the guard's statement count as the same piece). Null if it isn't evidence.
        /// </summary>
        public string EvidenceIdOf(Item item)
        {
            if (item.isEvidence) return item.id;
            var talk = ConversationWith(item.id);
            if (talk == null || item.line < 0 || item.line >= talk.lines.Count) return null;
            return CaseFile.NoteworthyIn(item.id, talk.lines[item.line])?.id;
        }

        /// <summary>A one-line name for the report and the end screen.</summary>
        public string NameOf(Item item) => item.isEvidence ? TitleOf(item) : TextOf(item);
    }
}
