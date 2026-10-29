using UnityEngine;
using UnityEngine.InputSystem;

namespace BlockoutMystery
{
    /// <summary>Keyboard and mouse input in one place, read straight from the Input System devices (null-safe).</summary>
    public static class GameInput
    {
        static Keyboard K => Keyboard.current;
        static Mouse M => Mouse.current;

        public static Vector2 Move
        {
            get
            {
                if (K == null) return Vector2.zero;
                var v = Vector2.zero;
                if (K.wKey.isPressed || K.upArrowKey.isPressed) v.y += 1f;
                if (K.sKey.isPressed || K.downArrowKey.isPressed) v.y -= 1f;
                if (K.dKey.isPressed || K.rightArrowKey.isPressed) v.x += 1f;
                if (K.aKey.isPressed || K.leftArrowKey.isPressed) v.x -= 1f;
                return Vector2.ClampMagnitude(v, 1f);
            }
        }

        public static Vector2 PointerDelta => M != null ? M.delta.ReadValue() : Vector2.zero;
        public static Vector2 PointerPosition => M != null ? M.position.ReadValue() : Vector2.zero;
        public static bool PointerDown => M != null && M.leftButton.wasPressedThisFrame;
        public static bool PointerHeld => M != null && M.leftButton.isPressed;
        public static bool PointerUp => M != null && M.leftButton.wasReleasedThisFrame;

        public static bool Interact => K != null && K.eKey.wasPressedThisFrame;
        public static bool Notebook => K != null && K.qKey.wasPressedThisFrame;
        public static bool Back => K != null && K.escapeKey.wasPressedThisFrame;

        /// <summary>Continue a line of dialogue.</summary>
        public static bool Advance =>
            K != null && (K.eKey.wasPressedThisFrame || K.spaceKey.wasPressedThisFrame || K.enterKey.wasPressedThisFrame);

        /// <summary>Switch between third and first person.</summary>
        public static bool ToggleView => K != null && K.vKey.wasPressedThisFrame;

        /// <summary>Mouse wheel this frame (positive = away from you).</summary>
        public static float Scroll => M != null ? M.scroll.ReadValue().y : 0f;

        public static bool PageNext => K != null && (K.dKey.wasPressedThisFrame || K.rightArrowKey.wasPressedThisFrame);
        public static bool PagePrevious => K != null && (K.aKey.wasPressedThisFrame || K.leftArrowKey.wasPressedThisFrame);
    }
}
