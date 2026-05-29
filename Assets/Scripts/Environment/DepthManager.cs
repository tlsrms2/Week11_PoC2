using System;
using UnityEngine;

namespace DeepSea.Environment
{
    /// <summary>
    /// Mid-level central manager that tracks the player's current depth,
    /// converts Z-coordinates to meters, and dispatches events when depth or depth zones change.
    /// This acts as a decoupled bridge for visual effects, UI, and enemy AIs.
    /// </summary>
    public class DepthManager : MonoBehaviour
    {
        public static DepthManager Instance { get; private set; }

        [Header("Tracking Target")]
        [Tooltip("The player transform to measure depth from.")]
        [SerializeField] private Transform playerTransform;

        [Header("Zone Thresholds (in meters)")]
        [Tooltip("Depth where Mid zone starts.")]
        [SerializeField] private float midZoneThreshold = 30f;
        
        [Tooltip("Depth where Deep zone starts.")]
        [SerializeField] private float deepZoneThreshold = 80f;

        // Current states
        public float CurrentDepth { get; private set; } = 0f;
        public DepthZone CurrentZone { get; private set; } = DepthZone.Shallow;

        // Events
        public event Action<float> OnDepthChanged;
        public event Action<DepthZone> OnZoneChanged;

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

        private void Update()
        {
            if (playerTransform == null) return;

            // Player Z-coordinate goes negative as they go deeper.
            // Depth is represented as positive meters: depth = -z.
            float calculatedDepth = Mathf.Max(0f, -playerTransform.position.z);

            if (!Mathf.Approximately(calculatedDepth, CurrentDepth))
            {
                CurrentDepth = calculatedDepth;
                OnDepthChanged?.Invoke(CurrentDepth);

                UpdateDepthZone();
            }
        }

        private void UpdateDepthZone()
        {
            DepthZone newZone = DepthZone.Shallow;

            if (CurrentDepth >= deepZoneThreshold)
            {
                newZone = DepthZone.Deep;
            }
            else if (CurrentDepth >= midZoneThreshold)
            {
                newZone = DepthZone.Mid;
            }

            if (newZone != CurrentZone)
            {
                CurrentZone = newZone;
                OnZoneChanged?.Invoke(CurrentZone);
            }
        }

        /// <summary>
        /// Explicitly register the player transform if spawned dynamically.
        /// </summary>
        public void RegisterPlayer(Transform player)
        {
            playerTransform = player;
        }

        /// <summary>
        /// Returns the standard threshold values.
        /// </summary>
        public float MidZoneThreshold => midZoneThreshold;
        public float DeepZoneThreshold => deepZoneThreshold;
    }
}
