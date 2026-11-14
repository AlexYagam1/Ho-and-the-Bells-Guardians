using UnityEngine;
using UnityEngine.InputSystem;

namespace HoAndTheBells.Player
{
    [DefaultExecutionOrder(-200)]
    public sealed class AdventurePlayerInput : MonoBehaviour
    {
        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool Sprint { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool DropPressed { get; private set; }
        public bool HasControl => controlEnabled && captured && Application.isFocused;
        private bool controlEnabled = true;
        private bool captured;

        private void Start() => Capture();

        public void SetControlEnabled(bool enabled)
        {
            controlEnabled = enabled;
            if (!enabled) Release();
            else Capture();
        }

        private void Update()
        {
            Clear();
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (!Application.isFocused || !controlEnabled || keyboard == null || mouse == null)
            {
                Release();
                return;
            }
            if (keyboard.escapeKey.wasPressedThisFrame) { Release(); return; }
            if (captured && Cursor.lockState != CursorLockMode.Locked) Release();
            if (!captured)
            {
                if (mouse.leftButton.wasPressedThisFrame) Capture();
                return; // Discard the recapture frame's mouse delta.
            }
            Move = Vector2.ClampMagnitude(new Vector2(
                (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0)), 1f);
            Look = mouse.delta.ReadValue();
            Sprint = keyboard.leftShiftKey.isPressed;
            JumpPressed = keyboard.spaceKey.wasPressedThisFrame;
            DropPressed = keyboard.leftCtrlKey.wasPressedThisFrame;
        }

        private void Capture()
        {
            if (!controlEnabled || !Application.isFocused || Keyboard.current == null || Mouse.current == null) return;
            captured = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Release()
        {
            captured = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Clear();
        }

        private void Clear() { Move = Look = Vector2.zero; Sprint = JumpPressed = DropPressed = false; }
        private void OnApplicationFocus(bool focused) { if (!focused) Release(); }
        private void OnDisable() => Release();
    }
}
