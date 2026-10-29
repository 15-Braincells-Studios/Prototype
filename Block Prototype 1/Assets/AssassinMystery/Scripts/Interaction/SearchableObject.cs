using System.Collections;
using UnityEngine;

namespace AssassinMystery
{
    /// <summary>
    /// Furniture or an object that can be searched. Shows an inspection card; may hold one clue, which is only
    /// ever collected once. Optionally swings a door, lid or drawer open the first time.
    /// </summary>
    public class SearchableObject : Interactable
    {
        [SerializeField] string objectName = "Desk";
        [Tooltip("Prompt verb: Search, Examine, Read...")]
        [SerializeField] string verb = "Search";
        [SerializeField] string roomName = "Great Hall";
        [TextArea(2, 5)] [SerializeField] string description;
        [Tooltip("Optional clue found here.")]
        [SerializeField] ClueData clue;
        [TextArea(1, 3)] [SerializeField] string nothingFoundText = "Nothing of interest.";

        [Header("Optional open animation")]
        [SerializeField] Transform openPart;
        [SerializeField] Vector3 openLocalOffset;
        [SerializeField] Vector3 openLocalEuler;

        bool searched;

        public string ObjectName => objectName;
        public string RoomName => roomName;
        public string Description => description;
        public ClueData Clue => clue;
        public string NothingFoundText => nothingFoundText;

        public override string Prompt => searched ? $"Examine {objectName} again" : $"{verb} {objectName}";

        public void Configure(string name, string promptVerb, string room, string desc, ClueData clueData, string nothingText,
            Transform point, float range, float height)
        {
            objectName = name;
            verb = promptVerb;
            roomName = room;
            description = desc;
            clue = clueData;
            nothingFoundText = nothingText;
            interactionPoint = point;
            extraRange = range;
            promptHeight = height;
        }

        public void ConfigureOpening(Transform part, Vector3 offset, Vector3 euler)
        {
            openPart = part;
            openLocalOffset = offset;
            openLocalEuler = euler;
        }

        void Awake()
        {
            if (!clue && string.IsNullOrEmpty(description))
                Debug.LogWarning($"[AssassinMystery] Searchable '{objectName}' has neither a description nor a clue.", this);
        }

        public override void Interact()
        {
            bool isNew = false;
            if (clue && InvestigationManager.Instance != null)
                isNew = InvestigationManager.Instance.DiscoverClue(clue, $"{roomName}: {objectName}");

            if (!searched && openPart) StartCoroutine(Open());
            searched = true;

            UIManager.Instance.ShowInspection(this, isNew);
        }

        IEnumerator Open()
        {
            Vector3 startPos = openPart.localPosition;
            Quaternion startRot = openPart.localRotation;
            Vector3 endPos = startPos + openLocalOffset;
            Quaternion endRot = startRot * Quaternion.Euler(openLocalEuler);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.5f)
            {
                float e = Mathf.SmoothStep(0f, 1f, t);
                openPart.localPosition = Vector3.Lerp(startPos, endPos, e);
                openPart.localRotation = Quaternion.Slerp(startRot, endRot, e);
                yield return null;
            }
            openPart.localPosition = endPos;
            openPart.localRotation = endRot;
        }
    }
}
