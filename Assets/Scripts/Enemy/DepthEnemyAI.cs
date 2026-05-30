using UnityEngine;
using DeepSea.Data;
using DeepSea.Player;
using DeepSea.Interaction;

namespace DeepSea.Enemy
{
    /// <summary>
    /// 시니어 레벨의 입체 깊이 추적 적 AI.
    /// 플레이어 수심 레이어별 시각적 은닉 규칙, 수면 안전구역 및 상층 경고 그림자 데칼 투사를 관리하고,
    /// 2D 빌보드 평면에서 적절하게 방향을 매칭하는 좌우 플립 및 회전을 부드럽게 연동합니다.
    /// </summary>
    [RequireComponent(typeof(DepthLayerFilter))]
    [RequireComponent(typeof(Collider))]
    public class DepthEnemyAI : MonoBehaviour
    {
        [Header("데이터 및 설정")]
        [SerializeField] private EnemyData enemyData;
        
        [Header("비주얼 오브젝트 참조")]
        [Tooltip("적 물고기의 본체 스프라이트 렌더러입니다.")]
        [SerializeField] private SpriteRenderer mainVisualSprite;

        [Header("그림자 투사 설정")]
        [Tooltip("플레이어 수심 레이어에 몬스터의 상층 추적 위험 경고를 투사해줄 데칼용 스프라이트입니다.")]
        [SerializeField] private SpriteRenderer shadowDecal;

        [Tooltip("경고 그림자 데칼이 최대로 도달할 수 있는 수심 오차 간격입니다.")]
        [SerializeField] private float maxShadowDistance = 15f;

        [Header("순찰 패턴 설정")]
        [Tooltip("순찰 중 헤엄칠 방향을 한 번 바꾸기까지 대기하는 초 단위 시간 간격입니다.")]
        [SerializeField] private float patrolDirectionChangeInterval = 3f;
        
        [Tooltip("기본 순찰 속도 비율입니다. (이동 속도 * 비율)")]
        [SerializeField] private float patrolSpeedMultiplier = 0.4f;

        // 종속성 및 상태
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

        private bool isInitialized = false;

        /// <summary>
        /// 동적 스포너가 런타임에 인스턴스화할 때 동적으로 EnemyData 데이터를 주입해 줍니다.
        /// </summary>
        public void Initialize(EnemyData data)
        {
            enemyData = data;
            isInitialized = true;

            if (mainVisualSprite != null && enemyData != null && enemyData.sprite != null)
            {
                mainVisualSprite.sprite = enemyData.sprite;
            }
        }

        private void Start()
        {
            if (enemyData == null)
            {
                if (!isInitialized)
                {
                    Debug.LogWarning($"[DepthEnemyAI] {gameObject.name}의 EnemyData가 누락되었습니다. 스포너로부터 데이터 주입을 대기합니다.");
                }
            }
            else
            {
                Initialize(enemyData);
            }

            // 씬 내부의 플레이어 생존자 참조 획득
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

            // 첫 랜덤 순찰 방향 지정
            PickRandomPatrolDirection();
        }

        private void Update()
        {
            if (playerTransform == null || playerSurvival == null || playerSurvival.IsDead)
            {
                if (shadowDecal != null) shadowDecal.gameObject.SetActive(false);
                
                // 예외 발생 시 모든 비주얼 스프라이트 안전 복원
                var allSprites = GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var sprite in allSprites)
                {
                    if (sprite != shadowDecal)
                    {
                        sprite.enabled = true;
                    }
                }
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            Vector3 moveDirection = Vector3.zero;
            
            // 감지 반경 이내인 경우 타겟 추적 활성화
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

            // 진행 방향에 맞춰 물고기 좌우 플립 및 2D 앵글 보정
            ApplyFishRotation(moveDirection);

            // 레이어별 시각 가림 효과 및 상층부 추적 그림자 투사 기믹 처리
            HandleZLayerVisualsAndShadow();
        }

        /// <summary>
        /// 플레이어를 향해 가로세로(X, Y) 및 수심(Z) 방향으로 점진적 입체 추적을 수행합니다.
        /// </summary>
        private Vector3 ChasePlayer()
        {
            Vector3 playerPos = playerTransform.position;
            Vector3 currentPos = transform.position;

            // 1. 수평 좌표(X, Y) 방향으로 플레이어 추격
            Vector3 horizontalDir = new Vector3(playerPos.x - currentPos.x, playerPos.y - currentPos.y, 0f);
            if (horizontalDir.sqrMagnitude > 0.001f)
            {
                horizontalDir.Normalize();
                transform.Translate(horizontalDir * (enemyData.moveSpeed * Time.deltaTime), Space.World);
            }

            // 2. 수심 좌표(Z) 방향으로 플레이어의 깊이에 도달하게 수직 이동
            float newZ = Mathf.MoveTowards(currentPos.z, playerPos.z, enemyData.depthTransitionSpeed * Time.deltaTime);
            transform.position = new Vector3(transform.position.x, transform.position.y, newZ);

            if (depthFilter != null)
            {
                depthFilter.RecalculateMyDepth();
            }

            return horizontalDir;
        }

