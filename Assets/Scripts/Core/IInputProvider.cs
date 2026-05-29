using UnityEngine;

namespace DeepSea.Core
{
    /// <summary>
    /// Abstract interface for gathering user inputs.
    /// This decouples the game logic from the specific Unity Input System backend,
    /// making it highly testable and extensible (e.g. for AI controlling the player, or unit testing).
    /// </summary>
    public interface IInputProvider
    {
        /// <summary>
        /// Returns the horizontal (X, Y) movement direction.
        /// Vector is clamped between -1 and 1.
        /// </summary>
        Vector2 GetMovementInput();

        /// <summary>
        /// Returns the vertical depth input.
        /// +1.0f represents ascending (going towards surface).
        /// -1.0f represents descending (diving deeper).
        /// </summary>
        float GetDepthInput();

        /// <summary>
        /// Returns true if the interaction button (e.g. Space or 'E') was pressed down in this frame.
        /// </summary>
        bool IsInteractionPressed();
    }
}
