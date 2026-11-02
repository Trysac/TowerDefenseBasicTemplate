using UnityEngine;
using UnityEngine.InputSystem;

namespace SaucerSiege.Game
{
    /// <summary>
    /// Mouse, keyboard and gamepad through the Input System. The mouse points at tiles; the
    /// keyboard arrows (or WASD) and the gamepad stick or D-pad move a tile cursor instead.
    /// </summary>
    public sealed class GameInput
    {
        const float StickRepeat = 0.16f;

        Vector2 lastMouse;
        float stickTimer;
        Vector2Int heldStick;

        static Keyboard Keys => Keyboard.current;
        static Mouse Pointer => Mouse.current;
        static Gamepad Pad => Gamepad.current;

        /// <summary>True when the mouse moved or clicked since the cursor last moved by keys or stick.</summary>
        public bool PointerActive { get; private set; } = true;

        public bool UsingGamepad { get; private set; }

        public Vector2 PointerPosition => Pointer != null ? Pointer.position.ReadValue() : Vector2.zero;

        public void Poll(float dt)
        {
            if (Pointer != null)
            {
                Vector2 mouse = Pointer.position.ReadValue();
                if ((mouse - lastMouse).sqrMagnitude > 9f || Pointer.leftButton.wasPressedThisFrame || Pointer.rightButton.wasPressedThisFrame)
                {
                    PointerActive = true;
                    UsingGamepad = false;
                }
                lastMouse = mouse;
            }
            stickTimer -= dt;
        }

        // ---- Pointer ----

        /// <summary>A tap can press and release within one frame; it still counts once.</summary>
        public bool Click => Pointer != null && Pointer.leftButton.wasPressedThisFrame;

        public bool RightClick => Pointer != null && Pointer.rightButton.wasPressedThisFrame;

        // ---- Tile cursor ----

        /// <summary>One cursor step this frame from the arrows, WASD, the D-pad or the stick (with repeat).</summary>
        public Vector2Int CursorStep()
        {
            var step = Vector2Int.zero;
            if (Keys != null)
            {
                if (Keys.leftArrowKey.wasPressedThisFrame || Keys.aKey.wasPressedThisFrame) step.x = -1;
                if (Keys.rightArrowKey.wasPressedThisFrame || Keys.dKey.wasPressedThisFrame) step.x = 1;
                if (Keys.upArrowKey.wasPressedThisFrame || Keys.wKey.wasPressedThisFrame) step.y = -1;
                if (Keys.downArrowKey.wasPressedThisFrame || Keys.sKey.wasPressedThisFrame) step.y = 1;
                if (step != Vector2Int.zero) UsingGamepad = false;
            }
            if (Pad != null)
            {
                if (Pad.dpad.left.wasPressedThisFrame) step.x = -1;
                if (Pad.dpad.right.wasPressedThisFrame) step.x = 1;
                if (Pad.dpad.up.wasPressedThisFrame) step.y = -1;
                if (Pad.dpad.down.wasPressedThisFrame) step.y = 1;
                Vector2 stick = Pad.leftStick.ReadValue();
                var held = new Vector2Int(stick.x > 0.55f ? 1 : stick.x < -0.55f ? -1 : 0, stick.y > 0.55f ? -1 : stick.y < -0.55f ? 1 : 0);
                if (held != Vector2Int.zero && (held != heldStick || stickTimer <= 0f))
                {
                    step = held;
                    stickTimer = held != heldStick ? StickRepeat * 2f : StickRepeat;
                }
                heldStick = held;
                if (step != Vector2Int.zero) UsingGamepad = true;
            }
            if (step != Vector2Int.zero)
            {
                PointerActive = false;
            }
            return step;
        }

        /// <summary>Build or select at the cursor: Enter, or the gamepad's south button.</summary>
        public bool Act =>
            (Keys != null && (Keys.enterKey.wasPressedThisFrame || Keys.numpadEnterKey.wasPressedThisFrame)) ||
            (Pad != null && Pad.buttonSouth.wasPressedThisFrame);

