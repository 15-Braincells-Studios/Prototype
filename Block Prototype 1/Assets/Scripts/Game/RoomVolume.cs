using System.Collections.Generic;
using UnityEngine;

namespace PreventTheMurder
{
    /// <summary>
    /// Marks the floor area of a room so clues can record where they were found.
    /// No collider, so it never blocks the interaction raycast. Add one per new room.
    /// </summary>
    public class RoomVolume : MonoBehaviour
    {
        [SerializeField] string roomName = "Room";
        [SerializeField] Vector3 size = new Vector3(8f, 4f, 8f);

        static readonly List<RoomVolume> Rooms = new List<RoomVolume>();

        public string RoomName => roomName;

        void OnEnable() => Rooms.Add(this);
        void OnDisable() => Rooms.Remove(this);

        public bool Contains(Vector3 point)
        {
            Vector3 local = point - transform.position;
            return Mathf.Abs(local.x) <= size.x * 0.5f && Mathf.Abs(local.z) <= size.z * 0.5f;
        }

        public static string NameAt(Vector3 point)
        {
            foreach (var room in Rooms)
                if (room.Contains(point)) return room.roomName;
            return string.Empty;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.6f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * size.y * 0.5f, size);
        }
    }
}
