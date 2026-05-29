using System;
using UnityEngine;
using DeepSea.Data;
using DeepSea.Environment;

namespace DeepSea.Player
{
    /// <summary>
    /// Senior-level Player Survival System.
    /// Manages oxygen levels dynamically using state variables (movement, ascent, carry weight).
    /// Subscribes to DepthManager events to automatically trigger surface refills and resource selling when Z reaches 0.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(InventorySystem))]
    public class PlayerSurvival : MonoBehaviour
    {
        [Header("Configurations")]
        [SerializeField] private PlayerData playerData;

        // Current status parameters
        public float CurrentOxygen { get; private set; }
        public bool IsDead { get; private set; } = false;

        // Events
        public event Action<float, float> OnOxygenChanged; // (current, max)
        public event Action OnOxygenDepleted;
        public event Action OnSurfaced; // Triggered when player reaches 0m depth

        // Dependencies
        private PlayerController playerController;
        private InventorySystem inventorySystem;
        private Rigidbody playerRb;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            inventorySystem = GetComponent<InventorySystem>();
            playerRb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            if (playerData == null)
            {
                Debug.LogError("[PlayerSurvival] PlayerData is missing!");
                return;
            }

            // Set initial oxygen to max
            CurrentOxygen = playerData.GetMaxOxygen();

            // Listen to Depth changes from DepthManager to trigger surfacing mechanics
            if (DepthManager.Instance != null)
            {
                DepthManager.Instance.OnDepthChanged += HandleDepthSurfacing;
            }
        }

        private void OnDestroy()
        {
            if (DepthManager.Instance != null)
            {
                DepthManager.Instance.OnDepthChanged -= HandleDepthSurfacing;
            }
        }

        private void Update()
        {
            if (IsDead || playerData == null) return;

            ConsumeOxygen();
        }

        private void ConsumeOxygen()
        {
            // 1. Calculate base depletion rate based on current state
            float depletionRate = playerData.baseOxygenDepletionRate;

            if (playerController.IsMoving)
            {
                depletionRate *= playerData.movementOxygenMultiplier;
            }

            // Check if ascending (Z velocity > 0 means moving towards 0 depth)
            if (playerRb != null && playerRb.linearVelocity.z > 0.05f)
            {
                depletionRate *= playerData.ascentOxygenMultiplier;
            }

            // 2. Add weight-based penalty to oxygen consumption
            float carryWeight = inventorySystem.GetCurrentWeight();
            float weightPenalty = carryWeight * playerData.weightOxygenPenaltyFactor;
            depletionRate += weightPenalty;

            // 3. Apply oxygen depletion
            CurrentOxygen -= depletionRate * Time.deltaTime;
            CurrentOxygen = Mathf.Clamp(CurrentOxygen, 0f, playerData.GetMaxOxygen());

            // Broadcast change
            OnOxygenChanged?.Invoke(CurrentOxygen, playerData.GetMaxOxygen());

            // 4. Check for depletion death
            if (CurrentOxygen <= 0f && !IsDead)
            {
                TriggerDeath();
            }
        }

        private void HandleDepthSurfacing(float depth)
        {
            // Surfacing occurs at exactly 0 depth
            if (depth <= 0.01f && !IsDead)
            {
                RefillOxygen();
                inventorySystem.ClearAndSellItems();
                OnSurfaced?.Invoke();
            }
        }

        /// <summary>
        /// Instantly refills oxygen to the maximum capacity based on upgrade level.
        /// </summary>
        public void RefillOxygen()
        {
            if (playerData == null || IsDead) return;

            float maxOx = playerData.GetMaxOxygen();
            if (CurrentOxygen < maxOx)
            {
                CurrentOxygen = maxOx;
                OnOxygenChanged?.Invoke(CurrentOxygen, maxOx);
                Debug.Log("[PlayerSurvival] Oxygen fully refilled at surface.");
            }
        }

        /// <summary>
        /// Decreases oxygen directly (e.g. from an enemy attack).
        /// </summary>
        public void DamageOxygen(float amount)
        {
            if (IsDead) return;

            CurrentOxygen -= amount;
            CurrentOxygen = Mathf.Clamp(CurrentOxygen, 0f, playerData.GetMaxOxygen());
            OnOxygenChanged?.Invoke(CurrentOxygen, playerData.GetMaxOxygen());

            Debug.Log($"[PlayerSurvival] Took {amount} oxygen damage. Remaining: {CurrentOxygen}");

            // Cancel any active gathering if damaged
            PlayerInteractionController pic = GetComponent<PlayerInteractionController>();
            if (pic != null)
            {
                pic.CancelGathering();
            }

            if (CurrentOxygen <= 0f)
            {
                TriggerDeath();
            }
        }

        private void TriggerDeath()
        {
            IsDead = true;
            playerController.SetInputActive(false);
            OnOxygenDepleted?.Invoke();
            Debug.LogError("[PlayerSurvival] Oxygen depleted! Player has drowned.");
            
            // Proactive Senior logic: Teleport player back to surface as penalty, or reset.
            // For now, let's keep them dead, and provide a resurrection/reset function for the game manager.
        }

        /// <summary>
        /// Revives the player with full oxygen at the surface.
        /// </summary>
        public void RevivePlayer()
        {
            IsDead = false;
            CurrentOxygen = playerData != null ? playerData.GetMaxOxygen() : 100f;
            playerController.TeleportToSurface();
            playerController.SetInputActive(true);
            OnOxygenChanged?.Invoke(CurrentOxygen, playerData != null ? playerData.GetMaxOxygen() : 100f);
            Debug.Log("[PlayerSurvival] Player revived and returned to the surface.");
        }
    }
}
