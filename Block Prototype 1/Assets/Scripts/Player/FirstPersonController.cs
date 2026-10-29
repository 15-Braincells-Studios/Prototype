using UnityEngine;

namespace PreventTheMurder
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [SerializeField] Transform cameraPivot;
        [SerializeField] float moveSpeed = 4f;
        [SerializeField] float lookSensitivity = 0.12f;
        [SerializeField] float gravity = -20f;
        [SerializeField] float maxPitch = 85f;

        CharacterController controller;
        float pitch;
        float verticalVelocity;

        /// <summary>Turned off by UIManager while a panel is open.</summary>
        public bool InputEnabled { get; set; } = true;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        void Update()
        {
            if (InputEnabled)
            {
                Vector2 look = InputReader.LookDelta * lookSensitivity;
                transform.Rotate(0f, look.x, 0f);
                pitch = Mathf.Clamp(pitch - look.y, -maxPitch, maxPitch);
                cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
            }

            Vector2 input = InputEnabled ? InputReader.Move : Vector2.zero;
            Vector3 velocity = (transform.right * input.x + transform.forward * input.y) * moveSpeed;

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += gravity * Time.deltaTime;
            velocity.y = verticalVelocity;

            controller.Move(velocity * Time.deltaTime);
        }
    }
}
