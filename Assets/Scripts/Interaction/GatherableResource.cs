using UnityEngine;
using DeepSea.Data;
using DeepSea.Player;

namespace DeepSea.Interaction
{
    /// <summary>
    /// 바다 내 수집 가능한 채집 노드(해초, 전복, 조개 등)의 베이스 컴포넌트입니다.
    /// 자동으로 DepthLayerFilter를 의무 부착하여, 동일한 수심 레이어를 가진 플레이어하고만
    /// 상호작용 및 물리 감지가 발생하도록 설계되었습니다.
    /// </summary>
    [RequireComponent(typeof(DepthLayerFilter))]
    [RequireComponent(typeof(Collider))]
    public class GatherableResource : MonoBehaviour, IGatherable
    {
        [Header("자원 데이터 설정")]
        [SerializeField] private ResourceData resourceData;

        [Header("비주얼 오브젝트 참조")]
        [Tooltip("자원 스프라이트를 그려줄 렌더러 컴포넌트입니다.")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        private bool isGathered = false;
        private bool isInitialized = false;

        /// <summary>
        /// 동적 스포너가 런타임에 인스턴스화할 때 동적으로 ResourceData 데이터를 주입해 줍니다.
        /// </summary>
        public void Initialize(ResourceData data)
        {
            resourceData = data;
            isInitialized = true;

            if (spriteRenderer != null && resourceData != null && resourceData.sprite != null)
            {
                spriteRenderer.sprite = resourceData.sprite;
            }
        }

        private void Start()
        {
            if (resourceData == null)
            {
                if (!isInitialized)
                {
                    Debug.LogWarning($"[GatherableResource] {gameObject.name}의 ResourceData가 누락되었습니다. 스포너로부터 데이터 주입을 대기합니다.");
                }
            }
            else
            {
                Initialize(resourceData);
            }
        }

        /// <summary>
        /// 플레이어가 상호작용 키를 눌러 채집에 성공했을 때 인벤토리에 보상을 추가하고 해당 노드를 소멸시킵니다.
        /// </summary>
        public void Gather(GameObject gatherer)
        {
            if (isGathered) return;

            // 채집자의 인벤토리 인터페이스 획득
            IInventory inventory = gatherer.GetComponent<IInventory>();
            if (inventory == null)
            {
                inventory = gatherer.GetComponentInChildren<IInventory>();
            }

            if (inventory != null)
            {
                if (inventory.IsFull())
                {
                    Debug.Log("[GatherableResource] 가방(망사리)이 가득 차서 수집할 수 없습니다!");
                    return;
                }

                isGathered = true;
                bool success = inventory.AddResource(resourceData);

                if (success)
                {
                    // 채집 성공 효과 연출 유틸리티 함수 호출
                    PlayGatherEffect();

                    // 수집 완료 후 씬에서 파괴 처리
                    Destroy(gameObject, 0.1f);
                }
                else
                {
                    isGathered = false;
                }
            }
            else
            {
                Debug.LogWarning("[GatherableResource] 채집을 시도한 오브젝트에 IInventory 인벤토리 컴포넌트가 부착되어 있지 않습니다.");
            }
        }

        public float GetGatherTime()
        {
            return resourceData != null ? resourceData.gatherTime : 1.0f;
        }

        public ResourceData GetResourceData()
        {
            return resourceData;
        }

        private void PlayGatherEffect()
        {
            // 이펙트 생성, 사운드 출력 등 다양한 센서리 연출을 꽂을 수 있는 예비 슬롯
            Debug.Log($"[GatherableResource] {resourceData.resourceName} 수집 완료!");
        }
    }
}
