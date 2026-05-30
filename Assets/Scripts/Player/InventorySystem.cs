using System;
using System.Collections.Generic;
using UnityEngine;
using DeepSea.Data;

namespace DeepSea.Player
{
    /// <summary>
    /// 시니어 레벨의 IInventory 인터페이스 구현체.
    /// 플레이어가 수집한 자원, 총 가방 무게, 현재 가치의 누적 골드를 정합적으로 관리합니다.
    /// 인벤토리 수량이나 재화 상태 변경 시 이벤트를 뿌려 UI와 PlayerController 속도 패널티가 즉각 반응하도록 조율합니다.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class InventorySystem : MonoBehaviour, IInventory
    {
        [Header("플레이어 기본 데이터")]
        [SerializeField] private PlayerData playerData;

        // 인벤토리 소지 목록
        private readonly List<ResourceData> carriedItems = new List<ResourceData>();

        // 상태 지표
        public float CurrentWeight { get; private set; } = 0f;
        public int TotalPotentialGold { get; private set; } = 0;

        // 이벤트 정의
        public event Action OnInventoryChanged;
        public event Action<int> OnItemsSold; // 판매된 총 골드량을 인자로 전달합니다.

        private PlayerController playerController;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
        }

        private void Start()
        {
            if (playerData == null)
            {
                Debug.LogError("[InventorySystem] PlayerData ScriptableObject 참조가 누락되었습니다!");
            }
        }

        public bool AddResource(ResourceData resource)
        {
            if (resource == null) return false;

            float maxWeight = playerData != null ? playerData.GetMaxCarryWeight() : 50f;

            // 엄격한 수하물 제한 검증
            if (CurrentWeight + resource.weight > maxWeight)
            {
                Debug.Log("[InventorySystem] 가방 무게 한계를 초과하여 아이템을 추가할 수 없습니다.");
                return false;
            }

            carriedItems.Add(resource);
            RecalculateInventory();

            return true;
        }

        public float GetCurrentWeight() => CurrentWeight;

        public bool IsFull()
        {
            float maxWeight = playerData != null ? playerData.GetMaxCarryWeight() : 50f;
            // Float 오차를 보정한 한계 근사 판단
            return CurrentWeight >= maxWeight - 0.1f;
        }

        /// <summary>
        /// 소지한 모든 채집물들을 즉각 비우고 누적된 가치 골드를 판매 정산 처리합니다.
        /// 일반적으로 해수면(Z=0)에 복귀했을 때 자동으로 호출됩니다.
        /// </summary>
        public void ClearAndSellItems()
        {
            if (carriedItems.Count == 0) return;

            int earnedGold = TotalPotentialGold;
            
            carriedItems.Clear();
            RecalculateInventory();

            OnItemsSold?.Invoke(earnedGold);
            Debug.Log($"[InventorySystem] 모든 채집물이 일제히 판매되었습니다! 누적 정산: +{earnedGold} G!");
        }

        /// <summary>
        /// 인벤토리의 소지 아이템 무게와 가치 총합을 다시 연산하고, 
        /// 가방에 가해진 부하에 비례하여 플레이어 컨트롤러의 속도 페널티를 동적으로 설정합니다.
        /// </summary>
        private void RecalculateInventory()
        {
            float newWeight = 0f;
            int newGold = 0;

            foreach (var item in carriedItems)
            {
                newWeight += item.weight;
                newGold += item.goldValue;
            }

            CurrentWeight = newWeight;
            TotalPotentialGold = newGold;

            // 소지 무게가 늘어날수록 플레이어 이동 속도가 선형 감쇠합니다.
            if (playerController != null && playerData != null)
            {
                // 실시간 스피드 배율 공식: 배율 = 1.0 - (현재 무게 * 페널티 계수 / 기본 속도)
                float penalty = CurrentWeight * playerData.weightSpeedPenaltyFactor;
                float baseSpeed = playerData.GetMoveSpeed();
                float multiplier = Mathf.Max(0.2f, 1.0f - (penalty / baseSpeed));
                
                playerController.UpdateSpeedMultiplier(multiplier);
            }

            OnInventoryChanged?.Invoke();
        }

        public IReadOnlyList<ResourceData> CarriedItems => carriedItems;
        
        /// <summary>
        /// 업그레이드 레벨이 반영된 현재의 최대 소지 가능 무게 한계치를 반환합니다.
        /// </summary>
        public float MaxWeight => playerData != null ? playerData.GetMaxCarryWeight() : 50f;
    }
}
