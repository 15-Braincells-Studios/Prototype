using System.Collections;
using UnityEngine;

namespace BlockoutMystery
{
    /// <summary>The locked closet door in the kitchen. Opens only with the key, which is used up.</summary>
    public class ClosetDoor : Interactable
    {
        [Tooltip("The door swings around this transform's Y axis.")]
        [SerializeField] Transform hinge;
        [SerializeField] float openYaw = 100f;
        [SerializeField] float openTime = 0.9f;

        public bool IsOpen { get; private set; }
        public override bool Available => !IsOpen;
        public override string Prompt => "Open the closet door";

        public void Configure(Transform doorHinge) => hinge = doorHinge;

        public override void Interact()
        {
            if (!Inventory.Instance.Remove(ItemId.Key))
            {
                UIRoot.Toast("Locked");
                return;
            }

            IsOpen = true;
            UIRoot.Toast("The key turns. The closet is open (the key stays in the lock).");
            StartCoroutine(Swing());
        }

        IEnumerator Swing()
        {
            Quaternion from = hinge.localRotation, to = from * Quaternion.Euler(0f, openYaw, 0f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / openTime)
            {
                hinge.localRotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            hinge.localRotation = to;
        }
    }
}
