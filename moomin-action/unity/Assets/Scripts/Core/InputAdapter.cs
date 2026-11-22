using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MoominAction
{
    /// <summary>새 Input System / 기존 Input Manager 어느 쪽 프로젝트든 동작하는 입력 도우미. 게임 입력은 전부 여기를 거침.</summary>
    public static class InputAdapter
    {
#if ENABLE_INPUT_SYSTEM
        static bool K(Key k) => Keyboard.current != null && Keyboard.current[k].wasPressedThisFrame;
        static bool KH(Key k) => Keyboard.current != null && Keyboard.current[k].isPressed;
        public static Vector2 Move()
        {
            Vector2 v = Vector2.zero;
            v.x += (KH(Key.D) || KH(Key.RightArrow) ? 1 : 0) - (KH(Key.A) || KH(Key.LeftArrow) ? 1 : 0);
            v.y += (KH(Key.W) || KH(Key.UpArrow) ? 1 : 0) - (KH(Key.S) || KH(Key.DownArrow) ? 1 : 0);
            if (Gamepad.current != null) v += Gamepad.current.leftStick.ReadValue();
            return Vector2.ClampMagnitude(v, 1f);
        }
        public static Vector2 Look() { var m = Mouse.current; Vector2 d = m != null ? m.delta.ReadValue() * 0.08f : Vector2.zero; if (Gamepad.current != null) d += Gamepad.current.rightStick.ReadValue() * 3f; return d; }
        public static bool AttackPressed() => (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) || K(Key.J) || (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame);
        public static bool SprintHeld() => KH(Key.LeftShift) || (Gamepad.current != null && Gamepad.current.leftStickButton.isPressed);
        // 전투(설계서 2-1): Space 회피, 우클릭 방어, Q/E/R 스킬, Tab 타깃 고정
        public static bool DodgePressed() => K(Key.Space) || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
        public static bool GuardHeld() => (Mouse.current != null && Mouse.current.rightButton.isPressed) || (Gamepad.current != null && Gamepad.current.leftShoulder.isPressed);
        public static int SkillPressed() { if (K(Key.Q) || (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame)) return 0; if (K(Key.E) || (Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame)) return 1; if (K(Key.R) || (Gamepad.current != null && Gamepad.current.rightTrigger.wasPressedThisFrame)) return 2; return -1; }
        public static bool LockOnPressed() => K(Key.Tab) || (Gamepad.current != null && Gamepad.current.rightStickButton.wasPressedThisFrame);
        public static bool RestartPressed() => K(Key.R);
        public static bool PausePressed() => K(Key.Escape) || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);
        public static bool InventoryPressed() => K(Key.I);
        public static bool EquipPressed() => K(Key.C);
        public static bool InteractPressed() => K(Key.F) || (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame);
        public static bool PetCommandPressed() => K(Key.G);
        public static bool MapPressed() => K(Key.M);
        public static int QuickPressed() { Key[] ks = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 }; for (int i = 0; i < 4; i++) if (K(ks[i])) return i; return -1; }
        // ── 메뉴 조작
        public static Vector2Int NavPressed() => new Vector2Int((K(Key.D) || K(Key.RightArrow) ? 1 : 0) - (K(Key.A) || K(Key.LeftArrow) ? 1 : 0), (K(Key.W) || K(Key.UpArrow) ? 1 : 0) - (K(Key.S) || K(Key.DownArrow) ? 1 : 0));
        public static bool ConfirmPressed() => K(Key.Enter) || K(Key.E) || K(Key.Space);
        public static bool CancelPressed() => K(Key.Escape) || K(Key.Backspace);
        public static bool DropPressed() => K(Key.X);
        public static bool SortPressed() => K(Key.R);
        public static bool TabNext() => K(Key.PageDown) || K(Key.RightBracket); public static bool TabPrev() => K(Key.PageUp) || K(Key.LeftBracket);
#else
        static bool K(KeyCode k) => Input.GetKeyDown(k);
        public static Vector2 Move() => Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
        public static Vector2 Look() => new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 3f;
        public static bool AttackPressed() => Input.GetMouseButtonDown(0) || K(KeyCode.J);
        public static bool SprintHeld() => Input.GetKey(KeyCode.LeftShift);
        public static bool DodgePressed() => K(KeyCode.Space);
        public static bool GuardHeld() => Input.GetMouseButton(1);
        public static int SkillPressed() => K(KeyCode.Q) ? 0 : K(KeyCode.E) ? 1 : K(KeyCode.R) ? 2 : -1;
        public static bool LockOnPressed() => K(KeyCode.Tab);
        public static bool RestartPressed() => K(KeyCode.R);
        public static bool PausePressed() => K(KeyCode.Escape);
        public static bool InventoryPressed() => K(KeyCode.I);
        public static bool EquipPressed() => K(KeyCode.C);
        public static bool InteractPressed() => K(KeyCode.F);
        public static bool PetCommandPressed() => K(KeyCode.G);
        public static bool MapPressed() => K(KeyCode.M);
        public static int QuickPressed() { KeyCode[] ks = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4 }; for (int i = 0; i < 4; i++) if (K(ks[i])) return i; return -1; }
        public static Vector2Int NavPressed() => new Vector2Int((K(KeyCode.D) || K(KeyCode.RightArrow) ? 1 : 0) - (K(KeyCode.A) || K(KeyCode.LeftArrow) ? 1 : 0), (K(KeyCode.W) || K(KeyCode.UpArrow) ? 1 : 0) - (K(KeyCode.S) || K(KeyCode.DownArrow) ? 1 : 0));
        public static bool ConfirmPressed() => K(KeyCode.Return) || K(KeyCode.E) || K(KeyCode.Space);
        public static bool CancelPressed() => K(KeyCode.Escape) || K(KeyCode.Backspace);
        public static bool DropPressed() => K(KeyCode.X);
        public static bool SortPressed() => K(KeyCode.R);
        public static bool TabNext() => K(KeyCode.PageDown) || K(KeyCode.RightBracket); public static bool TabPrev() => K(KeyCode.PageUp) || K(KeyCode.LeftBracket);
#endif
    }
}
