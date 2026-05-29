using UnityEngine;

namespace DeepSea.Core
{
    /// <summary>
    /// Senior-level Perspective Camera Controller designed for Top-down Depth mechanics.
    /// Standard 2D coordinates (X: Left/Right, Y: Up/Down) are maintained while
    /// the Z-axis represents the depth (0 is surface, negative is deeper).
    /// The camera looks straight forward along the Z-axis (Rotation = 0, 0, 0) with a negative Z offset,
    /// eliminating any axis inversion or projection issues.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class DepthCameraController : MonoBehaviour
    {
        [Header("Target Tracking")]
        [Tooltip("The player transform to follow.")]
        [SerializeField] private Transform target;

        [Tooltip("Smoothing factor for horizontal/vertical movement tracking.")]
        [SerializeField] private float movementSmoothTime = 0.15f;

        [Tooltip("Smoothing factor for depth (Z) movement tracking.")]
        [SerializeField] private float depthSmoothTime = 0.3f;

        [Header("Offset Settings")]
        [Tooltip("The camera's distance back from the player along the Z axis.")]
        [SerializeField] private float followDistance = 10f;

        // Current velocity vectors for SmoothDamp
        private Vector3 currentVelocity = Vector3.zero;

        private void Start()
        {
            // Set rotation to (0, 180, 0) to look straight down the negative Z-axis (deeper water)
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            if (target != null)
            {
                // Instantly snap to the target on start.
                Vector3 targetPos = target.position;
                targetPos.z += followDistance; // Camera stays shallower (Z+) to look deeper (Z-)
                transform.position = targetPos;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Target camera position (Shallower Z than target)
            Vector3 targetCamPos = target.position;
            targetCamPos.z += followDistance;

            // Apply different smoothing settings for XY (gameplay movement) and Z (depth transitions)
            Vector3 currentPos = transform.position;
            
            float newX = Mathf.SmoothDamp(currentPos.x, targetCamPos.x, ref currentVelocity.x, movementSmoothTime);
            float newY = Mathf.SmoothDamp(currentPos.y, targetCamPos.y, ref currentVelocity.y, movementSmoothTime);
            float newZ = Mathf.SmoothDamp(currentPos.z, targetCamPos.z, ref currentVelocity.z, depthSmoothTime);

            transform.position = new Vector3(newX, newY, newZ);
        }

        /// <summary>
        /// Explicitly sets the target to track.
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
