using System.Collections.Generic;
using UnityEngine;

namespace AssassinMystery
{
    /// <summary>
    /// Runs one conversation at a time. Topics that need evidence only appear once the player has it;
    /// answers can be logged to the notebook and can reveal testimony clues.
    /// </summary>
    [DefaultExecutionOrder(-70)]
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Tooltip("The NPC faces this while talking. Defaults to the player found by tag.")]
        [SerializeField] Transform listener;

        readonly HashSet<DialogueTopic> asked = new HashSet<DialogueTopic>();

        public NPCDialogue Current { get; private set; }

        public void Configure(Transform player) => listener = player;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void StartDialogue(NPCDialogue npc)
        {
            if (Current != null || !npc || !npc.Profile) return; // never two dialogue windows
            Current = npc;
            npc.BeginTalking(listener);
            InvestigationManager.Instance.MarkSpokenTo(npc.Profile);
            UIManager.Instance.ShowDialogue(npc, npc.Profile.greeting);
        }

        public List<DialogueTopic> AvailableTopics(NPCDialogue npc)
        {
            var list = new List<DialogueTopic>();
            if (!npc || !npc.Profile) return list;
            foreach (var t in npc.Profile.topics)
                if (!t.requiresClue || InvestigationManager.Instance.HasClue(t.requiresClue))
                    list.Add(t);
            return list;
        }

        public bool WasAsked(DialogueTopic t) => asked.Contains(t);

        public void Ask(DialogueTopic topic)
        {
            if (Current == null || topic == null) return;
            asked.Add(topic);
            var inv = InvestigationManager.Instance;

            if (topic.recordInNotebook)
                inv.RecordConversation(Current.Profile, topic.question, topic.response);
            if (topic.revealsClue)
                inv.DiscoverClue(topic.revealsClue, $"Testimony: {Current.Profile.displayName}");

            UIManager.Instance.ShowDialogueResponse(topic.response);
        }

        public void EndDialogue()
        {
            if (Current == null) return;
            Current.EndTalking();
            Current = null;
            UIManager.Instance.HideDialogue();
        }
    }
}
