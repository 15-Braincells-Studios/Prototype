using UnityEngine;

namespace PreventTheMurder
{
    /// <summary>A small object inside a container. Examining it discovers its clue and it is taken.</summary>
    public class ClueItem : MonoBehaviour, IInteractable
    {
        [SerializeField] ClueData clue;
        [SerializeField] string verb = "Examine";

        public void SetClue(ClueData assigned) => clue = assigned;

        public string GetPrompt() => clue ? $"[{InputReader.InteractKeyLabel}] {verb} {clue.title}" : null;

        public void Interact(PlayerInteractor player)
        {
            if (!clue) return;
            ClueManager.Instance.Discover(clue, RoomVolume.NameAt(transform.position));
            UIManager.Instance.ShowClueCard(clue);
            gameObject.SetActive(false);
        }
    }
}
