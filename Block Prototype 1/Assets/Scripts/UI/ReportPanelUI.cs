using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PreventTheMurder
{
    /// <summary>Victim / Culprit / Method dropdowns. Only collects the answer; ReportPoint judges it.</summary>
    public class ReportPanelUI : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] TMP_Text headerText;
        [SerializeField] TMP_Dropdown victimDropdown;
        [SerializeField] TMP_Dropdown culpritDropdown;
        [SerializeField] TMP_Dropdown methodDropdown;
        [SerializeField] TMP_Text messageText;
        [SerializeField] Button reportButton;
        [SerializeField] Button cancelButton;

        [Header("Text")]
        [SerializeField] string placeholderOption = "Choose...";
        [SerializeField] string incompleteMessage = "Choose a victim, a culprit and a method.";
        [SerializeField] string penaltyFormat = "<color=#FF6B5B>-{0} from the clock.</color>";

        ReportPoint point;
        List<CharacterData> suspects = new List<CharacterData>();

        public bool IsOpen => root.activeSelf;

        void Awake()
        {
            root.SetActive(false);
            reportButton.onClick.AddListener(Submit);
            cancelButton.onClick.AddListener(Close);
        }

        public void Open(ReportPoint reportPoint)
        {
            if (IsOpen) return;
            point = reportPoint;

            var scenario = GameManager.Instance.Scenario;
            suspects = scenario.Suspects.ToList();
            var names = suspects.Select(s => s.displayName).ToList();
            Fill(victimDropdown, names);
            Fill(culpritDropdown, names);
            Fill(methodDropdown, scenario.methodOptions);

            headerText.text = $"<b>{point.ReporterName}:</b> {scenario.reportPrompt}";
            messageText.text = string.Empty;
            root.SetActive(true);
            UIManager.Instance.PushModal();
        }

        public void Close()
        {
            if (!IsOpen) return;
            root.SetActive(false);
            UIManager.Instance.PopModal();
        }

        /// <summary>Keeps the previous choice when the panel is reopened.</summary>
        void Fill(TMP_Dropdown dropdown, List<string> options)
        {
            int previous = dropdown.value;
            dropdown.ClearOptions();
            var all = new List<string> { placeholderOption };
            all.AddRange(options);
            dropdown.AddOptions(all);
            dropdown.SetValueWithoutNotify(previous < all.Count ? previous : 0);
            dropdown.RefreshShownValue();
        }

        CharacterData Pick(TMP_Dropdown dropdown) => dropdown.value > 0 ? suspects[dropdown.value - 1] : null;

        void Submit()
        {
            var scenario = GameManager.Instance.Scenario;
            var result = point.Submit(Pick(victimDropdown), Pick(culpritDropdown), methodDropdown.value - 1, out float penalty);

            switch (result)
            {
                case ReportResult.Incomplete:
                    messageText.text = incompleteMessage;
                    break;
                case ReportResult.PartlyCorrect:
                    messageText.text = $"{scenario.partialMessage}\n{string.Format(penaltyFormat, ClockFormat.Format(penalty))}";
                    break;
                case ReportResult.Wrong:
                    messageText.text = $"{scenario.wrongMessage}\n{string.Format(penaltyFormat, ClockFormat.Format(penalty))}";
                    break;
                // Correct: GameManager ends the game and UIManager closes this panel.
            }
        }
    }
}
