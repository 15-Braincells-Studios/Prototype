using System;
using System.Collections.Generic;
using UnityEngine;

namespace PreventTheMurder
{
    /// <summary>A clue as found in this run. Kept separate from ClueData so play never writes to assets.</summary>
    public class DiscoveredClue
    {
        public ClueData clue;
        public string room;
        public float timeRemainingWhenFound;
    }

    /// <summary>
    /// Holds the clues found this run, in order, without duplicates. Listeners (toast, notepad, future
    /// suspect board) react to <see cref="OnClueDiscovered"/> instead of being called directly.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class ClueManager : MonoBehaviour
    {
        public static ClueManager Instance { get; private set; }

        public event Action<DiscoveredClue> OnClueDiscovered;

        readonly List<DiscoveredClue> discovered = new List<DiscoveredClue>();
        readonly HashSet<ClueData> discoveredSet = new HashSet<ClueData>();

        public IReadOnlyList<DiscoveredClue> Discovered => discovered;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool HasClue(ClueData clue) => clue != null && discoveredSet.Contains(clue);

        /// <returns>True if this was a new clue.</returns>
        public bool Discover(ClueData clue, string detectedRoom)
        {
            if (clue == null || !discoveredSet.Add(clue)) return false;

            var entry = new DiscoveredClue
            {
                clue = clue,
                room = string.IsNullOrEmpty(clue.foundInRoom) ? detectedRoom : clue.foundInRoom,
                timeRemainingWhenFound = GameManager.Instance ? GameManager.Instance.TimeRemaining : 0f,
            };
            discovered.Add(entry);
            OnClueDiscovered?.Invoke(entry);
            return true;
        }
    }
}
