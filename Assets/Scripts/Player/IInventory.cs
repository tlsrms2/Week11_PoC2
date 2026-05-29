using DeepSea.Data;

namespace DeepSea.Player
{
    /// <summary>
    /// Decoupled interface for player inventory systems.
    /// Allows gatherables to interact with the player inventory without direct references.
    /// </summary>
    public interface IInventory
    {
        /// <summary>
        /// Attempts to add a resource to the inventory net (망사리).
        /// Returns true if successfully added, false if inventory is full.
        /// </summary>
        bool AddResource(ResourceData resource);

        /// <summary>
        /// Gets the current accumulated weight of all carried resources.
        /// </summary>
        float GetCurrentWeight();

        /// <summary>
        /// Checks if the inventory is currently at its maximum weight capacity.
        /// </summary>
        bool IsFull();
    }
}
