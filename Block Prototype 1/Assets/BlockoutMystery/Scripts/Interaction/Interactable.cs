using System.Collections.Generic;
using UnityEngine;

namespace BlockoutMystery
{
    /// <summary>Anything the player can press E on. Registers itself so the <see cref="PlayerInteractor"/> can pick the best one.</summary>
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        [Tooltip("Where distance and aim are measured to. Falls back to this transform.")]
        [SerializeField] Transform point;
        [Tooltip("How close (metres, from the player's chest) the player has to be.")]
        [SerializeField] float range = 2.4f;

        public Vector3 Point => point ? point.position : transform.position;
        public float Range => range;

        /// <summary>False hides the prompt (an open door, a closed drawer's contents, ...).</summary>
        public virtual bool Available => true;
        public abstract string Prompt { get; }
        public abstract void Interact();

        public void ConfigureInteraction(Transform interactionPoint, float interactionRange)
        {
            point = interactionPoint;
            range = interactionRange;
        }

        protected virtual void Awake() => EnsureCollider();

        /// <summary>
        /// The cursor finds interactables with a raycast, so each one needs a collider. Ones built without any (the
        /// book, the antidote) get a trigger box around their meshes, which nothing else collides with.
        /// </summary>
        void EnsureCollider()
        {
            if (GetComponentInChildren<Collider>(true)) return;
            var renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            var local = new Bounds(transform.InverseTransformPoint(renderers[0].bounds.center), Vector3.zero);
            foreach (var r in renderers)
            {
                var b = r.bounds;
                for (int c = 0; c < 8; c++)
                    local.Encapsulate(transform.InverseTransformPoint(new Vector3(
                        c % 2 == 0 ? b.min.x : b.max.x, (c / 2) % 2 == 0 ? b.min.y : b.max.y, c / 4 == 0 ? b.min.z : b.max.z)));
            }
            var box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = local.center;
            box.size = local.size;
        }

        protected virtual void OnEnable() => All.Add(this);
        protected virtual void OnDisable() => All.Remove(this);
    }

    /// <summary>Something on an inspected object that reacts to a click (the hidden key).</summary>
    public interface IInspectClickable
    {
        void OnInspectClick();
    }
}
