using UnityEngine;

namespace BlockoutMystery
{
    /// <summary>
    /// Picks the interactable under the cursor dot (within reach of the player), shows its prompt and handles E. With a
    /// free camera the dot sits in the centre of the screen; in the isometric variant it follows the mouse.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        public static PlayerInteractor Instance { get; private set; }

        [SerializeField] Transform viewCamera;
        [Tooltip("How far the cursor ray reaches from the camera (metres). Reach itself is each interactable's Range.")]
        [SerializeField] float maxRayDistance = 12f;
        [Tooltip("Isometric variant: the camera hangs far above the level, so its ray has to be much longer.")]
        [SerializeField] float isoRayDistance = 100f;
        [Tooltip("Height of the point reach is measured from.")]
        [SerializeField] float chestHeight = 1.1f;

        readonly RaycastHit[] hits = new RaycastHit[32];
        Camera cam;

        public Interactable Focus { get; private set; }

        public void Configure(Transform camTransform) => viewCamera = camTransform;

        void Awake()
        {
            Instance = this;
            if (!viewCamera && Camera.main) viewCamera = Camera.main.transform;
            if (viewCamera) cam = viewCamera.GetComponent<Camera>();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Where the cursor dot is on screen.</summary>
        public static Vector2 CursorPosition => PrototypeVariant.Isometric
            ? GameInput.PointerPosition
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        void Update()
        {
            var flow = GameFlow.Instance;
            var dialogue = DialogueSystem.Instance;
            bool active = flow && flow.CanMove && !flow.ChangedThisFrame;
            Focus = active ? FindUnderCursor() : null;
            UIRoot.Instance.Hud.SetCursor(flow && flow.CanMove, Focus, CursorPosition);

            // While someone's line is up, E continues it, so don't offer to talk to them again.
            bool listening = dialogue && dialogue.IsFreeLine && Focus && Focus == dialogue.Speaker;
            UIRoot.Instance.Hud.SetPrompt(Focus && !listening ? Focus.Prompt : null);

            if (Focus && GameInput.Interact && !(dialogue && dialogue.ConsumedInteractThisFrame)) Focus.Interact();
        }

        /// <summary>
        /// The first thing the cursor ray hits decides: walls block, the player's own body is skipped. Of the
        /// interactables on that object and its parents, the closest one that is available wins (the antidote over the
        /// drawer it lies in), as long as the player is close enough.
        /// </summary>
        Interactable FindUnderCursor()
        {
            if (!cam) return null;
            var ray = cam.ScreenPointToRay(CursorPosition);
            float length = PrototypeVariant.Isometric ? isoRayDistance : maxRayDistance;
            int count = Physics.RaycastNonAlloc(ray, hits, length, ~0, QueryTriggerInteraction.Collide);

            RaycastHit? first = null;
            for (int n = 0; n < count; n++)
            {
                var h = hits[n];
                if (h.collider.transform.IsChildOf(transform)) continue; // the player
                if (h.collider.isTrigger && !h.collider.GetComponentInParent<Interactable>()) continue; // volumes
                if (!first.HasValue || h.distance < first.Value.distance) first = h;
            }
            if (!first.HasValue) return null;

            Vector3 chest = transform.position + Vector3.up * chestHeight;
            Vector3 point = first.Value.point;
            foreach (var i in first.Value.collider.GetComponentsInParent<Interactable>())
            {
                if (!i.isActiveAndEnabled || !i.Available) continue;
                float reach = Mathf.Min(Vector3.Distance(chest, point), Vector3.Distance(chest, i.Point));
                return reach <= i.Range ? i : null;
            }
            return null;
        }
    }
}
