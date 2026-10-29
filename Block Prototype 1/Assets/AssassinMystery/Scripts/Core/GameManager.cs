using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AssassinMystery
{
    public enum GameState { Briefing, Investigating, Cutscene, Ended }
    public enum LossReason { TimeExpired, TooManyAccusations }

    /// <summary>
    /// Owns the overall flow: briefing, investigation, pause, the endings and restart.
    /// Global keys (Esc, Tab) are handled here so only one place decides what is open.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Tooltip("The case to play.")]
        [SerializeField] MysteryData mystery;
        [SerializeField] AssassinationSequence sequence;
        [Tooltip("Show the title / briefing screen when the scene starts (Restart skips it).")]
        [SerializeField] bool showBriefing = true;

        static bool skipBriefingOnNextLoad;

        public MysteryData Mystery => mystery;
        public GameState State { get; private set; } = GameState.Briefing;
        public bool IsPaused { get; private set; }

        public bool CanControlPlayer =>
            State == GameState.Investigating && !IsPaused && UIManager.Instance != null && UIManager.Instance.ActivePanel == UIPanel.None;

        public event Action<GameState> OnStateChanged;

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            if (!mystery) Debug.LogError("[AssassinMystery] GameManager has no MysteryData assigned.", this);
            if (!sequence) Debug.LogError("[AssassinMystery] GameManager has no AssassinationSequence assigned.", this);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            if (GameTimer.Instance != null) GameTimer.Instance.OnExpired += () => Lose(LossReason.TimeExpired);

            bool skip = skipBriefingOnNextLoad || !showBriefing;
            skipBriefingOnNextLoad = false;
            if (skip) BeginInvestigation();
            else UIManager.Instance.ShowBriefing(mystery);
        }

        void SetState(GameState s)
        {
            State = s;
            OnStateChanged?.Invoke(s);
        }

        public void BeginInvestigation()
        {
            if (State == GameState.Investigating) return;
            UIManager.Instance.HideBriefing();
            SetState(GameState.Investigating);
            GameTimer.Instance.StartTimer(mystery ? mystery.durationSeconds : 600f);
            UIManager.Instance.Toast("The investigation begins", "Find the assassin before the vesper bell. Press Tab for your notebook.");
        }

        void Update()
        {
            var ui = UIManager.Instance;
            if (ui == null) return;

            if (GameInput.PausePressed)
            {
                if (IsPaused) Resume();
                else if (State == GameState.Investigating)
                {
                    if (ui.ActivePanel != UIPanel.None) ui.CloseActivePanel();
                    else Pause();
                }
            }
            else if (GameInput.NotebookPressed && State == GameState.Investigating && !IsPaused)
            {
                if (ui.ActivePanel == UIPanel.None) ui.OpenNotebook();
                else if (ui.ActivePanel == UIPanel.Notebook) ui.CloseActivePanel();
            }
            else if (GameInput.InteractPressed && ui.ActivePanel == UIPanel.Inspect && !IsPaused)
            {
                ui.CloseActivePanel();
            }
        }

        void LateUpdate()
        {
            bool locked = CanControlPlayer;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        public void Pause()
        {
            if (IsPaused || State != GameState.Investigating) return;
            IsPaused = true;
            Time.timeScale = 0f;
            UIManager.Instance.ShowPause(true);
        }

        public void Resume()
        {
            if (!IsPaused) return;
            IsPaused = false;
            Time.timeScale = 1f;
            UIManager.Instance.ShowPause(false);
        }

        public void Restart()
        {
            skipBriefingOnNextLoad = true;
            ReloadScene();
        }

        public void ReturnToMainMenu()
        {
            skipBriefingOnNextLoad = false;
            ReloadScene();
        }

        static void ReloadScene()
        {
            Time.timeScale = 1f;
            var scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0)
            {
                SceneManager.LoadScene(scene.buildIndex);
                return;
            }
#if UNITY_EDITOR
            // Not in Build Settings (kept out on purpose so this prototype stays separate): reload by path in the Editor.
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            Debug.LogError("[AssassinMystery] Add this scene to Build Settings so it can be restarted in a build.");
#endif
        }

        /// <summary>Called by AccusationManager when the assassin is named with enough proof.</summary>
        public void Win(IReadOnlyList<ClueData> provingEvidence)
        {
            if (State != GameState.Investigating) return;
            float remaining = GameTimer.Instance.Remaining;
            GameTimer.Instance.Stop();
            Resume();
            UIManager.Instance.CloseAllPanels();
            SetState(GameState.Cutscene);

            sequence.PlayPrevented(mystery.assassin, () =>
            {
                SetState(GameState.Ended);
                var a = mystery.assassin;
                string evidence = string.Join("\n", provingEvidence.Select(c => $"  - {c.title}"));
                string body =
                    $"<b>The assassin was {a.displayName}, {a.role}.</b>\n\n{mystery.solution}\n\n" +
                    $"<b>The evidence that exposed them:</b>\n{evidence}\n\n" +
                    $"<b>Time remaining:</b> {GameTimer.Format(remaining)}";
                UIManager.Instance.ShowEnd(true, "The Queen Lives", body);
            });
        }

        public void Lose(LossReason reason)
        {
            if (State != GameState.Investigating) return;
            GameTimer.Instance.Stop();
            Resume();
            UIManager.Instance.CloseAllPanels();
            SetState(GameState.Cutscene);

            sequence.PlayFailure(mystery.assassin, () =>
            {
                SetState(GameState.Ended);
                var a = mystery.assassin;
                string why = reason == LossReason.TimeExpired
                    ? "The vesper bell rang before you could name the killer."
                    : "Too many false accusations. While the guard chased your mistakes, the assassin struck.";
                string body =
                    $"{why}\n\n<b>The assassin was {a.displayName}, {a.role}.</b>\n\n{mystery.solution}";
                UIManager.Instance.ShowEnd(false, "The Queen Is Dead", body);
            });
        }
    }
}
