using System.Collections.Generic;
using UnityEngine;
using DeepSea.Data;
using DeepSea.Enemy;
using DeepSea.Interaction;

namespace DeepSea.Environment
{
    /// <summary>
    /// 시니어 레벨 배치 스포너 컴포넌트.
    /// 라운드가 시작될 때 바다의 각 수심 구역과 가로세로 범위 내에 지정된 자원 및 적들을 사전 배치(Pre-population)합니다.
    /// 수심 존(Depth Zone) 범위 내에서 무작위 좌표를 계산하고, 생성된 프리팹에
    /// 스크립터블 오브젝트(ScriptableObject) 데이터를 동적으로 주입하여 안정적인 런타임 동작을 보장합니다.
    /// </summary>
    public class DepthSpawner : MonoBehaviour
    {
        [System.Serializable]
        public struct EnemySpawnConfig
        {
            [Tooltip("에디터 인스펙터 창에서 식별하기 위한 편의용 라벨입니다.")]
            public string label;
            [Tooltip("생성할 적의 ScriptableObject 데이터입니다.")]
            public EnemyData enemyData;
            [Tooltip("생성할 적의 프리팹 GameObject입니다.")]
            public GameObject enemyPrefab;
            [Tooltip("라운드 시작 시 생성할 개수입니다.")]
            public int spawnCount;
        }

        [System.Serializable]
        public struct ResourceSpawnConfig
        {
            [Tooltip("에디터 인스펙터 창에서 식별하기 위한 편의용 라벨입니다.")]
            public string label;
            [Tooltip("생성할 자원의 ScriptableObject 데이터입니다.")]
            public ResourceData resourceData;
            [Tooltip("생성할 자원의 프리팹 GameObject입니다.")]
            public GameObject resourcePrefab;
            [Tooltip("라운드 시작 시 생성할 개수입니다.")]
            public int spawnCount;
        }

        [Header("스폰 영역 설정 (X, Y 수평 평면)")]
        [Tooltip("스폰 영역의 최소 X 좌표 한계선입니다.")]
        [SerializeField] private float minX = -40f;
        [Tooltip("스폰 영역의 최대 X 좌표 한계선입니다.")]
        [SerializeField] private float maxX = 40f;
        [Tooltip("스폰 영역의 최소 Y 좌표 한계선입니다.")]
        [SerializeField] private float minY = -40f;
        [Tooltip("스폰 영역의 최대 Y 좌표 한계선입니다.")]
        [SerializeField] private float maxY = 40f;

        [Header("몬스터 스폰 안전 버퍼")]
        [Tooltip("몬스터가 스폰될 수 있는 가장 얕은 수심(양수 미터)입니다. 플레이어가 수면 근처에서 급사하는 것을 방지합니다.")]
        [SerializeField] private float monsterMinDepthSafetyLimit = 20f;

        [Header("스폰 설정 목록")]
        [SerializeField] private List<EnemySpawnConfig> enemiesToSpawn = new List<EnemySpawnConfig>();
        [SerializeField] private List<ResourceSpawnConfig> resourcesToSpawn = new List<ResourceSpawnConfig>();

        private void Start()
        {
            SpawnAllResources();
            SpawnAllEnemies();
        }

        /// <summary>
        /// 설정된 모든 자원들을 수심에 맞게 분산 스폰합니다.
        /// </summary>
        private void SpawnAllResources()
        {
            int totalSpawned = 0;

            foreach (var config in resourcesToSpawn)
            {
                if (config.resourcePrefab == null || config.resourceData == null)
                {
                    Debug.LogWarning($"[DepthSpawner] 자원 설정 '{config.label}'에 프리팹 또는 데이터가 누락되었습니다! 스폰을 건너뜁니다.");
                    continue;
                }

                for (int i = 0; i < config.spawnCount; i++)
                {
                    Vector3 spawnPos = CalculateSpawnPosition(config.resourceData.minSpawnDepth, config.resourceData.maxSpawnDepth, false);
                    GameObject instance = Instantiate(config.resourcePrefab, spawnPos, Quaternion.identity);
                    instance.name = $"{config.resourceData.resourceName}_Spawned_{i}";

                    // 1. 의존성 주입: 생성된 자원에 ResourceData 주입
                    var resourceComponent = instance.GetComponent<GatherableResource>();
                    if (resourceComponent != null)
                    {
                        resourceComponent.Initialize(config.resourceData);
                    }

                    // 2. 깊이 동기화: 생성된 오브젝트의 깊이 필터를 호출해 플레이어와의 거리별 셋팅을 즉시 갱신
                    TriggerDepthFilterSync(instance);

                    totalSpawned++;
                }
            }

            Debug.Log($"[DepthSpawner] 바다 레이어 전반에 걸쳐 총 {totalSpawned}개의 자원 배치를 완료했습니다.");
        }

