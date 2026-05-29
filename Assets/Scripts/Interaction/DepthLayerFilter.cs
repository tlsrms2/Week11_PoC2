using UnityEngine;
using DeepSea.Environment;

namespace DeepSea.Interaction
{
    /// <summary>
    /// Senior-level depth-matching optimization filter.
    /// Subscribes to DepthManager's depth-change events rather than running heavy checks in Update().
    /// Activates/deactivates the object's 3D Colliders based on whether it shares the same vertical Z-layer as the player.
    /// This prevents unrealistic physical collisions between objects on different vertical depths.
    /// </summary>
    public class DepthLayerFilter : MonoBehaviour
    {
        [Header("Depth Tolerance Settings")]
        [Tooltip("The allowed Z-distance margin (in units) within which this object is considered to be on the same layer as the player.")]
        [SerializeField] private float layerTolerance = 0.5f;

        [Tooltip("If true, completely disables/enables the collider component based on depth match.")]
        [SerializeField] private bool manageCollider = true;

        [Tooltip("If true, fade sprite alpha when the object is at a different depth.")]
        [SerializeField] private bool applyVisualFade = true;

        // Internal caching
        private Collider myCollider;
        private SpriteRenderer mySpriteRenderer;
        private float myDepthZ;

        private void Awake()
        {
            myCollider = GetComponent<Collider>();
            mySpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
            // The object's static depth is its Z coordinate.
            myDepthZ = transform.position.z;
        }

        private void OnEnable()
        {
            if (DepthManager.Instance != null)
            {
                DepthManager.Instance.OnDepthChanged += OnPlayerDepthChanged;
                // Run an initial evaluation
                EvaluateDepthMatching(DepthManager.Instance.CurrentDepth);
            }
        }

        private void OnDisable()
        {
            if (DepthManager.Instance != null)
            {
                DepthManager.Instance.OnDepthChanged -= OnPlayerDepthChanged;
            }
        }

        private void OnPlayerDepthChanged(float playerDepth)
        {
            EvaluateDepthMatching(playerDepth);
        }

        private void EvaluateDepthMatching(float playerDepth)
        {
            // Player depth m = -playerZ.
            // Player Z = -playerDepth.
            float playerZ = -playerDepth;
            float zDifference = Mathf.Abs(myDepthZ - playerZ);

            bool isSameLayer = zDifference <= layerTolerance;

            // 1. Manage Collider activation to prevent out-of-depth physics collisions
            if (manageCollider && myCollider != null)
            {
                myCollider.enabled = isSameLayer;
            }

            // 2. Manage Visual Representation based on depth proximity
            if (applyVisualFade && mySpriteRenderer != null)
            {
                Color color = mySpriteRenderer.color;

                if (isSameLayer)
                {
                    color.a = 1.0f; // Solid
                }
                else
                {
                    // Fade out proportionally as distance increases
                    // Max distance fade clamp: fully transparent or faint shadow beyond 5.0 units.
                    float fadeFactor = Mathf.Clamp01(1.0f - (zDifference / 5.0f));
                    color.a = Mathf.Max(0.15f, fadeFactor * 0.5f); // Keep a faint ghost silhouette
                }

                mySpriteRenderer.color = color;
            }
        }

        /// <summary>
        /// Recalculates static depth. Call this if the object moves dynamically along the Z-axis.
        /// </summary>
        public void RecalculateMyDepth()
        {
            myDepthZ = transform.position.z;
            if (DepthManager.Instance != null)
            {
                EvaluateDepthMatching(DepthManager.Instance.CurrentDepth);
            }
        }
    }
}
