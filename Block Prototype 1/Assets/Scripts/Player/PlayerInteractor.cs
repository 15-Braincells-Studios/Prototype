using UnityEngine;

namespace PreventTheMurder
{
    /// <summary>Raycasts from the camera, shows the prompt for what the player looks at and fires it on E.</summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] Camera viewCamera;
        [SerializeField] float range = 2.5f;
        [Tooltip("The player sits on Ignore Raycast so the ray never hits its own capsule.")]
        [SerializeField] LayerMask interactMask = Physics.DefaultRaycastLayers;

        void Update()
        {
            IInteractable target = FindTarget();
            string prompt = target?.GetPrompt();

            if (UIManager.Instance) UIManager.Instance.SetPrompt(prompt);

            if (!string.IsNullOrEmpty(prompt) && InputReader.TryConsumeInteract())
                target.Interact(this);
        }

        IInteractable FindTarget()
        {
            Transform view = viewCamera.transform;
            if (!Physics.Raycast(view.position, view.forward, out RaycastHit hit, range, interactMask, QueryTriggerInteraction.Ignore))
                return null;
            return hit.collider.GetComponentInParent<IInteractable>();
        }

        void OnDisable()
        {
            if (UIManager.Instance) UIManager.Instance.SetPrompt(null);
        }
    }
}
