using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlockoutMystery
{
    /// <summary>
    /// Shows one line at a time through the variant's presenter (subtitles, speech bubbles, visual novel, close-up).
    /// During the intro, and in visual novel / close-up variants, the player stands still and E / Space / click
    /// continues. Otherwise the line is "free": the player keeps walking and looking around, and the line stays until
    /// E is pressed again or the player talks to someone else.
    /// </summary>
    [DefaultExecutionOrder(-50)] // after GameFlow, before PlayerInteractor
    public class DialogueSystem : MonoBehaviour
    {
        public static DialogueSystem Instance { get; private set; }

        [Tooltip("Speech bubbles close when the player walks further than this (metres) from the speaker.")]
        [SerializeField] float bubbleWalkAwayDistance = 6f;

        IDialoguePresenter presenter;
        List<string> pages;
        int page;
        Action onClosed;
        int openedFrame = -1;
        int consumedFrame = -1;
        bool haltsPlayer;

        public bool IsOpen { get; private set; }
        public Npc Speaker { get; private set; }

        /// <summary>A line is showing while the player can still walk around.</summary>
        public bool IsFreeLine => IsOpen && !haltsPlayer;

        /// <summary>E was used to continue a line this frame, so nothing else should react to it.</summary>
        public bool ConsumedInteractThisFrame => consumedFrame == Time.frameCount;

        IDialoguePresenter Presenter => presenter ??= UIRoot.Instance.DialoguePresenter;

        void Awake()
        {
            Instance = this;
            GameFlow.Instance.Changed += OnFlowChanged;
        }

        void OnDestroy()
        {
            if (GameFlow.Instance) GameFlow.Instance.Changed -= OnFlowChanged;
            if (Instance == this) Instance = null;
        }

        public void Show(Npc speaker, string line, Action closed = null)
        {
            if (IsOpen) Close();
            var flow = GameFlow.Instance;
            haltsPlayer = flow.Mode == GameMode.Intro || PrototypeVariant.DialogueHaltsPlayer;

            Speaker = speaker;
            onClosed = closed;
            IsOpen = true;
            openedFrame = Time.frameCount;
            pages = Presenter.Paginate(line);
            page = 0;
            Presenter.Show(speaker, pages[0], pages.Count > 1);

            if (haltsPlayer && flow.Mode != GameMode.Intro) flow.SetMode(GameMode.Dialogue);
        }

        void Update()
        {
            var flow = GameFlow.Instance;
            if (!IsOpen || Time.frameCount == openedFrame || flow.NotebookOpen || flow.ChangedThisFrame) return;

            if (haltsPlayer)
            {
                if (GameInput.Advance || GameInput.PointerDown || (flow.Mode == GameMode.Intro && GameInput.Back)) Next();
                return;
            }

            // A speech bubble is only heard nearby: walking away ends it.
            var player = PlayerInteractor.Instance;
            if (PrototypeVariant.Dialogue == DialogueStyle.SpeechBubbles && Speaker && player)
            {
                Vector3 apart = Speaker.transform.position - player.transform.position;
                apart.y = 0f;
                if (apart.magnitude > bubbleWalkAwayDistance)
                {
                    Close();
                    return;
                }
            }

            // Free line: E continues it, unless the player is aiming at something else. Then PlayerInteractor uses the
            // press instead (talking to someone else replaces this line).
            if (!GameInput.Interact) return;
            var focus = player ? player.Focus : null;
            if (focus && focus != Speaker) return;
            consumedFrame = Time.frameCount;
            Next();
        }

        /// <summary>Finishes the typing, shows the next part, or closes the line.</summary>
        public void Next()
        {
            if (!IsOpen) return;
            if (Presenter.IsTyping)
            {
                Presenter.Finish();
                return;
            }
            if (page < pages.Count - 1)
            {
                page++;
                Presenter.Show(Speaker, pages[page], page < pages.Count - 1);
                return;
            }
            Close();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            Presenter.Hide();
            Speaker = null;
            var callback = onClosed;
            onClosed = null;
            var flow = GameFlow.Instance;
            if (flow.Mode == GameMode.Dialogue) flow.SetMode(GameMode.Explore);
            callback?.Invoke();
        }

        void OnFlowChanged()
        {
            // A free line closes when the player starts something else: inspecting, reading, reporting, the ending.
            if (IsOpen && !haltsPlayer && GameFlow.Instance.Mode != GameMode.Explore) Close();
        }
    }

    /// <summary>One way of putting a line of dialogue on screen. Picked by <see cref="UIRoot"/> from the variant.</summary>
    public interface IDialoguePresenter
    {
        /// <summary>Splits a line into the parts shown one after another (a single part unless the style limits text).</summary>
        List<string> Paginate(string line);

        /// <param name="more">More parts of this line follow.</param>
        void Show(Npc speaker, string text, bool more);

        bool IsTyping { get; }
        void Finish();
        void Hide();
    }
}
