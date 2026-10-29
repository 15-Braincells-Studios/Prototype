using UnityEngine;

namespace BlockoutMystery
{
    /// <summary>One of seven identical hollow statuettes. The bottom is open; one of them hides the key inside.</summary>
    public class Statuette : Inspectable
    {
        [Tooltip("The key inside. Only active in the statuette GameFlow picked.")]
        [SerializeField] GameObject hiddenKey;

        public bool HasKey => hiddenKey && hiddenKey.activeSelf;

        public void Configure(GameObject key) => hiddenKey = key;

        public void SetHasKey(bool on)
        {
            if (hiddenKey) hiddenKey.SetActive(on);
        }

        public override string InspectHint =>
            "Hold the mouse and drag to rotate   |   Click to take things   |   E / Esc  put it back";
    }
}
