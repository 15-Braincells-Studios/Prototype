using System.Collections.Generic;
using UnityEngine;

namespace BlockoutMystery
{
    /// <summary>
    /// Reporting to the butler. Before the antidote is used he repeats his reminder; after, the report table opens
    /// (one suspect, one victim, any number of pieces of evidence from the journal) and submitting it ends the game.
    /// </summary>
    public class ReportSystem : MonoBehaviour
    {
        public static ReportSystem Instance { get; private set; }

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void TalkToButler(Npc butler)
        {
            if (!GameFlow.Instance.AntidoteUsed)
            {
                butler.Say(CaseFile.ButlerReminder);
                return;
            }

            GameFlow.Instance.SetMode(GameMode.Report);
            UIRoot.Instance.Report.Open();
        }

        public void Submit(string suspectId, string victimId, IReadOnlyList<Notebook.Item> evidence)
        {
            UIRoot.Instance.Report.Close();
            GameFlow.Instance.EndGame(suspectId, victimId, evidence);
        }

        public void Cancel()
        {
            UIRoot.Instance.Report.Close();
            if (GameFlow.Instance.Mode == GameMode.Report) GameFlow.Instance.SetMode(GameMode.Explore);
        }
    }
}
