using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeepSea.Core;
using DeepSea.Environment;
using DeepSea.Player;

namespace DeepSea.UI
{
    /// <summary>
    /// 시니어 레벨 게임플레이 실시간 UI 프리젠터 (MVP 패턴 지향).
    /// 플레이어의 생존 및 수심 상태를 구체적인 UI 렌더링 컴포넌트들로부터 디커플링하여 관리합니다.
    /// 실시간 연동:
    /// 1. 수심 UI: 화면 중앙 상단에 "현재 수심 / 최대 제한 수심" 표시.
    /// 2. 산소 UI: 화면 우측의 수직 산소 게이지 슬라이더와 정확한 수치 표시,
    ///    산소가 위험 수준으로 내려가면 녹색 -> 황색 -> 적색으로 경고 색상 가변 처리.
    /// 3. 경제 및 무게 UI: 획득한 골드량 및 현재 가방 소지 무게/최대 용량을 동적으로 시각화.
    /// </summary>
    public class GameplayUIPresenter : MonoBehaviour
    {
        [Header("수심 UI 참조")]
        [Tooltip("화면 중앙 상단에 '현재 수심 m / 최대 제한 수심 m'을 표기할 TextMeshPro 컴포넌트입니다.")]
        [SerializeField] private TextMeshProUGUI depthText;

        [Header("산소 UI 참조")]
        [Tooltip("화면 우측에 산소 수준을 표시해줄 세로 슬라이더입니다.")]
        [SerializeField] private Slider oxygenSlider;

        [Tooltip("산소통 슬라이더 아래에 정밀 수치를 보여줄 TextMeshPro 컴포넌트입니다.")]
        [SerializeField] private TextMeshProUGUI oxygenText;

        [Header("산소 위험 수준 경고 색상 설정")]
        [Tooltip("산소 슬라이더의 채워진 영역(Fill Area) 색상을 동적으로 제어할 이미지입니다.")]
        [SerializeField] private Image sliderFillImage;

        [SerializeField] private Color normalOxygenColor = new Color(0.2f, 0.8f, 0.3f);   // 청결한 녹색
        [SerializeField] private Color warningOxygenColor = new Color(0.9f, 0.7f, 0.1f);  // 주의 황색
        [SerializeField] private Color criticalOxygenColor = new Color(0.9f, 0.1f, 0.1f); // 위험 적색

        [Header("경제 및 인벤토리 UI 참조")]
        [Tooltip("현재 보유하고 있는 골드량을 표시할 TextMeshPro 컴포넌트입니다.")]
        [SerializeField] private TextMeshProUGUI goldText;

        [Tooltip("현재 가방(망사리) 무게 / 최대 제한 용량을 표시할 TextMeshPro 컴포넌트입니다.")]
        [SerializeField] private TextMeshProUGUI inventoryWeightText;

        [Tooltip("최대 용량 대비 현재 무게의 비율을 표기해줄 슬라이더입니다.")]
        [SerializeField] private Slider inventoryWeightSlider;

        // 캐싱된 종속성 및 변수
        private PlayerSurvival playerSurvival;
        private PlayerController playerController;
        private InventorySystem inventorySystem;
        private float maxDepthLimitInMeters = 200f;

        private void Start()
        {
            FindPlayerDependencies();
        }

        private void OnEnable()
        {
            // DepthManager 깊이 변경 이벤트 구독
            if (DepthManager.Instance != null)
            {
                DepthManager.Instance.OnDepthChanged += HandleDepthChanged;
            }

            // PlayerSurvival 산소 게이지 변경 이벤트 구독
            if (playerSurvival != null)
            {
                playerSurvival.OnOxygenChanged += HandleOxygenChanged;
            }

            // 인벤토리 상태 변경 이벤트 구독
            if (inventorySystem != null)
            {
                inventorySystem.OnInventoryChanged += HandleInventoryChanged;
            }

            // ShopManager 골드 잔액 변경 이벤트 구독
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnGoldChanged += HandleGoldChanged;
            }
        }

