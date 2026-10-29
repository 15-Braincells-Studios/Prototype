using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BlockoutMystery
{
    /// <summary>
    /// Builds all UI in code at startup and gives access to each view, on one HUD canvas over everything. (The inspect
    /// view's dim backdrop belongs to <see cref="InspectSystem"/>.)
    /// </summary>
    [DefaultExecutionOrder(-95)]
    public class UIRoot : MonoBehaviour
    {
        public static UIRoot Instance { get; private set; }

        public RectTransform Root { get; private set; }
        public HudView Hud { get; private set; }
        public InventoryBar InventoryBar { get; private set; }
        public DialogueBox Dialogue { get; private set; }
        public SpeechBubbleView Bubbles { get; private set; }
        public VisualNovelView VisualNovel { get; private set; }
        public CloseUpPresenter CloseUp { get; private set; }

        /// <summary>How this variant shows dialogue (see <see cref="PrototypeVariant"/>).</summary>
        public IDialoguePresenter DialoguePresenter { get; private set; }
        public NotebookPanel NotebookPanel { get; private set; }
        public BookPanel Book { get; private set; }
        public ReportPanel Report { get; private set; }
        public EndPopup End { get; private set; }

        public static void Toast(string message)
        {
            if (Instance) Instance.Hud.Toast(message);
        }

        void Awake()
        {
            Instance = this;
            EnsureEventSystem();
            Build();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Build()
        {
            var go = new GameObject("HUD Canvas", typeof(RectTransform)) { layer = 5 };
            go.transform.SetParent(transform, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            Root = (RectTransform)go.transform;

            // Sibling order is draw order: later views sit on top.
            var style = PrototypeVariant.Dialogue;
            Hud = HudView.Create(Root);
            InventoryBar = InventoryBar.Create(Root);
            if (style == DialogueStyle.CloseUp) CloseUp = CloseUpPresenter.Create(Root); // letterbox under the subtitle
            Dialogue = DialogueBox.Create(Root);
            if (style == DialogueStyle.SpeechBubbles) Bubbles = SpeechBubbleView.Create(Root);
            if (style == DialogueStyle.VisualNovel) VisualNovel = VisualNovelView.Create(Root);
            DialoguePresenter = style switch
            {
                DialogueStyle.SpeechBubbles => Bubbles,
                DialogueStyle.VisualNovel => VisualNovel,
                DialogueStyle.CloseUp => CloseUp.Bind(Dialogue),
                _ => Dialogue,
            };
            Book = BookPanel.Create(Root);
            Report = ReportPanel.Create(Root);
            NotebookPanel = NotebookPanel.Create(Root);
            End = EndPopup.Create(Root);
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem));
            es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }
}
