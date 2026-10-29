using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PreventTheMurder
{
    /// <summary>
    /// The single owner of the Canvas: HUD updates, panel hotkeys and the shared "a panel is open" state
    /// that unlocks the cursor, freezes the player and pauses the clock.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Player")]
        [SerializeField] FirstPersonController player;
        [SerializeField] PlayerInteractor interactor;

        [Header("HUD")]
        [SerializeField] GameObject promptRoot;
        [SerializeField] TMP_Text promptText;
        [SerializeField] TMP_Text clockLabel;
        [SerializeField] TMP_Text clockText;
        [SerializeField] Color clockColor = Color.white;
        [SerializeField] Color clockWarningColor = new Color(1f, 0.3f, 0.25f);
        [SerializeField] float clockWarningBelowSeconds = 120f;
        [SerializeField] ToastUI toast;

        [Header("Panels")]
        [SerializeField] DialoguePanelUI dialoguePanel;
        [SerializeField] NotepadUI notepad;
        [SerializeField] ClueCardUI clueCard;
        [SerializeField] ReportPanelUI reportPanel;
        [SerializeField] EndPanelUI winPanel;
        [SerializeField] EndPanelUI losePanel;
        [SerializeField] GameObject pauseRoot;
        [SerializeField] Button resumeButton;
        [SerializeField] Button pauseRestartButton;

        [Header("Text")]
        [SerializeField] string newClueFormat = "New clue: {0}";
        [SerializeField] string clueFoundFormat = "You found: {0}";

        int modalCount;
        bool paused;

        public bool AnyPanelOpen => modalCount > 0;

        void Awake()
        {
            Instance = this;
            pauseRoot.SetActive(false);
            resumeButton.onClick.AddListener(Resume);
            pauseRestartButton.onClick.AddListener(() => GameManager.Instance.Restart());
            SetPrompt(null);
        }

        void Start()
        {
            var gm = GameManager.Instance;
            gm.OnTimeChanged += UpdateClock;
            gm.OnGameEnded += HandleGameEnded;
            ClueManager.Instance.OnClueDiscovered += HandleClueDiscovered;

            if (gm.Scenario) clockLabel.text = gm.Scenario.clockLabel;
            UpdateClock(gm.TimeRemaining);
            ApplyInputState();
        }

        void OnDestroy()
        {
            if (GameManager.Instance)
            {
                GameManager.Instance.OnTimeChanged -= UpdateClock;
                GameManager.Instance.OnGameEnded -= HandleGameEnded;
            }
            if (ClueManager.Instance) ClueManager.Instance.OnClueDiscovered -= HandleClueDiscovered;
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (GameManager.Instance.IsOver) return;

            if (InputReader.PausePressed)
            {
                if (notepad.IsOpen) notepad.Close();
                else if (paused) Resume();
                else if (!AnyPanelOpen) Pause();
            }
            else if (InputReader.NotepadPressed)
            {
                if (notepad.IsOpen) notepad.Close();
                else if (!AnyPanelOpen) notepad.Open();
            }
        }

        // ---- Modal bookkeeping: every panel that should block play calls Push on open and Pop on close.

        public void PushModal()
        {
            modalCount++;
            ApplyInputState();
        }

        public void PopModal()
        {
            modalCount = Mathf.Max(0, modalCount - 1);
            ApplyInputState();
        }

        void ApplyInputState()
        {
            var gm = GameManager.Instance;
            bool over = gm && gm.IsOver;
            bool blocked = over || AnyPanelOpen;

            Cursor.lockState = blocked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = blocked;
            if (player) player.InputEnabled = !blocked;
            if (interactor) interactor.enabled = !blocked;
            if (gm && !over) gm.SetInUI(AnyPanelOpen);
        }

        // ---- HUD

        public void SetPrompt(string prompt)
        {
            bool show = !string.IsNullOrEmpty(prompt);
            if (show) promptText.text = prompt;
            if (promptRoot.activeSelf != show) promptRoot.SetActive(show);
        }

        public void ShowToast(string message) => toast.Show(message);

        void UpdateClock(float seconds)
        {
            clockText.text = ClockFormat.Format(seconds);
            clockText.color = seconds < clockWarningBelowSeconds ? clockWarningColor : clockColor;
        }

        void HandleClueDiscovered(DiscoveredClue found) => toast.Show(string.Format(newClueFormat, found.clue.title));

        // ---- Panels

        public void ShowClueCard(ClueData clue) => clueCard.Show(string.Format(clueFoundFormat, clue.title), clue.description);

        public void ShowCard(string title, string body) => clueCard.Show(title, body);

        public void OpenReport(ReportPoint point) => reportPanel.Open(point);

        void Pause()
        {
            paused = true;
            pauseRoot.SetActive(true);
            Time.timeScale = 0f;
            PushModal();
        }

        void Resume()
        {
            if (!paused) return;
            paused = false;
            pauseRoot.SetActive(false);
            Time.timeScale = 1f;
            PopModal();
        }

        void HandleGameEnded(bool won, string title, string body)
        {
            if (DialogueManager.Instance) DialogueManager.Instance.EndDialogue();
            notepad.Close();
            clueCard.Hide();
            reportPanel.Close();
            Resume();
            modalCount = 0;
            SetPrompt(null);

            (won ? winPanel : losePanel).Show(title, body);
            ApplyInputState();
        }
    }
}