        private void OnDisable()
        {
            if (DepthManager.Instance != null)
            {
                DepthManager.Instance.OnDepthChanged -= HandleDepthChanged;
            }

            if (playerSurvival != null)
            {
                playerSurvival.OnOxygenChanged -= HandleOxygenChanged;
            }

            if (inventorySystem != null)
            {
                inventorySystem.OnInventoryChanged -= HandleInventoryChanged;
            }

            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnGoldChanged -= HandleGoldChanged;
            }
        }

        private void FindPlayerDependencies()
        {
            playerSurvival = FindFirstObjectByType<PlayerSurvival>();
            playerController = FindFirstObjectByType<PlayerController>();
            inventorySystem = FindFirstObjectByType<InventorySystem>();

            if (playerController != null)
            {
                // 플레이어 컨트롤러의 최대 깊이 설정값 획득
                maxDepthLimitInMeters = playerController.MaxDepthLimitInMeters;
            }

            // 런타임에 동적으로 주입될 수 있으므로 재구독 방지 처리
            if (playerSurvival != null)
            {
                playerSurvival.OnOxygenChanged -= HandleOxygenChanged; 
                playerSurvival.OnOxygenChanged += HandleOxygenChanged;
                
                // 첫 UI 수치 정렬
                HandleOxygenChanged(playerSurvival.CurrentOxygen, 100f); 
            }

            if (inventorySystem != null)
            {
                inventorySystem.OnInventoryChanged -= HandleInventoryChanged;
                inventorySystem.OnInventoryChanged += HandleInventoryChanged;
                HandleInventoryChanged(); // 첫 UI 수치 정렬
            }

            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnGoldChanged -= HandleGoldChanged;
                ShopManager.Instance.OnGoldChanged += HandleGoldChanged;
                HandleGoldChanged(ShopManager.Instance.CurrentGold); // 첫 UI 수치 정렬
            }

            // 시니어 팁: OnEnable 시점에 싱글톤이 null이어서 이벤트 구독을 건너뛰는 레이스 컨디션을 방어하기 위해 여기서 재구독 처리를 수행합니다.
            if (DepthManager.Instance != null)
            {
                DepthManager.Instance.OnDepthChanged -= HandleDepthChanged; // 중복 방지
                DepthManager.Instance.OnDepthChanged += HandleDepthChanged;
                HandleDepthChanged(DepthManager.Instance.CurrentDepth);
            }
        }

        private void HandleDepthChanged(float currentDepth)
        {
            if (depthText == null) return;

            // 소수점 한 자리까지 "현재 수심 / 최대 제한 수심" 표기 (예: "42.5m / 200m")
            depthText.text = $"{currentDepth:F1}m / {maxDepthLimitInMeters:F0}m";
        }

        private void HandleOxygenChanged(float currentOxygen, float maxOxygen)
        {
            if (oxygenSlider == null || oxygenText == null) return;

            // 1. 슬라이더 동기화
            oxygenSlider.maxValue = maxOxygen;
            oxygenSlider.value = currentOxygen;

            // 2. 소수점 이하 올림 처리한 정수형 수치 표기 (예: "85 / 100")
            oxygenText.text = $"{Mathf.CeilToInt(currentOxygen)} / {Mathf.CeilToInt(maxOxygen)}";

            // 3. 다이내믹 위험 경고 색상 연출 (녹색 -> 황색 -> 적색)
            if (sliderFillImage != null && maxOxygen > 0.01f)
            {
                float oxygenPercent = currentOxygen / maxOxygen;

                if (oxygenPercent > 0.5f)
                {
                    sliderFillImage.color = normalOxygenColor;
                }
                else if (oxygenPercent > 0.2f)
                {
                    sliderFillImage.color = warningOxygenColor;
                }
                else
                {
                    sliderFillImage.color = criticalOxygenColor;
                    
                    // 위험 단계 도달 시 텍스트도 적색으로 가변 처리
                    oxygenText.color = criticalOxygenColor;
                }

                if (oxygenPercent > 0.2f)
                {
                    oxygenText.color = Color.white; // 정상 색상 복원
                }
            }
        }

        private void HandleGoldChanged(int currentGold)
        {
            if (goldText == null) return;
            goldText.text = $"{currentGold} G";
        }

        private void HandleInventoryChanged()
        {
            if (inventorySystem == null) return;

            float currentWeight = inventorySystem.GetCurrentWeight();
            float maxWeight = inventorySystem.MaxWeight;

            // 수치 텍스트 갱신 (예: "12.4 / 50.0 kg")
            if (inventoryWeightText != null)
            {
                inventoryWeightText.text = $"{currentWeight:F1} / {maxWeight:F1} kg";

                // 가방 수량에 따른 위험 텍스트 색상 경고 피드백
                if (inventorySystem.IsFull())
                {
                    inventoryWeightText.color = criticalOxygenColor;
                }
                else if (currentWeight >= maxWeight * 0.8f)
                {
                    inventoryWeightText.color = warningOxygenColor;
                }
                else
                {
                    inventoryWeightText.color = Color.white;
                }
            }

            // 슬라이더 동기화
            if (inventoryWeightSlider != null)
            {
                inventoryWeightSlider.maxValue = maxWeight;
                inventoryWeightSlider.value = currentWeight;

                // 무게 비율에 따른 게이지 fill 색상 피드백
                Image fill = inventoryWeightSlider.fillRect != null ? inventoryWeightSlider.fillRect.GetComponent<Image>() : null;
                if (fill != null && maxWeight > 0.01f)
                {
                    float percent = currentWeight / maxWeight;
                    if (percent >= 1.0f - 0.01f) fill.color = criticalOxygenColor;
                    else if (percent >= 0.8f) fill.color = warningOxygenColor;
                    else fill.color = normalOxygenColor;
                }
            }
        }

        /// <summary>
        /// 종속관계 강제 새로고침이 필요한 경우 외부에서 호출합니다.
        /// </summary>
        public void RefreshUI()
        {
            FindPlayerDependencies();
        }
    }
}
