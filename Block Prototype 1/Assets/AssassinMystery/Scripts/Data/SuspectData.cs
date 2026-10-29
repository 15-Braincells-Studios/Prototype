using System;
using System.Collections.Generic;
using UnityEngine;

namespace AssassinMystery
{
    public enum Headwear { None, Crown, Circlet, Helmet, ChefHat, Bonnet, WizardHat, FlatCap, Skullcap, Hood }

    [Serializable]
    public class DialogueTopic
    {
        [Tooltip("What the player asks.")]
        public string question;

        [TextArea(2, 6)] public string response;

        [Tooltip("Write this answer into the notebook's Conversations section.")]
        public bool recordInNotebook = true;

        [Tooltip("Optional. The question is only offered once the player has found this clue (confrontations).")]
        public ClueData requiresClue;

        [Tooltip("Optional. Hearing this answer adds this clue to the evidence.")]
        public ClueData revealsClue;
    }

    /// <summary>
    /// A person in the castle: identity, public profile, appearance and dialogue. Guilt is NOT stored here;
    /// the solution lives on <see cref="MysteryData"/> so swapping the mystery never touches the characters.
    /// </summary>
    [CreateAssetMenu(menuName = "Assassin Mystery/Suspect", fileName = "Suspect")]
    public class SuspectData : ScriptableObject
    {
        public string id;
        public string displayName;
        public string role;

        [Tooltip("Public profile shown in the notebook.")]
        [TextArea(2, 5)] public string profile;

        [Header("Appearance (used by the scene builder)")]
        public Color coatColor = Color.grey;
        public Color accentColor = Color.white;
        public Color skinColor = new Color(0.87f, 0.7f, 0.56f);
        public Headwear headwear;
        [Range(0.85f, 1.15f)] public float heightScale = 1f;
        public bool beard;

        [Header("Dialogue")]
        [TextArea(2, 5)] public string greeting;
        public List<DialogueTopic> topics = new List<DialogueTopic>();
    }
}
