using UnityEngine;
using DeepSea.Core;
using DeepSea.Data;

namespace DeepSea.Player
{
    /// <summary>
    /// Senior-level Player Controller.
    /// Manages 2D horizontal movement (X, Y) and vertical depth movement (Z) via Rigidbody.
    /// Adapts player speed based on equipment upgrades and inventory weight.
    /// Strictly limits the depth between sea level (Z = 0) and deep ocean.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(IInputProvider))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Data Configurations")]
        [SerializeField] private PlayerData playerData;

        [Header("Depth Constraints")]
        [Tooltip("Maximum allowed depth (represented as a negative Z value).")]
        [SerializeField] private float maxDepthLimit = -200f;

        // Dependencies
        private IInputProvider inputProvider;
        private Rigidbody rb;

        // Dynamic State modifiers
        private float speedMultiplier = 1.0f;
        private bool isInputEnabled = true;

        // Current status cache
        public float CurrentSpeed { get; private set; }
        public bool IsMoving { get; private set; }

        /// <summary>
        /// Gets the absolute maximum depth limit in positive meters.
        /// </summary>
        public float MaxDepthLimitInMeters => Mathf.Abs(maxDepthLimit);

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            inputProvider = GetComponent<IInputProvider>();

            // Setup Rigidbody for top-down constraints.
            // Rigidbody shouldn't be affected by engine gravity since we manually simulate floating/sinking.
            rb.useGravity = false;
            rb.linearDamping = 1.0f; // Soft natural stopping drag
            rb.angularDamping = 999f; // Prevent unwanted rotation spin
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        private void Start()
        {
            if (playerData == null)
            {
                Debug.LogError("[PlayerController] PlayerData ScriptableObject reference is missing!");
            }
        }

        private void FixedUpdate()
        {
            if (!isInputEnabled)
            {
                // Smoothly decelerate if input is disabled.
                rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 5f);
                IsMoving = false;
                CurrentSpeed = 0f;
                return;
            }

            // 1. Gather inputs
            Vector2 moveInput = inputProvider.GetMovementInput();
            float depthInput = inputProvider.GetDepthInput();

            // 2. Calculate dynamic speeds based on current stats and upgrades
            float targetMoveSpeed = playerData != null ? playerData.GetMoveSpeed() : 5f;
            float targetDepthSpeed = playerData != null ? playerData.baseDepthSpeed : 3f;

            // Apply dynamic speed multipliers (e.g. from weight or active effects)
            targetMoveSpeed *= speedMultiplier;

            // 3. Process movement physics
            // Horizontal movement (X, Y) relative to Camera View Space to prevent axis inversion
            Vector3 targetVelocity = Vector3.zero;
            Camera mainCam = Camera.main;

            if (mainCam != null)
            {
                // Align inputs with camera view plane (eliminates 180-degree rotation inversion)
                Vector3 camRight = mainCam.transform.right;
                Vector3 camUp = mainCam.transform.up;

                // Strip out Z components to keep movement purely on the XY plane
                camRight.z = 0f;
                camUp.z = 0f;
                camRight.Normalize();
                camUp.Normalize();

                Vector3 horizontalVelocity = (camRight * moveInput.x + camUp * moveInput.y) * targetMoveSpeed;
                targetVelocity = new Vector3(horizontalVelocity.x, horizontalVelocity.y, 0f);
            }
            else
            {
                // Fallback if no camera found
                targetVelocity = new Vector3(moveInput.x * targetMoveSpeed, moveInput.y * targetMoveSpeed, 0f);
            }

            // Vertical depth movement (Z)
            // depthInput is +1 for ascending (moving Z towards 0), -1 for descending (moving Z towards negative)
            targetVelocity.z = depthInput * targetDepthSpeed;

            // Apply velocity
            rb.linearVelocity = targetVelocity;

            // 4. Enforce strict depth boundaries (Z = 0 is surface, Z = maxDepthLimit is deepest)
            Vector3 currentPos = transform.position;
            if (currentPos.z > 0f)
            {
                currentPos.z = 0f;
                transform.position = currentPos;
                if (rb.linearVelocity.z > 0f)
                {
                    // Stop upward velocity when hitting the surface
                    Vector3 vel = rb.linearVelocity;
                    vel.z = 0f;
                    rb.linearVelocity = vel;
                }
            }
            else if (currentPos.z < maxDepthLimit)
            {
                currentPos.z = maxDepthLimit;
                transform.position = currentPos;
                if (rb.linearVelocity.z < 0f)
                {
                    // Stop downward velocity when hitting the bottom limit
                    Vector3 vel = rb.linearVelocity;
                    vel.z = 0f;
                    rb.linearVelocity = vel;
                }
            }

            // Update state metrics
            CurrentSpeed = rb.linearVelocity.magnitude;
            IsMoving = moveInput.sqrMagnitude > 0.01f || Mathf.Abs(depthInput) > 0.01f;
        }

        /// <summary>
        /// Controls whether player input is processed. Useful during cutscenes, death, or gathering.
        /// </summary>
        public void SetInputActive(bool active)
        {
            isInputEnabled = active;
            if (!active)
            {
                rb.linearVelocity = Vector3.zero;
            }
        }

        /// <summary>
        /// Updates the dynamic movement speed multiplier (e.g., set by inventory weight penalty).
        /// </summary>
        public void UpdateSpeedMultiplier(float multiplier)
        {
            speedMultiplier = Mathf.Clamp(multiplier, 0.1f, 2.0f);
        }

        /// <summary>
        /// Instantly snaps the player to the surface (Z = 0).
        /// </summary>
        public void TeleportToSurface()
        {
            Vector3 pos = transform.position;
            pos.z = 0f;
            transform.position = pos;
            rb.linearVelocity = Vector3.zero;
        }
    }
}
