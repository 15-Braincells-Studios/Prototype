using UnityEngine;

namespace BlockoutMystery
{
    /// <summary>How lines of dialogue are shown.</summary>
    public enum DialogueStyle { Subtitles, SpeechBubbles, VisualNovel, CloseUp }

    /// <summary>How the player sees the world.</summary>
    public enum ViewStyle { ThirdPersonSwitchable, Isometric }

    /// <summary>
    /// Which variant of the prototype this scene is. All variants share the same puzzle and systems; only the camera
    /// and the way dialogue is presented change. The scene builder sets this on the Systems object.
    /// </summary>
    [DefaultExecutionOrder(-110)]
    public class PrototypeVariant : MonoBehaviour
    {
        static PrototypeVariant instance;

        [Tooltip("Shown in the HUD so you know which variant you are playing.")]
        [SerializeField] string title = "Subtitles";
        [SerializeField] DialogueStyle dialogue = DialogueStyle.Subtitles;
        [SerializeField] ViewStyle view = ViewStyle.ThirdPersonSwitchable;

        public static string Title => instance ? instance.title : "Subtitles";
        public static DialogueStyle Dialogue => instance ? instance.dialogue : DialogueStyle.Subtitles;
        public static ViewStyle View => instance ? instance.view : ViewStyle.ThirdPersonSwitchable;
        public static bool Isometric => View == ViewStyle.Isometric;

        /// <summary>Visual novel and close-up conversations take over the screen, so the player stands still.</summary>
        public static bool DialogueHaltsPlayer => Dialogue == DialogueStyle.VisualNovel || Dialogue == DialogueStyle.CloseUp;

        public void Configure(string variantTitle, DialogueStyle dialogueStyle, ViewStyle viewStyle)
        {
            title = variantTitle;
            dialogue = dialogueStyle;
            view = viewStyle;
        }

        void Awake() => instance = this;

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
