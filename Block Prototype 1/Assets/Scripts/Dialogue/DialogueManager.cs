using System.Collections.Generic;
using UnityEngine;

namespace PreventTheMurder
{
    /// <summary>
    /// Runs one conversation at a time: greeting lines, then a topic menu until the player says farewell.
    /// Lines and topics with a requiredClue stay hidden until that clue is found.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [SerializeField] DialoguePanelUI panel;

        readonly Queue<DialogueLine> pending = new Queue<DialogueLine>();
        readonly Dictionary<DialogueData, HashSet<int>> askedTopics = new Dictionary<DialogueData, HashSet<int>>();
        readonly List<int> availableTopics = new List<int>();
        readonly List<(string label, bool asked)> topicOptions = new List<(string, bool)>();

        NPCController speaker;

        public bool IsActive => speaker != null;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void StartDialogue(NPCController npc, Transform player)
        {
            if (IsActive || npc.Dialogue == null) return;

            speaker = npc;
            speaker.BeginConversation(player);
            UIManager.Instance.PushModal();
            panel.Open(speaker.DisplayName);

            Enqueue(speaker.Dialogue.greeting);
            Advance();
        }

        public void EndDialogue()
        {
            if (!IsActive) return;
            pending.Clear();
            panel.Close();
            var npc = speaker;
            speaker = null;
            npc.EndConversation();
            UIManager.Instance.PopModal();
        }

        void Enqueue(List<DialogueLine> lines)
        {
            if (lines == null) return;
            foreach (var line in lines)
                if (line != null && IsUnlocked(line.requiredClue))
                    pending.Enqueue(line);
        }

        static bool IsUnlocked(ClueData requiredClue) => requiredClue == null || ClueManager.Instance.HasClue(requiredClue);

        void Advance()
        {
            if (!IsActive) return;

            if (pending.Count > 0)
            {
                var line = pending.Dequeue();
                panel.ShowLine(line.text, Advance);
                if (line.clueToUnlock)
                    ClueManager.Instance.Discover(line.clueToUnlock, RoomVolume.NameAt(speaker.transform.position));
                return;
            }

            ShowTopicMenu();
        }

        void ShowTopicMenu()
        {
            var data = speaker.Dialogue;
            availableTopics.Clear();
            for (int i = 0; i < data.topics.Count; i++)
                if (data.topics[i] != null && IsUnlocked(data.topics[i].requiredClue))
                    availableTopics.Add(i);

            if (availableTopics.Count == 0)
            {
                EndDialogue();
                return;
            }

            if (!askedTopics.TryGetValue(data, out var asked))
            {
                asked = new HashSet<int>();
                askedTopics[data] = asked;
            }

            topicOptions.Clear();
            foreach (int i in availableTopics)
                topicOptions.Add((data.topics[i].title, asked.Contains(i)));

            panel.ShowTopics(topicOptions, choice =>
            {
                int topicIndex = availableTopics[choice];
                asked.Add(topicIndex);
                Enqueue(data.topics[topicIndex].lines);
                Advance();
            }, data.farewellLabel, EndDialogue);
        }
    }
}
