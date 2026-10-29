using UnityEngine;

namespace BlockoutMystery
{
    /// <summary>Keeps a name tag turned toward the camera.</summary>
    public class Billboard : MonoBehaviour
    {
        Transform cam;

        void LateUpdate()
        {
            if (!cam)
            {
                if (!Camera.main) return;
                cam = Camera.main.transform;
            }
            transform.rotation = cam.rotation;
        }
    }
}
