using System.Collections.Generic;
using UnityEngine;

namespace AssassinMystery
{
    /// <summary>
    /// One complete mystery: who is in it, who is guilty and what proves it. Assign a different asset on
    /// GameManager to play another case.
    /// </summary>
    [CreateAssetMenu(menuName = "Assassin Mystery/Mystery", fileName = "Mystery")]
    public class MysteryData : ScriptableObject
    {
        public string title = "The Vesper Toast";
        public string subtitle = "A Medieval Assassination Mystery";
        [TextArea(4, 12)] public string briefing;

        [Header("Cast")]
        [Tooltip("The royal the assassin is after. Can be talked to, but cannot be accused.")]
        public SuspectData target;
        public List<SuspectData> suspects = new List<SuspectData>();

        [Header("Solution")]
        public SuspectData assassin;
        [Tooltip("Clues that genuinely prove the assassin's guilt. An accusation must present enough of them.")]
        public List<ClueData> keyEvidence = new List<ClueData>();
        [TextArea(4, 12)] public string solution;

        [Header("Balancing")]
        [Tooltip("Seconds until the assassination.")]
        public float durationSeconds = 600f;
    }
}
