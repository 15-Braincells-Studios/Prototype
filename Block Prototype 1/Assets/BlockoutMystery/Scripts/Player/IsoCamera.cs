using UnityEngine;

namespace BlockoutMystery
{
    /// <summary>Top-down isometric camera: orthographic, fixed angle, follows the player. The mouse wheel zooms.</summary>
    [RequireComponent(typeof(Camera))]
    public class IsoCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] float yaw = 45f;
        [SerializeField] float pitch = 35f;
        [Tooltip("How far back the camera sits. With an orthographic camera this only has to clear the level.")]
        [SerializeField] float distance = 40f;
        [Tooltip("Orthographic size: half the visible height in metres.")]
        [SerializeField] float size = 6.5f;
        [SerializeField] float minSize = 3.5f;
        [SerializeField] float maxSize = 12f;
        [SerializeField] float zoomStep = 0.8f;
        [SerializeField] float followSharpness = 8f;
        [SerializeField] Vector3 focusOffset = new Vector3(0f, 1f, 0f);

        Camera cam;
        Vector3 focus;
        float targetSize;

        public void Configure(Transform followTarget) => target = followTarget;

        void Start()
        {
            cam = GetComponent<Camera>();
            if (!target)
            {
                Debug.LogError("[BlockoutMystery] IsoCamera has no target.", this);
                enabled = false;
                return;
            }
            cam.orthographic = true;
            cam.orthographicSize = targetSize = size;
            focus = target.position + focusOffset;
            Apply();
        }

        void LateUpdate()
        {
            if (!target) return;
            var flow = GameFlow.Instance;
            if (flow && flow.Mode == GameMode.Inspect) return; // the inspect view borrows the camera

            float scroll = GameInput.Scroll;
            if ((!flow || flow.CanMove) && Mathf.Abs(scroll) > 0.01f)
                targetSize = Mathf.Clamp(targetSize - Mathf.Sign(scroll) * zoomStep, minSize, maxSize);
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, 1f - Mathf.Exp(-10f * Time.deltaTime));

            focus = Vector3.Lerp(focus, target.position + focusOffset, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));
            Apply();
        }

        void Apply()
        {
            var rot = Quaternion.Euler(pitch, yaw, 0f);
            transform.SetPositionAndRotation(focus - rot * Vector3.forward * distance, rot);
        }
    }
}
