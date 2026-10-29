using UnityEngine;

namespace PreventTheMurder
{
    public enum ClueCategory { Motive, Method, Opportunity, Timeline }

    [CreateAssetMenu(menuName = "Prevent the Murder/Clue", fileName = "Clue")]
    public class ClueData : ScriptableObject
    {
        public string id;
        public string title;

        [Tooltip("Full text shown on the clue card when the clue is found.")]
        [TextArea(3, 8)] public string description;

        [Tooltip("One short line (max ~12 words) in the player's own voice, written into the notepad.")]
        public string summary;

        public ClueCategory category;

        [Tooltip("Optional: the character this clue points at.")]
        public CharacterData pointsTo;

        [Tooltip("Optional. Leave empty and the room is filled in automatically from where the clue is found.")]
        public string foundInRoom;
    }
}
