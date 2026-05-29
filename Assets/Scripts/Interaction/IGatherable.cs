using DeepSea.Data;
using UnityEngine;

namespace DeepSea.Interaction
{
    /// <summary>
    /// Interface for any object that can be gathered or collected in the sea.
    /// Decouples the player gathering action from specific resource implementations.
    /// </summary>
    public interface IGatherable
    {
        /// <summary>
        /// Initiates the gathering process by the given gatherer GameObject.
        /// </summary>
        void Gather(GameObject gatherer);

        /// <summary>
        /// Returns the seconds required to successfully gather the resource.
        /// </summary>
        float GetGatherTime();

        /// <summary>
        /// Gets the ScriptableObject resource metadata.
        /// </summary>
        ResourceData GetResourceData();
    }
}
