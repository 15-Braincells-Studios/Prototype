using UnityEngine;

namespace PreventTheMurder
{
    /// <summary>Keeps world-space text facing the camera.</summary>
    public class Billboard : MonoBehaviour
    {
        Transform cameraTransform;

        void LateUpdate()
        {
            if (!cameraTransform)
            {
                var cam = Camera.main;
                if (!cam) return;
                cameraTransform = cam.transform;
            }
            transform.rotation = Quaternion.LookRotation(transform.position - cameraTransform.position, Vector3.up);
        }
    }
}
