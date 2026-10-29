using System;
using System.Collections.Generic;
using UnityEngine;

namespace PreventTheMurder
{
    [Serializable]
    public class DialogueLine
    {
        [TextArea(2, 4)] public string text;

        [Tooltip("Seeing this line discovers the clue.")]
        public ClueData clueToUnlock;

        [Tooltip("The line is skipped until the player has found this clue.")]
        public ClueData requiredClue;
    }

    [Serializable]
    public class DialogueTopic
    {
        [Tooltip("Button label, e.g. \"Ask about last night\".")]
        public string title;

        [Tooltip("The topic only appears once the player has found this clue.")]
        public ClueData requiredClue;

        public List<DialogueLine> lines = new List<DialogueLine>();
    }

    /// <summary>
    /// What one character says. The greeting plays first, then the player picks topics until "Farewell".
    /// Leave <see cref="topics"/> empty for a purely linear conversation.
    /// </summary>
    [CreateAssetMenu(menuName = "Prevent the Murder/Dialogue", fileName = "Dialogue")]
    public class DialogueData : ScriptableObject
    {
        public List<DialogueLine> greeting = new List<DialogueLine>();
        public List<DialogueTopic> topics = new List<DialogueTopic>();
        public string farewellLabel = "Farewell.";
    }
}
