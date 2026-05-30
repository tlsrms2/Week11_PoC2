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

            // 탑뷰 물리 제약을 위한 리지드바디 조율
            // 플레이어가 직접 떠오르고 가라앉는 호흡을 모사하므로 기본 중력은 끕니다.
            rb.useGravity = false;
            rb.linearDamping = 1.0f; // 부드럽고 자연스럽게 감속되는 저항
            rb.angularDamping = 999f; // 불필요한 스핀 현상 방지
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            // 시니어 팁: 리지드바디 이동의 뚝뚝 끊김(Jitter) 떨림을 제거하기 위한 보간 처리 활성화
            rb.interpolation = RigidbodyInterpolation.Interpolate;
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

            // 4. 엄격한 수심 경계 제어 (Z = 0 해수면, Z = maxDepthLimit 최대 수심)
            // 시니어 팁: FixedUpdate 내에서 transform.position을 직접 덮어쓰면 물리 보간이 깨져 뚝뚝 끊기므로 rb.position을 세팅합니다.
            Vector3 currentPos = rb.position;
            if (currentPos.z > 0f)
            {
                currentPos.z = 0f;
                rb.position = currentPos;
                if (rb.linearVelocity.z > 0f)
                {
                    // 수면에 닿으면 상승 물리 속도를 0으로 제한
                    Vector3 vel = rb.linearVelocity;
                    vel.z = 0f;
                    rb.linearVelocity = vel;
                }
            }
            else if (currentPos.z < maxDepthLimit)
            {
                currentPos.z = maxDepthLimit;
                rb.position = currentPos;
                if (rb.linearVelocity.z < 0f)
                {
                    // 바닥 제한에 닿으면 하강 물리 속도를 0으로 제한
                    Vector3 vel = rb.linearVelocity;
                    vel.z = 0f;
                    rb.linearVelocity = vel;
                }
            }

            // 플레이어가 수평으로 이동 중인 방향을 향해 머리가 바라보도록 회전 처리
            // 원본 스프라이트의 헤드가 위쪽(Y+)을 향하고 있으므로 angle에서 90도를 빼 각도를 맞춰줍니다.
            if (moveInput.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 180f, angle - 90f);
            }

            // 상태 지표 갱신
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
