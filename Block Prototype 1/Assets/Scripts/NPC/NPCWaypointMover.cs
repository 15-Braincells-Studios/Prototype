using System.Collections.Generic;
using UnityEngine;

namespace PreventTheMurder
{
    /// <summary>
    /// Walks an NPC along hand-placed waypoints, pausing at each stop. Routes are drawn so straight
    /// lines between points never leave the rooms or clip furniture.
    /// Extension point: swap the waypoint list per time of day for NPC schedules.
    /// </summary>
    public class NPCWaypointMover : MonoBehaviour
    {
        [SerializeField] List<Waypoint> waypoints = new List<Waypoint>();
        [Tooltip("Walk the route back and forth instead of looping from the last point to the first.")]
        [SerializeField] bool pingPong;
        [SerializeField] float speed = 1.3f;
        [SerializeField] Vector2 pauseSeconds = new Vector2(2f, 6f);
        [SerializeField] float turnSpeed = 300f;

        int index;
        int direction = 1;
        float waitTimer;
        bool held;

        /// <summary>Stops walking (e.g. during a conversation) without losing the route position.</summary>
        public void SetHeld(bool value) => held = value;

        void Start()
        {
            waitTimer = Random.Range(pauseSeconds.x, pauseSeconds.y);
        }

        void Update()
        {
            if (held || waypoints.Count < 2) return;

            if (waitTimer > 0f)
            {
                waitTimer -= Time.deltaTime;
                return;
            }

            Vector3 target = waypoints[index].transform.position;
            target.y = transform.position.y;
            Vector3 toTarget = target - transform.position;

            if (toTarget.sqrMagnitude < 0.0025f)
            {
                if (waypoints[index].pauseHere) waitTimer = Random.Range(pauseSeconds.x, pauseSeconds.y);
                AdvanceIndex();
                return;
            }

            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            Quaternion facing = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, facing, turnSpeed * Time.deltaTime);
        }

        void AdvanceIndex()
        {
            if (pingPong)
            {
                if (index + direction < 0 || index + direction >= waypoints.Count) direction = -direction;
                index += direction;
            }
            else
            {
                index = (index + 1) % waypoints.Count;
            }
        }
    }
}
