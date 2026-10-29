using TMPro;
using UnityEngine;

namespace PreventTheMurder
{
    public enum ReportResult { Incomplete, Correct, PartlyCorrect, Wrong }

    /// <summary>
    /// Where the player names victim, culprit and method. Kept apart from NPC and dialogue code so it can be
    /// moved, duplicated or replaced (a bell, a messenger, a different guard).
    /// </summary>
    public class ReportPoint : MonoBehaviour, IInteractable
    {
        [Tooltip("Character id of whoever takes the report (name shown in the prompt).")]
        [SerializeField] string reporterId = "captain_rowan";
        [SerializeField] string fallbackName = "the Guard";
        [SerializeField] TMP_Text nameLabel;

        [Header("Penalties (seconds taken off the clock)")]
        [SerializeField] float partialPenaltySeconds = 120f;
        [SerializeField] float wrongPenaltySeconds = 120f;

        CharacterData reporter;

        public string ReporterId => reporterId;
        public string ReporterName => reporter ? reporter.displayName : fallbackName;

        public void Bind(CharacterData character)
        {
            reporter = character;
            if (nameLabel && character) nameLabel.text = character.displayName;
        }

        public string GetPrompt() => $"[{InputReader.InteractKeyLabel}] Report to {ReporterName}";

        public void Interact(PlayerInteractor player) => UIManager.Instance.OpenReport(this);

        public ReportResult Submit(CharacterData victim, CharacterData culprit, int methodIndex, out float penalty)
        {
            penalty = 0f;
            if (!victim || !culprit || methodIndex < 0) return ReportResult.Incomplete;

            var gm = GameManager.Instance;
            var scenario = gm.Scenario;
            bool culpritRight = culprit == scenario.culprit;

            if (culpritRight && victim == scenario.victim && methodIndex == scenario.correctMethodIndex)
            {
                gm.Win();
                return ReportResult.Correct;
            }

            penalty = culpritRight ? partialPenaltySeconds : wrongPenaltySeconds;
            gm.ApplyPenalty(penalty);
            return culpritRight ? ReportResult.PartlyCorrect : ReportResult.Wrong;
        }
    }
}
