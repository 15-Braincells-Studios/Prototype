using System;
using System.Collections.Generic;
using UnityEngine;

namespace AssassinMystery
{
    public enum SuspectMark { Uninvestigated, Suspicious, PrimarySuspect, Cleared }

    /// <summary>A clue as found in this run. Kept apart from ClueData so play never writes to assets.</summary>
    public class ClueRecord
    {
        public ClueData clue;
        public string location;
        public float timeRemaining;
    }

    public class ConversationEntry
    {
        public SuspectData speaker;
        public string question;
        public string answer;
    }

    /// <summary>
    /// Everything the detective knows: clues, recorded conversations, who has been questioned and the player's
    /// own suspect marks. Marks are personal notes only and never affect who is guilty.
    /// </summary>
    [DefaultExecutionOrder(-80)]
    public class InvestigationManager : MonoBehaviour
    {
        public static InvestigationManager Instance { get; private set; }

        readonly List<ClueRecord> clues = new List<ClueRecord>();
        readonly HashSet<ClueData> clueSet = new HashSet<ClueData>();
        readonly List<ConversationEntry> conversations = new List<ConversationEntry>();
        readonly Dictionary<SuspectData, SuspectMark> marks = new Dictionary<SuspectData, SuspectMark>();
        readonly HashSet<SuspectData> spokenTo = new HashSet<SuspectData>();

        public IReadOnlyList<ClueRecord> Clues => clues;
        public IReadOnlyList<ConversationEntry> Conversations => conversations;

        public event Action<ClueRecord> OnClueDiscovered;
        /// <summary>Anything in the notebook changed.</summary>
        public event Action OnChanged;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool HasClue(ClueData clue) => clue && clueSet.Contains(clue);

        /// <returns>True if the clue is new.</returns>
        public bool DiscoverClue(ClueData clue, string location)
        {
            if (!clue || !clueSet.Add(clue)) return false;
            var record = new ClueRecord
            {
                clue = clue,
                location = location,
                timeRemaining = GameTimer.Instance ? GameTimer.Instance.Remaining : 0f
            };
            clues.Add(record);
            OnClueDiscovered?.Invoke(record);
            OnChanged?.Invoke();
            return true;
        }

        public void RecordConversation(SuspectData speaker, string question, string answer)
        {
            foreach (var c in conversations)
                if (c.speaker == speaker && c.question == question) return;
            conversations.Add(new ConversationEntry { speaker = speaker, question = question, answer = answer });
            OnChanged?.Invoke();
        }

        public void MarkSpokenTo(SuspectData s)
        {
            if (s && spokenTo.Add(s)) OnChanged?.Invoke();
        }

        public bool HasSpokenTo(SuspectData s) => s && spokenTo.Contains(s);

        public SuspectMark GetMark(SuspectData s) => s && marks.TryGetValue(s, out var m) ? m : SuspectMark.Uninvestigated;

        public void SetMark(SuspectData s, SuspectMark mark)
        {
            if (!s) return;
            marks[s] = mark;
            OnChanged?.Invoke();
        }

        public static string MarkLabel(SuspectMark m) => m switch
        {
            SuspectMark.Uninvestigated => "Uninvestigated",
            SuspectMark.Suspicious => "Suspicious",
            SuspectMark.PrimarySuspect => "Primary Suspect",
            SuspectMark.Cleared => "Cleared",
            _ => m.ToString()
        };
    }
}
