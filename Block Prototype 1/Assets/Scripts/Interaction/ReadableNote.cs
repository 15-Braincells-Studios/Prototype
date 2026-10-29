using UnityEngine;

namespace PreventTheMurder
{
    /// <summary>A note lying in the open. Reading it discovers its clue; it stays so it can be re-read.</summary>
    public class ReadableNote : MonoBehaviour, IInteractable, IClueSpot
    {
        [SerializeField] string spotId;
        [SerializeField] string displayName = "note";

        ClueData clue;

        public string SpotId => spotId;

        public void AssignClue(ClueData assigned)
        {
            clue = assigned;
            gameObject.SetActive(assigned != null);
        }

        public string GetPrompt() => clue ? $"[{InputReader.InteractKeyLabel}] Read {displayName}" : null;

        public void Interact(PlayerInteractor player)
        {
            if (!clue) return;
            ClueManager.Instance.Discover(clue, RoomVolume.NameAt(transform.position));
            UIManager.Instance.ShowClueCard(clue);
        }
    }
}
