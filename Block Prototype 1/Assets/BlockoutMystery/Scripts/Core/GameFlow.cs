using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlockoutMystery
{
    public enum GameMode { Intro, Explore, Dialogue, Inspect, Book, Report, Ended }

    /// <summary>
    /// Everything the end screen needs to know. Winning depends on the plate, the suspect and the victim; the evidence
    /// is graded on its own (<see cref="CaseFile.JudgeEvidence"/>).
    /// </summary>
    public readonly struct CaseResult
    {
        public readonly bool won, plateRight, suspectRight, victimRight;
        public readonly float time;
        public readonly string suspect, victim;
        public readonly HerbColor plate;
        public readonly CaseStrength strength;
        public readonly int evidenceCount, supporting, irrelevant;

        public CaseResult(float time, string suspect, string victim, IReadOnlyList<Notebook.Item> evidence, HerbColor plate)
        {
            this.time = time;
            this.suspect = suspect;
            this.victim = victim;
            this.plate = plate;
            plateRight = plate == CaseFile.PoisonedPlate;
            suspectRight = suspect == CaseFile.SolutionSuspect;
            victimRight = victim == CaseFile.SolutionVictim;
            won = plateRight && suspectRight && victimRight;

            var ids = new List<string>();
            foreach (var item in evidence) ids.Add(Notebook.Instance.EvidenceIdOf(item));
            evidenceCount = ids.Count;
            strength = CaseFile.JudgeEvidence(ids, out supporting, out irrelevant);
        }
    }

    /// <summary>
    /// The game state: the butler's intro, which mode is active (exploring or one of the modal views), the journal
    /// overlay, Q / E / Escape routing, the mouse cursor, the key's hiding place, the antidote choice and the ending.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }

        [Tooltip("Statuettes that can hide the key. One is picked at random on every start.")]
        [SerializeField] Statuette[] statuettes;
        [Tooltip("For testing: always hide the key in this statuette (index into the list). -1 = random.")]
        [SerializeField] int forceKeyStatuette = -1;

        int lastChangeFrame = -1;

        public GameMode Mode { get; private set; } = GameMode.Intro;
        public bool NotebookOpen { get; private set; }
        public bool CanMove => Mode == GameMode.Explore && !NotebookOpen;

        /// <summary>True on the frame the mode or the journal changed, so the same key press is not handled twice.</summary>
        public bool ChangedThisFrame => lastChangeFrame == Time.frameCount;

        public HerbColor? AntidotePlate { get; private set; }
        public bool AntidoteUsed => AntidotePlate.HasValue;
        public Statuette KeyStatuette { get; private set; }

        public event Action Changed;

        public void Configure(Statuette[] keyHolders) => statuettes = keyHolders;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            HideKey();
            SetMode(GameMode.Intro);

            Npc butler = null;
            foreach (var npc in FindObjectsByType<Npc>(FindObjectsSortMode.None))
                if (npc.Id == CaseFile.Butler) butler = npc;

            Notebook.Instance.Record(CaseFile.Butler, CaseFile.NameOf(CaseFile.Butler), CaseFile.ButlerIntro);
            DialogueSystem.Instance.Show(butler, CaseFile.ButlerIntro, () =>
            {
                GameTimer.Instance.Begin();
                SetMode(GameMode.Explore);
            });
        }

        void HideKey()
        {
            if (statuettes == null || statuettes.Length == 0)
                statuettes = FindObjectsByType<Statuette>(FindObjectsSortMode.InstanceID);
            if (statuettes.Length == 0)
            {
                Debug.LogError("[BlockoutMystery] No statuettes in the scene, so the key cannot be hidden.", this);
                return;
            }

            foreach (var s in statuettes) if (s) s.SetHasKey(false);
            int index = forceKeyStatuette >= 0 && forceKeyStatuette < statuettes.Length
                ? forceKeyStatuette
                : UnityEngine.Random.Range(0, statuettes.Length);
            KeyStatuette = statuettes[index];
            KeyStatuette.SetHasKey(true);
        }

        void Update()
        {
            ApplyCursor();
            if (Mode == GameMode.Intro || Mode == GameMode.Ended) return; // the intro dialogue handles its own input

            if (GameInput.Notebook)
            {
                SetNotebook(!NotebookOpen);
                return;
            }

            // E puts the inspected object back (and closes the book), like Escape. Not on the frame E opened it.
            if (GameInput.Interact && !NotebookOpen && !ChangedThisFrame)
            {
                if (Mode == GameMode.Inspect)
                {
                    InspectSystem.Instance.End();
                    return;
                }
                if (Mode == GameMode.Book)
                {
                    UIRoot.Instance.Book.Close();
                    return;
                }
            }

            if (!GameInput.Back) return;
            if (NotebookOpen)
            {
                SetNotebook(false);
                return;
            }

            switch (Mode)
            {
                case GameMode.Explore: DialogueSystem.Instance.Close(); break; // a line shown while walking
                case GameMode.Inspect: InspectSystem.Instance.End(); break;
                case GameMode.Dialogue: DialogueSystem.Instance.Close(); break;
                case GameMode.Book: UIRoot.Instance.Book.Close(); break;
                case GameMode.Report:
                    if (!UIRoot.Instance.Report.CloseEvidencePicker()) ReportSystem.Instance.Cancel();
                    break;
            }
        }

        public void SetMode(GameMode mode)
        {
            Mode = mode;
            MarkChanged();
        }

        public void SetNotebook(bool open)
        {
            if (open && (Mode == GameMode.Intro || Mode == GameMode.Ended)) return;
            NotebookOpen = open;
            UIRoot.Instance.NotebookPanel.SetVisible(open);
            MarkChanged();
        }

        void MarkChanged()
        {
            lastChangeFrame = Time.frameCount;
            ApplyCursor();
            Changed?.Invoke();
        }

        /// <summary>
        /// The system cursor only shows where you point at things: panels, the journal and the inspect view. While
        /// exploring the HUD draws its own dot instead (in the centre, or following the mouse in the isometric variant,
        /// whose camera doesn't use the mouse to look); during the intro and held conversations nothing is needed. Applied every frame, because the Editor and the OS can bring the cursor back on their
        /// own (a click in the Game view, Escape, switching windows).
        /// </summary>
        void ApplyCursor()
        {
            bool pointer = NotebookOpen || Mode == GameMode.Inspect || Mode == GameMode.Book || Mode == GameMode.Report || Mode == GameMode.Ended;
            var lockState = !pointer && !PrototypeVariant.Isometric ? CursorLockMode.Locked : CursorLockMode.None;
            if (Cursor.lockState != lockState) Cursor.lockState = lockState;
            if (Cursor.visible != pointer) Cursor.visible = pointer;
        }

        /// <summary>The single antidote portion goes onto this plate. Final.</summary>
        public bool UseAntidote(HerbColor plate)
        {
            if (AntidoteUsed || !Inventory.Instance.Remove(ItemId.Antidote)) return false;
            AntidotePlate = plate;
            return true;
        }

        public void EndGame(string suspect, string victim, IReadOnlyList<Notebook.Item> evidence)
        {
            if (Mode == GameMode.Ended) return;
            var result = new CaseResult(GameTimer.Instance.Stop(), suspect, victim, evidence, AntidotePlate ?? HerbColor.Orange);
            if (NotebookOpen) SetNotebook(false);
            SetMode(GameMode.Ended);
            UIRoot.Instance.End.Show(result);
        }

        /// <summary>Reloads the scene: new key statuette, fresh timer, empty inventory and journal.</summary>
        public void Restart()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0)
            {
                SceneManager.LoadScene(scene.buildIndex);
                return;
            }
#if UNITY_EDITOR
            // Not in Build Settings (kept out on purpose so the prototypes stay separate): reload by path in the Editor.
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            Debug.LogError("[BlockoutMystery] Add this scene to the build so it can be restarted.");
#endif
        }
    }
}
