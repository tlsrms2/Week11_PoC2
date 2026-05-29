using System;
using System.Collections.Generic;
using UnityEngine;
using DeepSea.Data;

namespace DeepSea.Player
{
    /// <summary>
    /// Senior-level Inventory System implementing IInventory.
    /// Tracks gathered resources, total carry weight, and potential gold values.
    /// Dispatches events when inventory changes, allowing PlayerController and UI to adapt immediately.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class InventorySystem : MonoBehaviour, IInventory
    {
        [Header("Configurations")]
        [SerializeField] private PlayerData playerData;

        // Internal Item lists
        private readonly List<ResourceData> carriedItems = new List<ResourceData>();

        // State metrics
        public float CurrentWeight { get; private set; } = 0f;
        public int TotalPotentialGold { get; private set; } = 0;

        // Events
        public event Action OnInventoryChanged;
        public event Action<int> OnItemsSold; // Passes the gold earned

        private PlayerController playerController;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
        }

        private void Start()
        {
            if (playerData == null)
            {
                Debug.LogError("[InventorySystem] PlayerData ScriptableObject is missing!");
            }
        }

        public bool AddResource(ResourceData resource)
        {
            if (resource == null) return false;

            float maxWeight = playerData != null ? playerData.GetMaxCarryWeight() : 50f;

            // Strict weight capacity check
            if (CurrentWeight + resource.weight > maxWeight)
            {
                Debug.Log("[InventorySystem] Cannot add item. Exceeds weight limit.");
                return false;
            }

            carriedItems.Add(resource);
            RecalculateInventory();

            return true;
        }

        public float GetCurrentWeight() => CurrentWeight;

        public bool IsFull()
        {
            float maxWeight = playerData != null ? playerData.GetMaxCarryWeight() : 50f;
            // Standard small margin check for float comparison
            return CurrentWeight >= maxWeight - 0.1f;
        }

        /// <summary>
        /// Clears all carried resources and returns the accumulated gold earned.
        /// Typically triggered when the player returns to the sea surface.
        /// </summary>
        public void ClearAndSellItems()
        {
            if (carriedItems.Count == 0) return;

            int earnedGold = TotalPotentialGold;
            
            carriedItems.Clear();
            RecalculateInventory();

            OnItemsSold?.Invoke(earnedGold);
            Debug.Log($"[InventorySystem] Sold all items for {earnedGold} Gold!");
        }

        private void RecalculateInventory()
        {
            float newWeight = 0f;
            int newGold = 0;

            foreach (var item in carriedItems)
            {
                newWeight += item.weight;
                newGold += item.goldValue;
            }

            CurrentWeight = newWeight;
            TotalPotentialGold = newGold;

            // Apply speed penalty directly to player controller
            if (playerController != null && playerData != null)
            {
                // Speed = BaseSpeed - (Weight * SpeedPenaltyFactor)
                // Expressed as a multiplier: SpeedMultiplier = 1.0f - (Weight * PenaltyFactor / BaseSpeed)
                float penalty = CurrentWeight * playerData.weightSpeedPenaltyFactor;
                float baseSpeed = playerData.GetMoveSpeed();
                float multiplier = Mathf.Max(0.2f, 1.0f - (penalty / baseSpeed));
                
                playerController.UpdateSpeedMultiplier(multiplier);
            }

            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// Read-only access to currently carried items list.
        /// </summary>
        public IReadOnlyList<ResourceData> CarriedItems => carriedItems;
    }
}
