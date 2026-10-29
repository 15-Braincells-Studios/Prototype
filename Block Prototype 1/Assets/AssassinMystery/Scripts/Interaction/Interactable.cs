using System.Collections.Generic;
using UnityEngine;

namespace AssassinMystery
{
    /// <summary>Anything the player can use with E. Registers itself so the interactor needs no physics queries.</summary>
    public abstract class Interactable : MonoBehaviour
    {
        static readonly List<Interactable> all = new List<Interactable>();
        public static IReadOnlyList<Interactable> All => all;

        [Tooltip("Where the player must stand near. Defaults to this transform.")]
        [SerializeField] protected Transform interactionPoint;
        [Tooltip("Added to the player's interaction range, for large furniture.")]
        [SerializeField] protected float extraRange;
        [Tooltip("Height of the on-screen prompt above the interaction point.")]
        [SerializeField] protected float promptHeight = 1.2f;

        public abstract string Prompt { get; }
        public virtual bool CanInteract => true;
        public float ExtraRange => extraRange;
        public Vector3 InteractionPoint => interactionPoint ? interactionPoint.position : transform.position;
        public Vector3 PromptPosition => InteractionPoint + Vector3.up * promptHeight;

        public abstract void Interact();

        protected virtual void OnEnable() => all.Add(this);
        protected virtual void OnDisable() => all.Remove(this);
    }
}
