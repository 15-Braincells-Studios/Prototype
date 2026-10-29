using System.Collections;
using UnityEngine;

namespace PreventTheMurder
{
    /// <summary>
    /// A drawer that slides or a lid/door that swings. The first time it opens it reveals its clue item,
    /// or shows a short "empty" toast when the scenario put nothing in it.
    /// </summary>
    public class OpenableContainer : MonoBehaviour, IInteractable, IClueSpot
    {
        public enum MotionType { Slide, Rotate }

        [SerializeField] string spotId;
        [SerializeField] string displayName = "drawer";

        [Header("Motion")]
        [SerializeField] Transform movingPart;
        [SerializeField] MotionType motion = MotionType.Slide;
        [Tooltip("Slide: local position offset when open.")]
        [SerializeField] Vector3 openOffset = new Vector3(0f, 0f, -0.35f);
        [Tooltip("Rotate: local euler rotation added when open.")]
        [SerializeField] Vector3 openEuler = new Vector3(100f, 0f, 0f);
        [SerializeField] float duration = 0.35f;

        [Header("Contents")]
        [SerializeField] ClueItem contentItem;
        [SerializeField] string emptyMessage = "Nothing of note inside.";

        ClueData clue;
        bool isOpen;
        bool isMoving;
        bool hasBeenOpened;
        Vector3 closedPosition;
        Quaternion closedRotation;

        public string SpotId => spotId;

        void Awake()
        {
            if (!movingPart) movingPart = transform;
            closedPosition = movingPart.localPosition;
            closedRotation = movingPart.localRotation;
            if (contentItem) contentItem.gameObject.SetActive(false);
        }

        public void AssignClue(ClueData assigned)
        {
            clue = assigned;
            if (contentItem) contentItem.SetClue(assigned);
        }

        public string GetPrompt()
        {
            if (isMoving) return null;
            return $"[{InputReader.InteractKeyLabel}] {(isOpen ? "Close" : "Open")} {displayName}";
        }

        public void Interact(PlayerInteractor player)
        {
            if (isMoving) return;
            isOpen = !isOpen;
            StartCoroutine(Animate(isOpen));

            if (isOpen && !hasBeenOpened)
            {
                hasBeenOpened = true;
                if (clue && contentItem) contentItem.gameObject.SetActive(true);
                else if (!string.IsNullOrEmpty(emptyMessage) && UIManager.Instance) UIManager.Instance.ShowToast(emptyMessage);
            }
        }

        IEnumerator Animate(bool open)
        {
            isMoving = true;
            Vector3 fromPosition = movingPart.localPosition;
            Quaternion fromRotation = movingPart.localRotation;
            Vector3 toPosition = open && motion == MotionType.Slide ? closedPosition + openOffset : closedPosition;
            Quaternion toRotation = open && motion == MotionType.Rotate ? closedRotation * Quaternion.Euler(openEuler) : closedRotation;

            for (float t = 0f; t < 1f;)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / Mathf.Max(0.01f, duration));
                float eased = Mathf.SmoothStep(0f, 1f, t);
                movingPart.localPosition = Vector3.Lerp(fromPosition, toPosition, eased);
                movingPart.localRotation = Quaternion.Slerp(fromRotation, toRotation, eased);
                yield return null;
            }
            isMoving = false;
        }
    }
}
