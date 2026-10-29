using UnityEngine;

namespace BlockoutMystery
{
    /// <summary>A character who stands still and says their one line when the player presses E. The butler takes reports.</summary>
    public class Npc : Interactable
    {
        [SerializeField] string id;
        [SerializeField] string displayName;
        [TextArea(2, 4)]
        [SerializeField] string line;

        Transform nameTag;

        public string Id => id;
        public string DisplayName => displayName;

        /// <summary>Where a speech bubble sits: just above the floating name tag.</summary>
        public Vector3 BubbleAnchor
        {
            get
            {
                if (!nameTag)
                {
                    var tag = GetComponentInChildren<TMPro.TMP_Text>();
                    nameTag = tag ? tag.transform : transform;
                }
                return nameTag == transform ? transform.position + Vector3.up * 2.6f : nameTag.position + Vector3.up * 0.3f;
            }
        }

        public override string Prompt => "Talk to " + displayName;

        /// <summary>What they say: the line in <see cref="CaseFile"/> (so text changes don't need a scene rebuild), else the scene's copy.</summary>
        public string Line
        {
            get
            {
                string fromCase = CaseFile.LineOf(id);
                return string.IsNullOrEmpty(fromCase) ? line : fromCase;
            }
        }

        public void Configure(string npcId, string npcName, string npcLine)
        {
            id = npcId;
            displayName = npcName;
            line = npcLine;
        }

        public override void Interact()
        {
            if (id == CaseFile.Butler) ReportSystem.Instance.TalkToButler(this);
            else Say(Line);
        }

        /// <summary>Shows the line and writes it in the journal.</summary>
        public void Say(string text)
        {
            Notebook.Instance.Record(id, displayName, text);
            DialogueSystem.Instance.Show(this, text);
        }
    }
}
