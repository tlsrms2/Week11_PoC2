using UnityEngine;
using DeepSea.Data;
using DeepSea.Player;
using DeepSea.Interaction;

namespace DeepSea.Enemy
{
    /// <summary>
    /// Senior-level Dynamic Depth Chasing Enemy AI.
    /// Manages proper visibility hiding, warning shadow projection, 
    /// smooth 360-degree rotation matching movement direction (right-headed sprite),
    /// and clean visual state synchronization on the camera billboard plane.
    /// </summary>
    [RequireComponent(typeof(DepthLayerFilter))]
    [RequireComponent(typeof(Collider))]
    public class DepthEnemyAI : MonoBehaviour
    {
        [Header("Data & Configuration")]
        [SerializeField] private EnemyData enemyData;
        
        [Header("Visual References")]
        [Tooltip("The main visual SpriteRenderer of the enemy fish.")]
        [SerializeField] private SpriteRenderer mainVisualSprite;

        [Header("Shadow Projection")]
        [Tooltip("The SpriteRenderer child used as the warning shadow decal on the player's layer.")]
        [SerializeField] private SpriteRenderer shadowDecal;

        [Tooltip("Maximum depth offset at which the shadow is projected.")]
        [SerializeField] private float maxShadowDistance = 15f;

        [Header("Patrol Settings")]
        [Tooltip("How long to swim in a direction before changing during patrol.")]
        [SerializeField] private float patrolDirectionChangeInterval = 3f;
        
        [Tooltip("Speed multiplier during casual patrol swimming.")]
        [SerializeField] private float patrolSpeedMultiplier = 0.4f;

        // Dependencies & States
        private Transform playerTransform;
        private PlayerSurvival playerSurvival;
        private DepthLayerFilter depthFilter;

        private Vector3 patrolDirection;
        private float patrolTimer;
        private bool isChasing = false;

        private void Awake()
        {
            depthFilter = GetComponent<DepthLayerFilter>();
        }

        private void Start()
        {
            if (enemyData == null)
            {
                Debug.LogError($"[DepthEnemyAI] EnemyData is missing on {gameObject.name}!");
                return;
            }

            // Bind the visual sprite from ScriptableObject if assigned.
            if (mainVisualSprite != null && enemyData.sprite != null)
            {
                mainVisualSprite.sprite = enemyData.sprite;
            }

            // Find player in scene
            var survival = FindFirstObjectByType<PlayerSurvival>();
            if (survival != null)
            {
                playerTransform = survival.transform;
                playerSurvival = survival;
            }

            if (shadowDecal != null)
            {
                shadowDecal.gameObject.SetActive(false);
            }

            // Initialize random patrol direction
            PickRandomPatrolDirection();
        }

        private void Update()
        {
            if (playerTransform == null || playerSurvival == null || playerSurvival.IsDead)
            {
                if (shadowDecal != null) shadowDecal.gameObject.SetActive(false);
                if (mainVisualSprite != null) mainVisualSprite.enabled = true; // Safety default
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            Vector3 moveDirection = Vector3.zero;
            
            // Determine behavior based on detection radius
            if (distanceToPlayer <= enemyData.detectionRadius)
            {
                isChasing = true;
                moveDirection = ChasePlayer();
            }
            else
            {
                isChasing = false;
                moveDirection = IdlePatrol();
            }

            // Apply proper 2D rotation matching movement direction
            ApplyFishRotation(moveDirection);

            // Handle depth visibility rules and warning shadow
            HandleZLayerVisualsAndShadow();
        }

        private Vector3 ChasePlayer()
        {
            Vector3 playerPos = playerTransform.position;
            Vector3 currentPos = transform.position;

            // 1. Move horizontally (X, Y) towards player
            Vector3 horizontalDir = new Vector3(playerPos.x - currentPos.x, playerPos.y - currentPos.y, 0f);
            if (horizontalDir.sqrMagnitude > 0.001f)
            {
                horizontalDir.Normalize();
                transform.Translate(horizontalDir * (enemyData.moveSpeed * Time.deltaTime), Space.World);
            }

            // 2. Transition vertically (Z) towards player's depth layer
            float newZ = Mathf.MoveTowards(currentPos.z, playerPos.z, enemyData.depthTransitionSpeed * Time.deltaTime);
            transform.position = new Vector3(transform.position.x, transform.position.y, newZ);

            if (depthFilter != null)
            {
                depthFilter.RecalculateMyDepth();
            }

            return horizontalDir;
        }

        private Vector3 IdlePatrol()
        {
            patrolTimer += Time.deltaTime;
            if (patrolTimer >= patrolDirectionChangeInterval)
            {
                PickRandomPatrolDirection();
            }

            // Translate horizontally during patrol
            float patrolSpeed = enemyData.moveSpeed * patrolSpeedMultiplier;
            transform.Translate(patrolDirection * (patrolSpeed * Time.deltaTime), Space.World);

            return patrolDirection;
        }

        private void PickRandomPatrolDirection()
        {
            patrolTimer = 0f;
            // Pure horizontal random vector
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            patrolDirection = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
        }

        /// <summary>
        /// Computes and applies smooth 2D rotation facing the movement direction.
        /// Preserves flat 2D billboard sprite perspective without squishing or skewing.
        /// Assumes the original fish sprite has its head facing RIGHT (X+).
        /// </summary>
        private void ApplyFishRotation(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.001f) return;

            // 1. Calculate angle on the 2D plane (X, Y)
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // 2. Check if we need to flip the fish horizontally to prevent swimming upside down
            // When moving left (angle between 90 and 270 or -90 and -270 degrees), we flip
            bool shouldFlip = Mathf.Abs(angle) > 90f;

            if (mainVisualSprite != null)
            {
                // Align perfectly parallel to the camera rotation plane (which is rotated Y:180 to look down deep-sea Z-)
                // Camera Y is 180. To billboard perfectly, we align our rotation.
                // We use local Z-axis roll for 3D-safe 2D rotation.
                if (shouldFlip)
                {
                    // Flipping via sprite flipX is highly stable for 2D aesthetics
                    mainVisualSprite.flipX = true;
                    
                    // Adjust angle when flipped so head points along the vector correctly
                    float flippedAngle = angle + 180f;
                    transform.rotation = Quaternion.Euler(0f, 180f, flippedAngle);
                }
                else
                {
                    mainVisualSprite.flipX = false;
                    transform.rotation = Quaternion.Euler(0f, 180f, angle);
                }
            }

            // Sync the Warning Shadow Decal rotation and flips to perfectly match the fish's orientation
            if (shadowDecal != null)
            {
                shadowDecal.flipX = mainVisualSprite != null && mainVisualSprite.flipX;
                shadowDecal.transform.rotation = transform.rotation;
            }
        }

