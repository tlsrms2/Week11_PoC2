using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using DeepSea.Core;

namespace DeepSea.Player
{
    /// <summary>
    /// Robust implementation of IInputProvider utilizing Unity's New Input System.
    /// Includes a fallback to standard legacy Input for maximum reliability.
    /// </summary>
    public class UnityInputProvider : MonoBehaviour, IInputProvider
    {
        [Header("Keyboard Bindings (Fallback / Direct)")]
        [SerializeField] private Key ascendKey = Key.Space;
        [SerializeField] private Key descendKey = Key.LeftCtrl;
        [SerializeField] private Key interactKey = Key.E;

        public Vector2 GetMovementInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                float x = 0f;
                float y = 0f;

                if (Keyboard.current[Key.W].isPressed || Keyboard.current[Key.UpArrow].isPressed) y += 1f;
                if (Keyboard.current[Key.S].isPressed || Keyboard.current[Key.DownArrow].isPressed) y -= 1f;
                if (Keyboard.current[Key.A].isPressed || Keyboard.current[Key.LeftArrow].isPressed) x -= 1f;
                if (Keyboard.current[Key.D].isPressed || Keyboard.current[Key.RightArrow].isPressed) x += 1f;

                Vector2 input = new Vector2(x, y);
                if (input.sqrMagnitude > 1f)
                {
                    input.Normalize();
                }
                return input;
            }
#endif
            // Legacy Fallback
            float legacyX = Input.GetAxisRaw("Horizontal");
            float legacyY = Input.GetAxisRaw("Vertical");
            Vector2 legacyInput = new Vector2(legacyX, legacyY);
            if (legacyInput.sqrMagnitude > 1f)
            {
                legacyInput.Normalize();
            }
            return legacyInput;
        }

        public float GetDepthInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                float depth = 0f;
                // Ascend (towards surface, i.e., moving Z closer to 0)
                if (Keyboard.current[ascendKey].isPressed)
                {
                    depth += 1f;
                }
                // Descend (diving deeper, i.e., moving Z more negative)
                if (Keyboard.current[descendKey].isPressed)
                {
                    depth -= 1f;
                }
                return depth;
            }
#endif
            // Legacy Fallback
            float legacyDepth = 0f;
            if (Input.GetKey(KeyCode.Space))
            {
                legacyDepth += 1f;
            }
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C))
            {
                legacyDepth -= 1f;
            }
            return legacyDepth;
        }

        public bool IsInteractionPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                return Keyboard.current[interactKey].wasPressedThisFrame;
            }
#endif
            // Legacy Fallback
            return Input.GetKeyDown(KeyCode.E);
        }
    }
}
