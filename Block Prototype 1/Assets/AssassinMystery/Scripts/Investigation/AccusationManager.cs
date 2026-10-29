using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AssassinMystery
{
    /// <summary>
    /// Judges an accusation: the right suspect AND enough key evidence presented. Any failed case gets the same
    /// answer, so a failure never tells the player whether they had the right person.
    /// </summary>
    public class AccusationManager : MonoBehaviour
    {
        public static AccusationManager Instance { get; private set; }

        [Tooltip("Seconds lost after a failed accusation.")]
        [SerializeField] float failedAccusationPenalty = 60f;
        [Tooltip("This many failed accusations and the assassin strikes at once.")]
        [SerializeField] int maxFailedAccusations = 3;
        [Tooltip("How many of the mystery's key evidence clues must be presented to convict.")]
        [SerializeField] int requiredKeyEvidence = 3;

        public int FailedAccusations { get; private set; }
        public int AccusationsLeft => maxFailedAccusations - FailedAccusations;
        public float Penalty => failedAccusationPenalty;
        public int RequiredKeyEvidence => requiredKeyEvidence;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Accuse(SuspectData suspect, IList<ClueData> presented)
        {
            var gm = GameManager.Instance;
            if (!suspect || gm == null || gm.State != GameState.Investigating) return;
            var mystery = gm.Mystery;

            var proving = presented.Where(c => mystery.keyEvidence.Contains(c)).ToList();
            if (suspect == mystery.assassin && proving.Count >= requiredKeyEvidence)
            {
                gm.Win(proving);
                return;
            }

            FailedAccusations++;
            UIManager.Instance.CloseAllPanels();
            if (FailedAccusations >= maxFailedAccusations)
            {
                gm.Lose(LossReason.TooManyAccusations);
                return;
            }

            GameTimer.Instance.Penalize(failedAccusationPenalty);
            UIManager.Instance.ShowBanner("The accusation fails",
                $"Captain Hale is not convinced by your case against {suspect.displayName}. " +
                $"Calming the uproar costs {failedAccusationPenalty:0} seconds, and the real assassin grows bolder. " +
                $"({AccusationsLeft} accusation{(AccusationsLeft == 1 ? "" : "s")} left)");
        }
    }
}
