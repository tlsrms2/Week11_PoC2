using UnityEngine;
using DeepSea.Data;
using DeepSea.Player;

namespace DeepSea.Interaction
{
    /// <summary>
    /// Base component for gatherable items in the sea (e.g. seaweed, abalone, clams).
    /// Automatically applies DepthLayerFilter to restrict interactions to matching vertical layers.
    /// </summary>
    [RequireComponent(typeof(DepthLayerFilter))]
    [RequireComponent(typeof(Collider))]
    public class GatherableResource : MonoBehaviour, IGatherable
    {
        [Header("Resource Configuration")]
        [SerializeField] private ResourceData resourceData;

        [Header("Visual References")]
        [Tooltip("The SpriteRenderer which renders the resource sprite. Safe if nested.")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        private bool isGathered = false;

        private void Start()
        {
            if (resourceData == null)
            {
                Debug.LogError($"[GatherableResource] ResourceData is missing on {gameObject.name}!");
                return;
            }

            // Bind the visual sprite from ScriptableObject if spriteRenderer is assigned.
            if (spriteRenderer != null && resourceData.sprite != null)
            {
                spriteRenderer.sprite = resourceData.sprite;
            }
        }

        public void Gather(GameObject gatherer)
        {
            if (isGathered) return;

            // Find IInventory interface on the gatherer
            IInventory inventory = gatherer.GetComponent<IInventory>();
            if (inventory == null)
            {
                // Backup check: children or parents
                inventory = gatherer.GetComponentInChildren<IInventory>();
            }

            if (inventory != null)
            {
                if (inventory.IsFull())
                {
                    Debug.Log("[GatherableResource] Inventory is full! Cannot collect.");
                    return;
                }

                isGathered = true;
                bool success = inventory.AddResource(resourceData);

                if (success)
                {
                    // Trigger visual gathering feedback (e.g. play particle, float up, fade away)
                    PlayGatherEffect();

                    // Destroy object after gather completes
                    Destroy(gameObject, 0.1f);
                }
                else
                {
                    isGathered = false;
                }
            }
            else
            {
                Debug.LogWarning("[GatherableResource] The gatherer does not possess an IInventory component.");
            }
        }

        public float GetGatherTime()
        {
            return resourceData != null ? resourceData.gatherTime : 1.0f;
        }

        public ResourceData GetResourceData()
        {
            return resourceData;
        }

        private void PlayGatherEffect()
        {
            // Simple visual/audio placeholder hooks for the senior coder to polish later.
            // E.g., AudioSource.PlayClipAtPoint(...)
            // Instantiate collection particles.
            Debug.Log($"[GatherableResource] {resourceData.resourceName} successfully gathered!");
        }
    }
}
