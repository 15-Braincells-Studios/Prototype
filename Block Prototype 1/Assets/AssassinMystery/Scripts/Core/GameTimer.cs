using System;
using UnityEngine;

namespace AssassinMystery
{
    /// <summary>
    /// The countdown to the assassination. Runs on scaled time, so it stops while the game is paused
    /// (timeScale 0) but keeps running while the player talks, searches or reads the notebook.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class GameTimer : MonoBehaviour
    {
        public static GameTimer Instance { get; private set; }

        [Tooltip("Above 0 overrides the mystery's duration. Handy for testing the failure ending.")]
        [SerializeField] float durationOverride;
        [Tooltip("Below this many seconds the timer turns amber.")]
        [SerializeField] float cautionThreshold = 180f;
        [Tooltip("Below this many seconds the timer turns red and pulses.")]
        [SerializeField] float warningThreshold = 60f;

        public float Remaining { get; private set; }
        public float Duration { get; private set; }
        public bool IsRunning { get; private set; }
        public float CautionThreshold => cautionThreshold;
        public float WarningThreshold => warningThreshold;

        public event Action<float> OnTick;
        public event Action OnExpired;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void StartTimer(float mysteryDuration)
        {
            Duration = durationOverride > 0f ? durationOverride : mysteryDuration;
            Remaining = Duration;
            IsRunning = true;
            OnTick?.Invoke(Remaining);
        }

        public void Stop() => IsRunning = false;

        /// <summary>Removes time, e.g. after a failed accusation. Can end the game.</summary>
        public void Penalize(float seconds)
        {
            if (!IsRunning) return;
            Remaining = Mathf.Max(0f, Remaining - seconds);
            OnTick?.Invoke(Remaining);
            if (Remaining <= 0f) Expire();
        }

        void Update()
        {
            if (!IsRunning) return;
            Remaining -= Time.deltaTime;
            if (Remaining <= 0f)
            {
                Remaining = 0f;
                OnTick?.Invoke(Remaining);
                Expire();
                return;
            }
            OnTick?.Invoke(Remaining);
        }

        void Expire()
        {
            IsRunning = false;
            OnExpired?.Invoke();
        }

        public static string Format(float seconds)
        {
            int total = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
