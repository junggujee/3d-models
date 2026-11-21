using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MoominAction
{
    /// <summary>새 Input System / 기존 Input Manager 어느 쪽 프로젝트든 동작하는 입력 도우미.</summary>
    public static class InputAdapter
    {
#if ENABLE_INPUT_SYSTEM
        public static Vector2 Move()
        {
            Vector2 v = Vector2.zero; var k = Keyboard.current;
            if (k != null) { v.x += (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0); v.y += (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0); }
            if (Gamepad.current != null) v += Gamepad.current.leftStick.ReadValue();
            return Vector2.ClampMagnitude(v, 1f);
        }
        public static Vector2 Look() { var m = Mouse.current; Vector2 d = m != null ? m.delta.ReadValue() * 0.08f : Vector2.zero; if (Gamepad.current != null) d += Gamepad.current.rightStick.ReadValue() * 3f; return d; }
        public static bool AttackPressed() => (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) || (Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame);
        public static bool JumpPressed() => (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
        public static bool SprintHeld() => (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed) || (Gamepad.current != null && Gamepad.current.leftStickButton.isPressed);
        public static bool RestartPressed() => Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        public static bool EscapePressed() => Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        public static Vector2 Move() => Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
        public static Vector2 Look() => new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 3f;
        public static bool AttackPressed() => Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J);
        public static bool JumpPressed() => Input.GetKeyDown(KeyCode.Space);
        public static bool SprintHeld() => Input.GetKey(KeyCode.LeftShift);
        public static bool RestartPressed() => Input.GetKeyDown(KeyCode.R);
        public static bool EscapePressed() => Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}