        /// <summary>Cancel building or the selection: Esc, Backspace or the east button.</summary>
        public bool Cancel =>
            (Keys != null && (Keys.escapeKey.wasPressedThisFrame || Keys.backspaceKey.wasPressedThisFrame)) ||
            (Pad != null && Pad.buttonEast.wasPressedThisFrame);

        // ---- Battle commands ----

        /// <summary>The tower picked with keys 1-4 (0-based), or -1.</summary>
        public int TowerKey
        {
            get
            {
                if (Keys == null) return -1;
                if (Keys.digit1Key.wasPressedThisFrame || Keys.numpad1Key.wasPressedThisFrame) return 0;
                if (Keys.digit2Key.wasPressedThisFrame || Keys.numpad2Key.wasPressedThisFrame) return 1;
                if (Keys.digit3Key.wasPressedThisFrame || Keys.numpad3Key.wasPressedThisFrame) return 2;
                if (Keys.digit4Key.wasPressedThisFrame || Keys.numpad4Key.wasPressedThisFrame) return 3;
                return -1;
            }
        }

        /// <summary>-1 or +1 to step through the towers to build (Q/E, or the gamepad shoulders).</summary>
        public int TowerCycle
        {
            get
            {
                if ((Keys != null && Keys.qKey.wasPressedThisFrame) || (Pad != null && Pad.leftShoulder.wasPressedThisFrame)) return -1;
                if ((Keys != null && Keys.eKey.wasPressedThisFrame) || (Pad != null && Pad.rightShoulder.wasPressedThisFrame)) return 1;
                return 0;
            }
        }

        public bool CallWave =>
            (Keys != null && (Keys.spaceKey.wasPressedThisFrame || Keys.nKey.wasPressedThisFrame)) ||
            (Pad != null && Pad.buttonNorth.wasPressedThisFrame);

        public bool Upgrade =>
            (Keys != null && Keys.uKey.wasPressedThisFrame) || (Pad != null && Pad.buttonWest.wasPressedThisFrame);

        public bool Sell =>
            (Keys != null && (Keys.xKey.wasPressedThisFrame || Keys.deleteKey.wasPressedThisFrame)) ||
            (Pad != null && Pad.leftTrigger.wasPressedThisFrame);

        public bool CycleTarget =>
            (Keys != null && Keys.tKey.wasPressedThisFrame) || (Pad != null && Pad.rightTrigger.wasPressedThisFrame);

        public bool Speed =>
            (Keys != null && Keys.fKey.wasPressedThisFrame) || (Pad != null && Pad.selectButton.wasPressedThisFrame);

        public bool Pause =>
            (Keys != null && (Keys.escapeKey.wasPressedThisFrame || Keys.pKey.wasPressedThisFrame)) ||
            (Pad != null && Pad.startButton.wasPressedThisFrame);

        public bool Mute => Keys != null && Keys.mKey.wasPressedThisFrame;

        // ---- Menus ----

        public bool Confirm =>
            (Keys != null && (Keys.enterKey.wasPressedThisFrame || Keys.numpadEnterKey.wasPressedThisFrame || Keys.spaceKey.wasPressedThisFrame)) ||
            (Pad != null && Pad.buttonSouth.wasPressedThisFrame);

        public bool Back =>
            (Keys != null && (Keys.escapeKey.wasPressedThisFrame || Keys.backspaceKey.wasPressedThisFrame)) ||
            (Pad != null && Pad.buttonEast.wasPressedThisFrame);

        public bool Restart =>
            (Keys != null && Keys.rKey.wasPressedThisFrame) || (Pad != null && Pad.buttonNorth.wasPressedThisFrame);

        public bool ToMenu =>
            (Keys != null && Keys.qKey.wasPressedThisFrame) || (Pad != null && Pad.buttonWest.wasPressedThisFrame);

        /// <summary>-1 or +1 for a left or right step through a menu.</summary>
        public int MenuStep()
        {
            Vector2Int step = CursorStep();
            return step.x;
        }
    }
}
