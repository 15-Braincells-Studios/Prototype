using UnityEngine;

namespace AssassinMystery
{
    /// <summary>WASD movement relative to the camera, with Shift to hurry. Turns the body toward the walking direction.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class ThirdPersonController : MonoBehaviour
    {
        [Tooltip("Movement is relative to this camera. Falls back to Camera.main.")]
        [SerializeField] Transform cameraTransform;
        [Tooltip("Visual child that bobs while walking.")]
        [SerializeField] Transform model;
        [SerializeField] float walkSpeed = 3.2f;
        [SerializeField] float runSpeed = 5.6f;
        [SerializeField] float acceleration = 14f;
        [SerializeField] float turnSpeed = 720f;
        [SerializeField] float gravity = -20f;

        CharacterController controller;
        Vector3 velocity;
        float verticalVelocity;
        float bobPhase;
        bool cutsceneMode;

        public Transform Model => model;

        public void Configure(Transform cam, Transform modelRoot)
        {
            cameraTransform = cam;
            model = modelRoot;
        }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
            if (!cameraTransform) Debug.LogError("[AssassinMystery] ThirdPersonController has no camera to move relative to.", this);
        }

        void Update()
        {
            if (cutsceneMode) return;

            bool canMove = GameManager.Instance == null || GameManager.Instance.CanControlPlayer;
            Vector2 input = canMove ? GameInput.Move : Vector2.zero;

            Vector3 forward = cameraTransform ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            float speed = canMove && GameInput.Sprint ? runSpeed : walkSpeed;
            Vector3 desired = (forward * input.y + right * input.x) * speed;

            velocity = Vector3.MoveTowards(velocity, desired, acceleration * Time.deltaTime);

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += gravity * Time.deltaTime;

            controller.Move((velocity + Vector3.up * verticalVelocity) * Time.deltaTime);

            Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
            if (flat.sqrMagnitude > 0.04f)
            {
                var targetRot = Quaternion.LookRotation(flat);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            }

            AnimateWalk(flat.magnitude);
        }

        /// <summary>Simple procedural walk: a bob and slight lean, stronger when running.</summary>
        public void AnimateWalk(float speed)
        {
            if (!model) return;
            float k = Mathf.Clamp01(speed / runSpeed);
            bobPhase += Time.deltaTime * Mathf.Lerp(0f, 14f, k);
            float bob = Mathf.Abs(Mathf.Sin(bobPhase)) * 0.07f * k;
            model.localPosition = new Vector3(0f, bob, 0f);
            model.localRotation = Quaternion.Euler(6f * k, 0f, Mathf.Sin(bobPhase) * 3f * k);
        }

        /// <summary>Hands the transform to a cutscene (collision off) or back to the player.</summary>
        public void SetCutsceneMode(bool on)
        {
            cutsceneMode = on;
            controller.enabled = !on;
            velocity = Vector3.zero;
            if (on && model)
            {
                model.localPosition = Vector3.zero;
                model.localRotation = Quaternion.identity;
            }
        }
    }
}
