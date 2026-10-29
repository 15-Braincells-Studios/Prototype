using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PreventTheMurder
{
    public enum GameState { Playing, InUI, Won, Lost }

    /// <summary>
    /// Owns game state, the countdown and the active scenario. On start it binds the scenario to the scene:
    /// NPCs by character id, clue spots by spot id, report points by reporter id.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Tooltip("The story for this run. Assign a different ScenarioData asset to play another mystery.")]
        [SerializeField] ScenarioData scenario;
        [SerializeField] bool pauseClockInUI = true;
        [SerializeField] bool showIntroCard = true;

        public ScenarioData Scenario => scenario;
        public GameState State { get; private set; } = GameState.Playing;
        public float TimeRemaining { get; private set; }
        public bool IsOver => State == GameState.Won || State == GameState.Lost;

        public event Action<float> OnTimeChanged;
        public event Action<GameState> OnStateChanged;
        /// <summary>won, title, body</summary>
        public event Action<bool, string, string> OnGameEnded;

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            if (!scenario)
            {
                Debug.LogError("GameManager has no ScenarioData assigned.", this);
                enabled = false;
                return;
            }

            BindScenario();
            SetTime(scenario.timeLimitSeconds);

            if (showIntroCard && !string.IsNullOrEmpty(scenario.intro) && UIManager.Instance)
                UIManager.Instance.ShowCard(scenario.title, scenario.intro);
        }

        void Update()
        {
            if (IsOver) return;
            if (State == GameState.InUI && pauseClockInUI) return;
            SetTime(TimeRemaining - Time.deltaTime);
        }

        /// <summary>
        /// Extension point: new NPCs, rooms, clue spots or report points (a bell, a messenger) only need
        /// matching ids in the scenario asset; nothing here has to change.
        /// </summary>
        void BindScenario()
        {
            foreach (var npc in FindObjectsByType<NPCController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var entry = scenario.FindCast(npc.CharacterId);
                if (entry == null)
                {
                    npc.gameObject.SetActive(false); // not part of this story
                    continue;
                }
                npc.Bind(entry.character, entry.dialogue);
            }

            foreach (var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (behaviour is IClueSpot spot)
                    spot.AssignClue(scenario.FindClueForSpot(spot.SpotId));

            foreach (var reportPoint in FindObjectsByType<ReportPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                reportPoint.Bind(scenario.FindCast(reportPoint.ReporterId)?.character);
        }

        void SetTime(float seconds)
        {
            TimeRemaining = Mathf.Max(0f, seconds);
            OnTimeChanged?.Invoke(TimeRemaining);
            if (TimeRemaining <= 0f && !IsOver) EndGame(false, scenario.loseTitle, scenario.loseText);
        }

        public void ApplyPenalty(float seconds)
        {
            if (!IsOver) SetTime(TimeRemaining - seconds);
        }

        public void SetInUI(bool inUI)
        {
            if (IsOver) return;
            var next = inUI ? GameState.InUI : GameState.Playing;
            if (next == State) return;
            State = next;
            OnStateChanged?.Invoke(State);
        }

        public void Win() => EndGame(true, scenario.winTitle, scenario.FormatWinText());

        void EndGame(bool won, string title, string body)
        {
            State = won ? GameState.Won : GameState.Lost;
            OnStateChanged?.Invoke(State);
            OnGameEnded?.Invoke(won, title, body);
        }

        /// <summary>Reloads the scene, which rebuilds every manager from scratch (no stale state).</summary>
        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
