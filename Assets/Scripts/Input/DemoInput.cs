using UnityEngine;
using UnityEngine.InputSystem;

namespace ShatteredForge.Input
{
    /// <summary>
    /// Minimal Input System helpers for the prototype (Player Settings: Input System only).
    /// </summary>
    public static class DemoInput
    {
        public static bool KeyDown(Key key)
        {
            return Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
        }

        /// <summary>
        /// WASD / arrows + left stick. XZ plane, magnitude clamped to 1.
        /// </summary>
        public static Vector3 ReadMoveXZ()
        {
            var h = 0f;
            var v = 0f;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
                {
                    h -= 1f;
                }

                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
                {
                    h += 1f;
                }

                if (kb.wKey.isPressed || kb.upArrowKey.isPressed)
                {
                    v += 1f;
                }

                if (kb.sKey.isPressed || kb.downArrowKey.isPressed)
                {
                    v -= 1f;
                }
            }

            var gp = Gamepad.current;
            if (gp != null)
            {
                var s = gp.leftStick.ReadValue();
                h += s.x;
                v += s.y;
            }

            var move = new Vector3(h, 0f, v);
            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }

            return move;
        }
    }
}
