using UnityEngine;
using UnityEngine.InputSystem;

namespace AssassinMystery
{
    /// <summary>Keyboard and mouse input in one place (Input System devices, read directly; null-safe).</summary>
    public static class GameInput
    {
        public static Vector2 Move
        {
            get
            {
                var k = Keyboard.current;
                if (k == null) return Vector2.zero;
                var v = Vector2.zero;
                if (k.wKey.isPressed || k.upArrowKey.isPressed) v.y += 1f;
                if (k.sKey.isPressed || k.downArrowKey.isPressed) v.y -= 1f;
                if (k.dKey.isPressed || k.rightArrowKey.isPressed) v.x += 1f;
                if (k.aKey.isPressed || k.leftArrowKey.isPressed) v.x -= 1f;
                return Vector2.ClampMagnitude(v, 1f);
            }
        }

        public static Vector2 Look => Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
        public static bool Sprint => Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
        public static bool InteractPressed => Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        public static bool PausePressed => Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;

        public static bool NotebookPressed =>
            Keyboard.current != null && (Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.nKey.wasPressedThisFrame);
    }
}
