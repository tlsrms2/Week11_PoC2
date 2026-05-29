using UnityEngine;

namespace DeepSea.Data
{
    [CreateAssetMenu(fileName = "PlayerData", menuName = "DeepSea/PlayerData", order = 1)]
    public class PlayerData : ScriptableObject
    {
        [Header("Movement Settings")]
        [Tooltip("Horizontal (X, Y) movement speed.")]
        public float baseMoveSpeed = 5.0f;
        
        [Tooltip("Vertical (Z) descent/ascent speed.")]
        public float baseDepthSpeed = 3.0f;

        [Header("Oxygen & Survival Settings")]
        [Tooltip("Maximum oxygen capacity.")]
        public float maxOxygen = 100f;
        
        [Tooltip("Standard oxygen consumption rate per second.")]
        public float baseOxygenDepletionRate = 1.0f;

        [Tooltip("Additional oxygen consumed when moving.")]
        public float movementOxygenMultiplier = 1.2f;

        [Tooltip("Additional oxygen consumed when ascending.")]
        public float ascentOxygenMultiplier = 1.5f;

        [Header("Inventory & Weight Settings")]
        [Tooltip("Maximum carry weight in the inventory net (망사리).")]
        public float maxCarryWeight = 50.0f;

        [Tooltip("How much weight affects oxygen depletion rate. (DepletionRate = Base + Weight * Penalty)")]
        public float weightOxygenPenaltyFactor = 0.05f;

        [Tooltip("How much weight affects movement speed. (Speed = Base - Weight * Penalty)")]
        public float weightSpeedPenaltyFactor = 0.03f;

        [Header("Upgrade Levels (Current Status)")]
        public int oxygenUpgradeLevel = 1;
        public int capacityUpgradeLevel = 1;
        public int speedUpgradeLevel = 1;

        /// <summary>
        /// Calculates the dynamic maximum oxygen based on the upgrade level.
        /// </summary>
        public float GetMaxOxygen()
        {
            // Level 1: 100, Level 2: 130, Level 3: 160, etc.
            return maxOxygen + (oxygenUpgradeLevel - 1) * 30f;
        }

        /// <summary>
        /// Calculates the dynamic move speed based on the upgrade level.
        /// </summary>
        public float GetMoveSpeed()
        {
            // Level 1: 5.0, Level 2: 5.8, Level 3: 6.6, etc.
            return baseMoveSpeed + (speedUpgradeLevel - 1) * 0.8f;
        }

        /// <summary>
        /// Calculates the dynamic inventory capacity based on the upgrade level.
        /// </summary>
        public float GetMaxCarryWeight()
        {
            // Level 1: 50, Level 2: 75, Level 3: 100, etc.
            return maxCarryWeight + (capacityUpgradeLevel - 1) * 25f;
        }
    }
}
