using System;
using UnityEngine;
using DeepSea.Player;

namespace DeepSea.Environment
{
    /// <summary>
    /// 시니어 레벨의 수심 통합 관리 매니저.
    /// 플레이어의 현재 수심 Z 좌표를 미터 단위 양수로 계산 및 변환하고,
    /// 수심 값이 바뀌거나 수심 존(Depth Zone)이 전환될 때 이벤트를 발송합니다.
    /// 시각 효과, UI 연출, 적 AI들의 물리 레이어 연산 처리를 연결하는 디커플링 통로 역할을 수행합니다.
    /// </summary>
    public class DepthManager : MonoBehaviour
    {
        public static DepthManager Instance { get; private set; }

        [Header("추적 타겟")]
        [Tooltip("수심을 측정할 플레이어의 Transform 컴포넌트입니다. 비워둘 경우 자동으로 씬 내의 플레이어를 검색하여 추적합니다.")]
        [SerializeField] private Transform playerTransform;

        [Header("수심 존(Zone) 경계 설정 (미터 단위)")]
        [Tooltip("황혼(Mid) 존이 시작되는 수심 깊이 한계입니다.")]
        [SerializeField] private float midZoneThreshold = 30f;
        
        [Tooltip("심해(Deep) 존이 시작되는 수심 깊이 한계입니다.")]
        [SerializeField] private float deepZoneThreshold = 80f;

        // 현재 상태 지표
        public float CurrentDepth { get; private set; } = 0f;
        public DepthZone CurrentZone { get; private set; } = DepthZone.Shallow;

        // 이벤트 정의
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
            // 시니어 팁: 만약 인스펙터에서 플레이어 참조 연결이 누락되었거나 동적 생성된 경우, 스스로 찾아내 등록하는 방어코드 작동
            if (playerTransform == null)
            {
                var playerController = FindFirstObjectByType<PlayerController>();
                if (playerController != null)
                {
                    playerTransform = playerController.transform;
                }
                else
                {
                    return; // 플레이어가 씬에 아직 생기지 않은 경우 연산을 대기합니다.
                }
            }

            // 플레이어 Z 좌표는 깊이 들어갈수록 음수가 됩니다.
            // 양수 미터 단위 수심 변환: 수심 = -Z
            float calculatedDepth = Mathf.Max(0f, -playerTransform.position.z);

            if (!Mathf.Approximately(calculatedDepth, CurrentDepth))
            {
                CurrentDepth = calculatedDepth;
                OnDepthChanged?.Invoke(CurrentDepth);

                UpdateDepthZone();
            }
        }

        /// <summary>
        /// 수심 미터에 맞게 Shallow(0~30m), Mid(30~80m), Deep(80m+) 존을 판별하고 전환 이벤트를 날려줍니다.
        /// </summary>
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
        /// 동적으로 플레이어가 스폰되었을 경우 플레이어 Transform 참조를 등록해 줍니다.
        /// </summary>
        public void RegisterPlayer(Transform player)
        {
            playerTransform = player;
        }

        public float MidZoneThreshold => midZoneThreshold;
        public float DeepZoneThreshold => deepZoneThreshold;
    }
}