        /// <summary>
        /// 설정된 모든 몬스터들을 수심 및 수면 안전거리를 고려하여 분산 스폰합니다.
        /// </summary>
        private void SpawnAllEnemies()
        {
            int totalSpawned = 0;

            foreach (var config in enemiesToSpawn)
            {
                if (config.enemyPrefab == null || config.enemyData == null)
                {
                    Debug.LogWarning($"[DepthSpawner] 적 설정 '{config.label}'에 프리팹 또는 데이터가 누락되었습니다! 스폰을 건너뜁니다.");
                    continue;
                }

                for (int i = 0; i < config.spawnCount; i++)
                {
                    // 수면 근처 스폰 방지를 위해 최소 수심 안전 한계 적용
                    float minDepth = Mathf.Max(config.enemyData.minSpawnDepth, monsterMinDepthSafetyLimit);
                    float maxDepth = config.enemyData.maxSpawnDepth;

                    Vector3 spawnPos = CalculateSpawnPosition(minDepth, maxDepth, true);
                    GameObject instance = Instantiate(config.enemyPrefab, spawnPos, Quaternion.identity);
                    instance.name = $"{config.enemyData.enemyName}_Spawned_{i}";

                    // 1. 의존성 주입: 생성된 적에 EnemyData 주입
                    var enemyAI = instance.GetComponent<DepthEnemyAI>();
                    if (enemyAI != null)
                    {
                        enemyAI.Initialize(config.enemyData);
                    }

                    // 2. 깊이 동기화: 첫 프레임 물리 연산 오차 방지를 위한 필터 즉시 동기화
                    TriggerDepthFilterSync(instance);

                    totalSpawned++;
                }
            }

            Debug.Log($"[DepthSpawner] 황혼 및 심해 레이어에 총 {totalSpawned}마리의 적 배치를 완료했습니다.");
        }

        /// <summary>
        /// 지정된 수평 범위(X, Y)와 수심(Z) 범위 내에서 무작위 스폰 좌표를 계산합니다.
        /// Z축은 깊이를 나타내며, 아래로 내려갈수록 음수 값을 가집니다 (수심 미터 = -Z).
        /// </summary>
        private Vector3 CalculateSpawnPosition(float minDepthMeters, float maxDepthMeters, bool isMonster)
        {
            float randX = Random.Range(minX, maxX);
            float randY = Random.Range(minY, maxY);

            // 경계값 대소 보호
            if (minDepthMeters > maxDepthMeters)
            {
                float temp = minDepthMeters;
                minDepthMeters = maxDepthMeters;
                maxDepthMeters = temp;
            }

            float randDepth = Random.Range(minDepthMeters, maxDepthMeters);
            
            // 수심 Z 좌표는 음수로 표현됨
            float randZ = -randDepth;

            return new Vector3(randX, randY, randZ);
        }

        /// <summary>
        /// 생성된 오브젝트의 DepthLayerFilter를 강제로 동기화하여, 
        /// 첫 프레임에서 엉뚱한 깊이 레이어에 있는 플레이어와 물리 충돌하는 물리 예외를 예방합니다.
        /// </summary>
        private void TriggerDepthFilterSync(GameObject spawnedObj)
        {
            var filter = spawnedObj.GetComponent<DepthLayerFilter>();
            if (filter == null)
            {
                filter = spawnedObj.GetComponentInChildren<DepthLayerFilter>();
            }

            if (filter != null)
            {
                filter.RecalculateMyDepth();
            }
        }

        // 유니티 에디터의 Scene 뷰에서 스폰 영역 경계를 시각적으로 그려줍니다. (선택 시 활성화)
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.5f, 0.35f);
            
            // 수면 높이에서의 수평 경계 사각형 표시
            Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
            Vector3 size = new Vector3(maxX - minX, maxY - minY, 1f);
            Gizmos.DrawWireCube(center, size);

            // 최대 종단 수심 한계 사각형 표시
            Gizmos.color = new Color(0.9f, 0.1f, 0.2f, 0.2f);
            float maxZ = -200f; // 일반적인 최대 깊이 한계
            Vector3 center3D = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, maxZ * 0.5f);
            Vector3 size3D = new Vector3(maxX - minX, maxY - minY, Mathf.Abs(maxZ));
            Gizmos.DrawWireCube(center3D, size3D);
        }
    }
}
