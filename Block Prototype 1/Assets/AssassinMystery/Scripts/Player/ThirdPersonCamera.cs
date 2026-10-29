using UnityEngine;

namespace AssassinMystery
{
    /// <summary>Orbit camera behind the player: mouse to look, smooth follow, pulls in when a wall is in the way.</summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector3 pivotOffset = new Vector3(0f, 1.75f, 0f);
        [Tooltip("Sideways offset so the detective sits left of centre (over-the-shoulder).")]
        [SerializeField] float shoulderOffset = 0.55f;
        [SerializeField] float distance = 3.4f;
        [SerializeField] float minDistance = 0.7f;
        [Tooltip("Degrees per pixel of mouse movement.")]
        [SerializeField] float sensitivity = 0.12f;
        [SerializeField] float minPitch = -25f;
        [SerializeField] float maxPitch = 60f;
        [SerializeField] float followSharpness = 14f;
        [SerializeField] float collisionRadius = 0.25f;
        [Tooltip("What the camera collides with. The player and NPCs sit on Ignore Raycast so they are skipped.")]
        [SerializeField] LayerMask collisionMask = ~(1 << 2);
        [Tooltip("Hide the player's model when the camera is pushed closer than this.")]
        [SerializeField] float hideTargetBelow = 1.1f;

        Renderer[] targetRenderers;

        float yaw;
        float pitch = 16f;
        float currentDistance;
        Vector3 pivot;

        public void Configure(Transform followTarget) => target = followTarget;

        void Start()
        {
            if (!target)
            {
                Debug.LogError("[AssassinMystery] ThirdPersonCamera has no target.", this);
                enabled = false;
                return;
            }
            targetRenderers = target.GetComponentsInChildren<Renderer>();
            SnapBehindTarget();
        }

        void OnDisable()
        {
            // Cutscenes take the camera over; make sure the detective is visible in them.
            if (targetRenderers != null)
                foreach (var r in targetRenderers) if (r) r.enabled = true;
        }

        public void SnapBehindTarget()
        {
            yaw = target.eulerAngles.y;
            pitch = 16f;
            pivot = target.position + pivotOffset;
            currentDistance = distance;
            Apply();
        }

        void LateUpdate()
        {
            if (!target) return;

            if (GameManager.Instance == null || GameManager.Instance.CanControlPlayer)
            {
                Vector2 look = GameInput.Look;
                yaw += look.x * sensitivity;
                pitch = Mathf.Clamp(pitch - look.y * sensitivity, minPitch, maxPitch);
            }

            Vector3 desiredPivot = target.position + pivotOffset;
            pivot = Vector3.Lerp(pivot, desiredPivot, 1f - Mathf.Exp(-followSharpness * Time.unscaledDeltaTime));
            Apply();
        }

        void Apply()
        {
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 back = -(rot * Vector3.forward);

            // Shift the orbit centre to the shoulder, but not through a wall.
            Vector3 side = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            float shoulder = shoulderOffset;
            if (Physics.SphereCast(pivot, collisionRadius, side, out var sideHit, shoulderOffset, collisionMask, QueryTriggerInteraction.Ignore))
                shoulder = Mathf.Max(0f, sideHit.distance - 0.05f);
            Vector3 origin = pivot + side * shoulder;

            float wanted = distance;
            if (Physics.SphereCast(origin, collisionRadius, back, out var hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
                wanted = Mathf.Max(minDistance, hit.distance - 0.05f);

            // Pull in instantly, ease back out.
            currentDistance = wanted < currentDistance
                ? wanted
                : Mathf.Lerp(currentDistance, wanted, 1f - Mathf.Exp(-6f * Time.unscaledDeltaTime));

            transform.SetPositionAndRotation(origin + back * currentDistance, rot);

            bool show = currentDistance >= hideTargetBelow;
            if (targetRenderers != null && targetRenderers.Length > 0 && targetRenderers[0].enabled != show)
                foreach (var r in targetRenderers) r.enabled = show;
        }
    }
}
