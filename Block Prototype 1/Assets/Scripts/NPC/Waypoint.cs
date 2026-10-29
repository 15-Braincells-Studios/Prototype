using UnityEngine;

namespace PreventTheMurder
{
    /// <summary>A stop on an NPC route. Turn off pauseHere for pass-through points such as archways.</summary>
    public class Waypoint : MonoBehaviour
    {
        public bool pauseHere = true;

        void OnDrawGizmos()
        {
            Gizmos.color = pauseHere ? new Color(0.2f, 0.8f, 1f) : new Color(0.6f, 0.6f, 0.6f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.05f, 0.2f);
        }
    }
}
