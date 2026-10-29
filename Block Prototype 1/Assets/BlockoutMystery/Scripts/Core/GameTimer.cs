using UnityEngine;

namespace BlockoutMystery
{
    /// <summary>Counts up from the moment the butler's intro ends until the player reports. There is no time limit.</summary>
    [DefaultExecutionOrder(-90)]
    public class GameTimer : MonoBehaviour
    {
        public static GameTimer Instance { get; private set; }

        float startTime;
        float stoppedElapsed;

        public bool Started { get; private set; }
        public bool Running { get; private set; }
        public float Elapsed => !Started ? 0f : Running ? Time.time - startTime : stoppedElapsed;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Begin()
        {
            startTime = Time.time;
            Started = Running = true;
        }

        /// <summary>Stops the clock and returns the time taken.</summary>
        public float Stop()
        {
            if (Running)
            {
                stoppedElapsed = Time.time - startTime;
                Running = false;
            }
            return Elapsed;
        }

        public static string Format(float seconds)
        {
            int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{s / 60:00}:{s % 60:00}";
        }
    }
}
