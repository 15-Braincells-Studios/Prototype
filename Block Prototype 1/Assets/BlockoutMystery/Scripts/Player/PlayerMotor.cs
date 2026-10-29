using UnityEngine;

namespace BlockoutMystery
{
    /// <summary>WASD walking relative to the camera. The body turns toward the walking direction.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        [Tooltip("Movement is relative to this camera. Falls back to Camera.main.")]
        [SerializeField] Transform cameraTransform;
        [SerializeField] float speed = 4.2f;
        [SerializeField] float acceleration = 20f;
        [SerializeField] float turnSpeed = 720f;
        [SerializeField] float gravity = -20f;

        CharacterController controller;
        FollowCamera follow;
        Vector3 velocity;
        float verticalVelocity;

        public void Configure(Transform cam) => cameraTransform = cam;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
            if (cameraTransform) follow = cameraTransform.GetComponent<FollowCamera>();
        }

        void Update()
        {
            bool canMove = GameFlow.Instance == null || GameFlow.Instance.CanMove;
            Vector2 input = canMove ? GameInput.Move : Vector2.zero;

            Vector3 forward = cameraTransform ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 desired = (forward * input.y + right * input.x) * speed;
            velocity = Vector3.MoveTowards(velocity, desired, acceleration * Time.deltaTime);

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += gravity * Time.deltaTime;
            controller.Move((velocity + Vector3.up * verticalVelocity) * Time.deltaTime);

            if (follow && follow.FirstPerson)
                transform.rotation = Quaternion.Euler(0f, follow.Yaw, 0f); // the body faces where you look
            else if (velocity.sqrMagnitude > 0.04f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(velocity), turnSpeed * Time.deltaTime);
        }
    }
}