        private void HandleZLayerVisualsAndShadow()
        {
            if (playerTransform == null) return;

            float enemyZ = transform.position.z;
            float playerZ = playerTransform.position.z;

            // Strict depth margin (same as tolerance in DepthLayerFilter)
            float tolerance = 0.5f;

            // Scenario: Enemy is ABOVE the player (Z is shallower/closer to surface, i.e., enemyZ > playerZ)
            if (enemyZ > playerZ + tolerance)
            {
                // 1. DESTRUCTIVE FIX: HIDE ORIGINAL VISUAL!
                // Since the enemy is at a shallower depth, the original visual sprite MUST be completely disabled!
                if (mainVisualSprite != null)
                {
                    mainVisualSprite.enabled = false;
                }

                // 2. Project Warning Shadow on the player's plane
                float zDistance = enemyZ - playerZ;

                if (shadowDecal != null)
                {
                    if (zDistance <= maxShadowDistance)
                    {
                        shadowDecal.gameObject.SetActive(true);

                        // Lock shadow perfectly on the player's depth layer (Z)
                        shadowDecal.transform.position = new Vector3(transform.position.x, transform.position.y, playerZ);

                        // Scale shadow based on closeness: closer = smaller & sharper, further = larger & blurry
                        float closeness = Mathf.Clamp01(1.0f - (zDistance / maxShadowDistance));
                        float targetScale = Mathf.Lerp(1.6f, 0.7f, closeness);
                        shadowDecal.transform.localScale = new Vector3(targetScale, targetScale, 1f);

                        // Dynamic opacity
                        Color shadowColor = shadowDecal.color;
                        shadowColor.a = Mathf.Lerp(0.1f, 0.75f, closeness);
                        shadowDecal.color = shadowColor;
                    }
                    else
                    {
                        shadowDecal.gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                // Scenario: Enemy is on the same layer or below the player
                // 1. Restore main visual sprite visibility
                if (mainVisualSprite != null)
                {
                    mainVisualSprite.enabled = true;
                }

                // 2. Hide Warning Shadow
                if (shadowDecal != null)
                {
                    shadowDecal.gameObject.SetActive(false);
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            EvaluateImpact(collision.gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            EvaluateImpact(other.gameObject);
        }

        private void EvaluateImpact(GameObject hitObject)
        {
            // Only deal damage if on the same depth layer as player
            if (playerSurvival != null && hitObject == playerSurvival.gameObject)
            {
                playerSurvival.DamageOxygen(enemyData.attackDamage);

                // Recoil back Z-wise after hitting
                Vector3 rec = transform.position;
                rec.z -= 2.0f; // Push deeper
                transform.position = rec;
                if (depthFilter != null) depthFilter.RecalculateMyDepth();
            }
        }
    }
}
