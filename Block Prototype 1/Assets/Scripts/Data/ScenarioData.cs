using System;
using System.Collections.Generic;
using UnityEngine;

namespace PreventTheMurder
{
    [Serializable]
    public class CastEntry
    {
        public CharacterData character;
        [Tooltip("What this character says in this story. Leave empty for a character who cannot be talked to.")]
        public DialogueData dialogue;
    }

    [Serializable]
    public class CluePlacement
    {
        [Tooltip("Matches the Spot Id on an OpenableContainer or ReadableNote in the scene.")]
        public string spotId;
        public ClueData clue;
    }

    /// <summary>
    /// One complete mystery. Everything story-specific lives here, so another story is just another asset
    /// assigned to GameManager. Scene objects are matched by character id and clue spot id;
    /// spots without a placement are left empty (red herrings).
    /// </summary>
    [CreateAssetMenu(menuName = "Prevent the Murder/Scenario", fileName = "Scenario")]
    public class ScenarioData : ScriptableObject
    {
        [Header("Story")]
        public string title = "Prevent the Murder";
        [TextArea(3, 6)] public string intro;

        [Header("Clock")]
        [Min(10f)] public float timeLimitSeconds = 900f;
        public string clockLabel = "Until the Vesper Bell";

        [Header("Cast (matched to scene NPCs and report points by character id)")]
        public List<CastEntry> cast = new List<CastEntry>();

        [Header("Solution")]
        public CharacterData victim;
        public CharacterData culprit;
        public List<string> methodOptions = new List<string>();
        public int correctMethodIndex;

        [Header("Clue placement (matched to scene clue spots by spot id)")]
        public List<CluePlacement> cluePlacements = new List<CluePlacement>();
        [Tooltip("Every clue in this story, including dialogue clues. Reference and checklist only.")]
        public List<ClueData> allClues = new List<ClueData>();

        [Header("Report outcomes")]
        [TextArea(2, 4)] public string reportPrompt;
        [TextArea(2, 4)] public string partialMessage;
        [TextArea(2, 4)] public string wrongMessage;
        public string winTitle = "The murder is prevented";
        [Tooltip("{victim} and {culprit} are replaced with display names.")]
        [TextArea(2, 4)] public string winText;

        [Header("The murder (time runs out)")]
        public string loseTitle;
        [TextArea(3, 6)] public string loseText;

        public IEnumerable<CharacterData> Suspects
        {
            get
            {
                foreach (var entry in cast)
                    if (entry != null && entry.character && entry.character.isSuspect)
                        yield return entry.character;
            }
        }

        public CastEntry FindCast(string characterId)
        {
            foreach (var entry in cast)
                if (entry != null && entry.character && entry.character.id == characterId)
                    return entry;
            return null;
        }

        public ClueData FindClueForSpot(string spotId)
        {
            foreach (var placement in cluePlacements)
                if (placement != null && placement.spotId == spotId)
                    return placement.clue;
            return null;
        }

        public string FormatWinText()
        {
            return (winText ?? string.Empty)
                .Replace("{victim}", victim ? victim.displayName : "?")
                .Replace("{culprit}", culprit ? culprit.displayName : "?");
        }
    }
}
