using System.Collections.Generic;
using UnityEngine;

namespace AssassinMystery
{
    public enum ClueCategory { Motive, Means, Opportunity, Alibi, Relationship, Testimony }

    /// <summary>A piece of evidence. Where it was found is recorded at runtime, so one clue asset can live anywhere.</summary>
    [CreateAssetMenu(menuName = "Assassin Mystery/Clue", fileName = "Clue")]
    public class ClueData : ScriptableObject
    {
        public string id;
        public string title;
        public ClueCategory category;

        [Tooltip("Full text shown when the clue is found and in the notebook.")]
        [TextArea(3, 8)] public string description;

        [Tooltip("Suspects this clue visibly relates to. Leave empty when the link is not obvious from the clue itself.")]
        public List<SuspectData> relatedSuspects = new List<SuspectData>();
    }
}
