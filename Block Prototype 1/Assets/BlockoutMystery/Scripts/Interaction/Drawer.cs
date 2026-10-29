using System.Collections;
using UnityEngine;

namespace BlockoutMystery
{
    /// <summary>The top drawer of the chest in the closet. Slides open and closed; holds the green herbs and the antidote.</summary>
    public class Drawer : Interactable
    {
        [Tooltip("The part that slides out.")]
        [SerializeField] Transform tray;
        [Tooltip("Slide distance in the tray's parent space.")]
        [SerializeField] Vector3 openOffset = new Vector3(0f, 0f, 0.42f);
        [SerializeField] float openTime = 0.5f;

        Vector3 closedPosition;
        Coroutine slide;

        public bool IsOpen { get; private set; }
        public override string Prompt => IsOpen ? "Close the drawer" : "Open the drawer";

        public void Configure(Transform slidingTray) => tray = slidingTray;

        protected override void Awake()
        {
            base.Awake();
            if (tray) closedPosition = tray.localPosition;
        }

        public override void Interact()
        {
            IsOpen = !IsOpen;
            if (IsOpen) Notebook.Instance.RecordEvidence(CaseFile.DrawerHerbsEvidence);
            if (slide != null) StopCoroutine(slide);
            slide = StartCoroutine(Slide(IsOpen ? closedPosition + openOffset : closedPosition));
        }

        IEnumerator Slide(Vector3 to)
        {
            Vector3 from = tray.localPosition;
            float time = openTime * Vector3.Distance(from, to) / Mathf.Max(openOffset.magnitude, 0.001f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(time, 0.001f))
            {
                tray.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            tray.localPosition = to;
            slide = null;
        }
    }
}
