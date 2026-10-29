using UnityEngine;
using UnityEngine.InputSystem;

namespace PreventTheMurder
{
    /// <summary>
    /// Every raw input read in the game goes through here, so switching to action maps, gamepad
    /// or the legacy Input Manager only means changing this file.
    /// </summary>
    public static class InputReader
    {
        public const string InteractKeyLabel = "E";

        static int consumedFrame = -1;

        public static Vector2 Move
        {
            get
            {
                var kb = Keyboard.current;
                if (kb == null) return Vector2.zero;
                float x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
                float y = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
                return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
            }
        }

        public static Vector2 LookDelta => Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;

        public static bool NotepadPressed => WasPressed(Key.J);
        public static bool PausePressed => WasPressed(Key.Escape);

        /// <summary>
        /// Interact / continue. Only one caller gets the press per frame, so closing a panel with E
        /// does not also trigger whatever the player is looking at.
        /// </summary>
        public static bool TryConsumeInteract(bool includeContinueKeys = false)
        {
            if (consumedFrame == Time.frameCount) return false;
            var kb = Keyboard.current;
            if (kb == null) return false;

            bool pressed = kb.eKey.wasPressedThisFrame
                || (includeContinueKeys && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame));
            if (pressed) consumedFrame = Time.frameCount;
            return pressed;
        }

        static bool WasPressed(Key key) => Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
    }
}
