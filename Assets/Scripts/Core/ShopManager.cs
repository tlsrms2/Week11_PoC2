using System;
using UnityEngine;
using DeepSea.Data;
using DeepSea.Player;

namespace DeepSea.Core
{
    /// <summary>
    /// Senior-level Shop & Economy Manager.
    /// Manages the player's gold, validates upgrade purchases, and persists modifications inside PlayerData.
    /// Emits events for UI binders to cleanly update gold displays and upgrade shop options.
    /// </summary>
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        [Header("Economy Configurations")]
        [SerializeField] private PlayerData playerData;

        [Header("Upgrade Cost Formula Settings")]
        [Tooltip("Base cost for Level 1 -> Level 2 upgrade.")]
        [SerializeField] private int baseUpgradeCost = 100;
        
        [Tooltip("Cost multiplier per upgrade level: Cost = Base * (CurrentLevel * Multiplier)")]
        [SerializeField] private float costMultiplier = 1.5f;

        [Tooltip("Maximum allowed level for upgrades.")]
        [SerializeField] private int maxUpgradeLevel = 5;

        // Dynamic State
        public int CurrentGold { get; private set; } = 0;

        // Events
        public event Action<int> OnGoldChanged;
        public event Action OnUpgradeSuccess;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Robust connection: Listen to PlayerSurfaces surfaced and items sold.
            var inventory = FindFirstObjectByType<InventorySystem>();
            if (inventory != null)
            {
                inventory.OnItemsSold += AddGold;
            }
        }

        private void OnDestroy()
        {
            var inventory = FindFirstObjectByType<InventorySystem>();
            if (inventory != null)
            {
                inventory.OnItemsSold -= AddGold;
            }
        }

        /// <summary>
        /// Adds earned gold to the player's account.
        /// </summary>
        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            CurrentGold += amount;
            OnGoldChanged?.Invoke(CurrentGold);
            Debug.Log($"[ShopManager] Gold updated! +{amount} Gold. Current total: {CurrentGold} Gold.");
        }

        /// <summary>
        /// Attempts to purchase an Oxygen Upgrade.
        /// </summary>
        public bool BuyOxygenUpgrade()
        {
            if (playerData == null) return false;
            if (playerData.oxygenUpgradeLevel >= maxUpgradeLevel) return false;

            int cost = GetUpgradeCost(playerData.oxygenUpgradeLevel);
            if (CurrentGold >= cost)
            {
                CurrentGold -= cost;
                playerData.oxygenUpgradeLevel++;
                
                OnGoldChanged?.Invoke(CurrentGold);
                OnUpgradeSuccess?.Invoke();
                Debug.Log($"[ShopManager] Upgraded Oxygen to Level {playerData.oxygenUpgradeLevel}!");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Attempts to purchase an Inventory Capacity Upgrade.
        /// </summary>
        public bool BuyCapacityUpgrade()
        {
            if (playerData == null) return false;
            if (playerData.capacityUpgradeLevel >= maxUpgradeLevel) return false;

            int cost = GetUpgradeCost(playerData.capacityUpgradeLevel);
            if (CurrentGold >= cost)
            {
                CurrentGold -= cost;
                playerData.capacityUpgradeLevel++;

                OnGoldChanged?.Invoke(CurrentGold);
                OnUpgradeSuccess?.Invoke();
                Debug.Log($"[ShopManager] Upgraded Net Capacity to Level {playerData.capacityUpgradeLevel}!");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Attempts to purchase a Speed Upgrade.
        /// </summary>
        public bool BuySpeedUpgrade()
        {
            if (playerData == null) return false;
            if (playerData.speedUpgradeLevel >= maxUpgradeLevel) return false;

            int cost = GetUpgradeCost(playerData.speedUpgradeLevel);
            if (CurrentGold >= cost)
            {
                CurrentGold -= cost;
                playerData.speedUpgradeLevel++;

                OnGoldChanged?.Invoke(CurrentGold);
                OnUpgradeSuccess?.Invoke();
                Debug.Log($"[ShopManager] Upgraded Speed to Level {playerData.speedUpgradeLevel}!");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Calculates the dynamic upgrade cost based on the level.
        /// </summary>
        public int GetUpgradeCost(int currentLevel)
        {
            return Mathf.RoundToInt(baseUpgradeCost * (currentLevel * costMultiplier));
        }

        /// <summary>
        /// Direct access to PlayerData levels for UI.
        /// </summary>
        public PlayerData PlayerData => playerData;
        public int MaxUpgradeLevel => maxUpgradeLevel;
    }
}