        /// <summary>
        /// 평화롭게 임의의 수평 방향으로 순찰합니다.
        /// </summary>
        private Vector3 IdlePatrol()
        {
            patrolTimer += Time.deltaTime;
            if (patrolTimer >= patrolDirectionChangeInterval)
            {
                PickRandomPatrolDirection();
            }

            float patrolSpeed = enemyData.moveSpeed * patrolSpeedMultiplier;
            transform.Translate(patrolDirection * (patrolSpeed * Time.deltaTime), Space.World);

            return patrolDirection;
        }

        private void PickRandomPatrolDirection()
        {
            patrolTimer = 0f;
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            patrolDirection = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
        }

        /// <summary>
        /// 진행 방향에 맞춰 물고기 리소스를 회전/플립합니다.
        /// 플랫한 2D 빌보드 특성이 왜곡되지 않도록 카메라 각도에 정렬합니다.
        /// (해당 물고기 원본 리소스는 오른쪽(X+)을 향해 있다고 가정합니다)
        /// </summary>
        private void ApplyFishRotation(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.001f) return;

            // 1. 가로세로 평면에서의 아크탄젠트 각도 계산
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // 2. 물고기가 진행 중 뒤집어지는 것을 피하기 위해 각도에 따른 가로 플립
            bool shouldFlip = Mathf.Abs(angle) > 90f;

            if (mainVisualSprite != null)
            {
                // 카메라 평면(Y축 180도 회전)에 완전 일치하도록 빌보드 로직 및 z축 각도 롤 조절
                if (shouldFlip)
                {
                    mainVisualSprite.flipX = true;
                    float flippedAngle = angle + 180f;
                    transform.rotation = Quaternion.Euler(0f, 180f, flippedAngle);
                }
                else
                {
                    mainVisualSprite.flipX = false;
                    transform.rotation = Quaternion.Euler(0f, 180f, angle);
                }
            }

            // 그림자 데칼도 본체와 회전율/플립 여부를 완벽히 동기화해 줍니다.
            if (shadowDecal != null)
            {
                shadowDecal.flipX = mainVisualSprite != null && mainVisualSprite.flipX;
                shadowDecal.transform.rotation = transform.rotation;
            }
        }

        /// <summary>
        /// 레이어 깊이에 따른 특수 연출:
        /// 적이 플레이어보다 얕은 물(enemyZ > playerZ)에 있는 경우 본체 스프라이트를 끄고 
        /// 아래 플레이어의 층에 경고 그림자만 Projection 합니다.
        /// </summary>
        private void HandleZLayerVisualsAndShadow()
        {
            if (playerTransform == null) return;

            float enemyZ = transform.position.z;
            float playerZ = playerTransform.position.z;

            float tolerance = 0.5f; // 수심 일치 허용 오차

            // 몬스터가 플레이어보다 위에(얕은 수심에) 위치한 경우
            if (enemyZ > playerZ + tolerance)
            {
                // 1. 본체 및 모든 자식 비주얼 스프라이트 비활성화 (중복 부착된 SpriteRenderer 오류 원천 방지)
                var allSprites = GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var sprite in allSprites)
                {
                    if (sprite != shadowDecal)
                    {
                        sprite.enabled = false;
                    }
                }

                // 2. 플레이어 수심 평면에 그림자 데칼 투사
                float zDistance = enemyZ - playerZ;

                if (shadowDecal != null)
                {
                    if (zDistance <= maxShadowDistance)
                    {
                        shadowDecal.gameObject.SetActive(true);

                        // 그림자의 Z 위치를 플레이어 수심으로 고정
                        shadowDecal.transform.position = new Vector3(transform.position.x, transform.position.y, playerZ);

                        // 거리에 따라 그림자의 크기와 굵기를 선형 보간 (가까울수록 작고 짙게, 멀수록 옅고 넓게)
                        float closeness = Mathf.Clamp01(1.0f - (zDistance / maxShadowDistance));
                        float targetScale = Mathf.Lerp(1.6f, 0.7f, closeness);
                        shadowDecal.transform.localScale = new Vector3(targetScale, targetScale, 1f);

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
                // 몬스터가 동일 층에 왔거나 아래에 깔린 경우
                // 1. 본체 및 모든 자식 비주얼 스프라이트 복원
                var allSprites = GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var sprite in allSprites)
                {
                    if (sprite != shadowDecal)
                    {
                        sprite.enabled = true;
                    }
                }

                // 2. 그림자 끄기
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

        /// <summary>
        /// 충돌 시 수심 층이 맞는지 최종 교차 검증을 거쳐 산소 데미지를 가합니다.
        /// </summary>
        private void EvaluateImpact(GameObject hitObject)
        {
            if (playerSurvival != null && hitObject == playerSurvival.gameObject)
            {
                playerSurvival.DamageOxygen(enemyData.attackDamage);

                // 부딪힌 뒤 Z축 깊이 방향으로 2.0 유닛 만큼 튕겨나가게 유도 (물리 넉백)
                Vector3 rec = transform.position;
                rec.z -= 2.0f;
                transform.position = rec;
                if (depthFilter != null) depthFilter.RecalculateMyDepth();
            }
        }
    }
}
