using UnityEngine;

namespace BlockoutMystery
{
    /// <summary>
    /// Player camera for the third/first person variants. Third person orbits behind the player and pulls in when a
    /// wall is in the way; V switches to first person (eyes in the player's head). It can also be borrowed for a
    /// close-up shot of someone talking, blending in and back out.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class FollowCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector3 pivotOffset = new Vector3(0f, 1.65f, 0f);
        [SerializeField] float distance = 4f;
        [SerializeField] float minDistance = 0.6f;
        [Tooltip("Degrees per pixel of mouse movement.")]
        [SerializeField] float sensitivity = 0.12f;
        [SerializeField] float minPitch = -20f;
        [SerializeField] float maxPitch = 65f;
        [SerializeField] float collisionRadius = 0.25f;
        [Tooltip("What the camera collides with. The player and NPCs are on Ignore Raycast so they are skipped.")]
        [SerializeField] LayerMask collisionMask = ~(1 << 2);
        [Tooltip("Hide the player's body when the camera is pushed closer than this.")]
        [SerializeField] float hideTargetBelow = 1f;

        [Header("First person")]
        [Tooltip("Allow V to switch between third and first person.")]
        [SerializeField] bool allowFirstPerson = true;
        [SerializeField] float eyeHeight = 1.6f;

        [Header("Close-up shots")]
        [Tooltip("How far in front of the speaker's face the camera sits.")]
        [SerializeField] float shotDistance = 1.25f;
        [SerializeField] float shotFieldOfView = 38f;
        [SerializeField] float shotBlendTime = 0.6f;

        Camera cam;
        Renderer[] targetRenderers;
        float yaw;
        float pitch = 15f;
        float currentDistance;
        float baseFieldOfView;
        Transform shotSubject, lastShotSubject;
        float shotBlend;

        public bool FirstPerson { get; private set; }
        public float Yaw => yaw;

        public void Configure(Transform followTarget) => target = followTarget;

        void Start()
        {
            if (!target)
            {
                Debug.LogError("[BlockoutMystery] FollowCamera has no target.", this);
                enabled = false;
                return;
            }
            cam = GetComponent<Camera>();
            baseFieldOfView = cam.fieldOfView;
            targetRenderers = target.GetComponentsInChildren<Renderer>();
            yaw = target.eulerAngles.y;
            currentDistance = distance;
            ThirdPersonPose(out var pos, out var rot);
            transform.SetPositionAndRotation(pos, rot);
        }

        /// <summary>Starts moving in for a close-up of this person's face.</summary>
        public void BeginShot(Transform subject) => shotSubject = lastShotSubject = subject;

        /// <summary>Returns to the player.</summary>
        public void EndShot() => shotSubject = null;

        public void SetFirstPerson(bool on)
        {
            FirstPerson = on && allowFirstPerson;
            if (!FirstPerson) currentDistance = minDistance; // ease back out from the head
            pitch = Mathf.Clamp(pitch, FirstPerson ? -75f : minPitch, FirstPerson ? 75f : maxPitch);
        }

        void LateUpdate()
        {
            if (!target) return;
            var flow = GameFlow.Instance;
            if (flow && flow.Mode == GameMode.Inspect) return; // hold still: the inspected object hangs off the camera

            if (!flow || flow.CanMove)
            {
                if (GameInput.ToggleView) SetFirstPerson(!FirstPerson);
                Vector2 look = GameInput.PointerDelta;
                yaw += look.x * sensitivity;
                pitch = Mathf.Clamp(pitch - look.y * sensitivity, FirstPerson ? -75f : minPitch, FirstPerson ? 75f : maxPitch);
            }

            Vector3 pos;
            Quaternion rot;
            if (FirstPerson) FirstPersonPose(out pos, out rot);
            else ThirdPersonPose(out pos, out rot);

            shotBlend = Mathf.MoveTowards(shotBlend, shotSubject ? 1f : 0f, Time.unscaledDeltaTime / shotBlendTime);
            bool inShot = shotBlend > 0f && lastShotSubject;
            if (inShot)
            {
                ShotPose(lastShotSubject, out var shotPos, out var shotRot);
                float s = Mathf.SmoothStep(0f, 1f, shotBlend);
                pos = Vector3.Lerp(pos, shotPos, s);
                rot = Quaternion.Slerp(rot, shotRot, s);
                cam.fieldOfView = Mathf.Lerp(baseFieldOfView, shotFieldOfView, s);
            }
            else cam.fieldOfView = baseFieldOfView;

            transform.SetPositionAndRotation(pos, rot);
            // Hide the player while the close-up is (nearly) in place, so they can't block the face; show them again
            // as soon as the camera starts pulling back so they are already there when it arrives.
            bool hiddenByShot = inShot && (shotSubject ? shotBlend > 0.3f : shotBlend > 0.85f);
            SetTargetVisible(!FirstPerson && !hiddenByShot && currentDistance >= hideTargetBelow);
        }

        void ThirdPersonPose(out Vector3 pos, out Quaternion rot)
        {
            rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pivot = target.position + pivotOffset;
            Vector3 back = -(rot * Vector3.forward);

            float wanted = distance;
            if (Physics.SphereCast(pivot, collisionRadius, back, out var hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
                wanted = Mathf.Max(minDistance, hit.distance - 0.05f);

            // Pull in instantly, ease back out.
            currentDistance = wanted < currentDistance
                ? wanted
                : Mathf.Lerp(currentDistance, wanted, 1f - Mathf.Exp(-6f * Time.deltaTime));
            pos = pivot + back * currentDistance;
        }

        void FirstPersonPose(out Vector3 pos, out Quaternion rot)
        {
            rot = Quaternion.Euler(pitch, yaw, 0f);
            pos = target.position + Vector3.up * eyeHeight + Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * 0.1f;
        }

        void ShotPose(Transform subject, out Vector3 pos, out Quaternion rot)
        {
            Vector3 face = subject.position + Vector3.up * 1.6f;
            pos = face + subject.forward * shotDistance + subject.right * 0.28f + Vector3.up * 0.04f;
            rot = Quaternion.LookRotation(face + Vector3.up * 0.02f - pos);
        }

        void SetTargetVisible(bool show)
        {
            if (targetRenderers == null || targetRenderers.Length == 0 || targetRenderers[0].enabled == show) return;
            foreach (var r in targetRenderers) r.enabled = show;
        }
    }
}
