using UnityEngine;

namespace BlockoutMystery
{
    /// <summary>An object that E picks up into the inspect view (statuettes, plates).</summary>
    public abstract class Inspectable : Interactable
    {
        [SerializeField] string displayName = "Object";
        [Tooltip("Rotation (relative to the camera) the object starts at in the inspect view.")]
        [SerializeField] Vector3 inspectEuler = new Vector3(-15f, 20f, 0f);
        [Tooltip("Metres in front of the camera.")]
        [SerializeField] float inspectDistance = 0.65f;

        public string DisplayName => displayName;
        public Vector3 InspectEuler => inspectEuler;
        public float InspectDistance => inspectDistance;

        public override string Prompt => "Inspect " + displayName.ToLowerInvariant();
        public override void Interact() => InspectSystem.Instance.Begin(this);

        public virtual string InspectHint => "Hold the mouse and drag to rotate   |   E / Esc  put it back";

        public virtual void OnInspectBegin() { }
        public virtual void OnInspectEnd() { }

        public void ConfigureInspect(string name, Vector3 euler, float distance)
        {
            displayName = name;
            inspectEuler = euler;
            inspectDistance = distance;
        }
    }
}
