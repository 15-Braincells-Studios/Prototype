using UnityEngine;

namespace PreventTheMurder
{
    /// <summary>A person in the castle. Scene NPCs and report points find their character by <see cref="id"/>.</summary>
    [CreateAssetMenu(menuName = "Prevent the Murder/Character", fileName = "Character")]
    public class CharacterData : ScriptableObject
    {
        public string id;
        public string displayName;
        public string role;
        public Color color = Color.grey;

        [Tooltip("Listed as a Victim / Culprit option in the report panel.")]
        public bool isSuspect = true;
    }
}
