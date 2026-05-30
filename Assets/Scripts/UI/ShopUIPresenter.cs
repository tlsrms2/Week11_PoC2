using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeepSea.Core;
using DeepSea.Player;
using DeepSea.Environment;

namespace DeepSea.UI
{
    /// <summary>
    /// 시니어 레벨 상점 UI 프리젠터 (MVP 패턴 지향).
    /// 상점 UI 패널의 활성화 상태를 관리하고, 업그레이드 버튼 클릭 이벤트를 ShopManager에 바인딩하며,
    /// 각 업그레이드 항목의 시각적 상태(텍스트, 가격, 버튼 활성화 여부)를 동적으로 업데이트합니다.
    /// </summary>
    public class ShopUIPresenter : MonoBehaviour
    {
        [Header("상점 패널 참조")]
        [Tooltip("상점 화면 전체를 활성화/비활성화할 부모 UI 게임오브젝트입니다.")]
        [SerializeField] private GameObject shopPanel;

        [Tooltip("상점 패널을 수동으로 닫을 때 사용하는 닫기 버튼입니다.")]
        [SerializeField] private Button closeButton;

        [Header("업그레이드 항목: 산소통")]
        [SerializeField] private Button upgradeOxygenButton;
        [SerializeField] private TextMeshProUGUI oxygenStatusText;

        [Header("업그레이드 항목: 가방(망사리) 용량")]
        [SerializeField] private Button upgradeCapacityButton;
        [SerializeField] private TextMeshProUGUI capacityStatusText;

        [Header("업그레이드 항목: 이동 속도")]
        [SerializeField] private Button upgradeSpeedButton;
        [SerializeField] private TextMeshProUGUI speedStatusText;

        // 종속성
        private PlayerSurvival playerSurvival;

        private void Start()
        {
            FindPlayerDependencies();
            InitializeButtons();

            // 기본 상태는 닫혀 있도록 설정
            if (shopPanel != null)
            {
                shopPanel.SetActive(false);
            }

            // 시니어 팁: OnEnable 시점 싱글톤 null 레이스 컨디션 방어를 위해 여기서 재구독 처리를 수행합니다.
            if (DepthManager.Instance != null)
            {
                DepthManager.Instance.OnDepthChanged -= HandleDepthChanged;
                DepthManager.Instance.OnDepthChanged += HandleDepthChanged;
            }

            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnGoldChanged -= HandleGoldChanged;
                ShopManager.Instance.OnGoldChanged += HandleGoldChanged;
                ShopManager.Instance.OnUpgradeSuccess -= HandleUpgradeSuccess;
                ShopManager.Instance.OnUpgradeSuccess += HandleUpgradeSuccess;
            }
        }

        private void OnEnable()
        {
            // 플레이어가 수면에 복귀했을 때 상점 자동 켜기
            if (playerSurvival != null)
            {
                playerSurvival.OnSurfaced += HandleSurfaced;
            }

            // 플레이어가 다시 물속으로 잠수해 들어가면 상점 자동 닫기
            if (DepthManager.Instance != null)
            {
                DepthManager.Instance.OnDepthChanged += HandleDepthChanged;
            }

            // 업그레이드 결과 및 재화가 바뀔 때 화면 새로고침
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnGoldChanged += HandleGoldChanged;
                ShopManager.Instance.OnUpgradeSuccess += HandleUpgradeSuccess;
            }
        }

        private void OnDisable()
        {
            if (playerSurvival != null)
            {
                playerSurvival.OnSurfaced -= HandleSurfaced;
            }

            if (DepthManager.Instance != null)
            {
                DepthManager.Instance.OnDepthChanged -= HandleDepthChanged;
            }

            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnGoldChanged -= HandleGoldChanged;
                ShopManager.Instance.OnUpgradeSuccess -= HandleUpgradeSuccess;
            }
        }

        private void FindPlayerDependencies()
        {
            playerSurvival = FindFirstObjectByType<PlayerSurvival>();
        }

        private void InitializeButtons()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseShop);
            }

            if (upgradeOxygenButton != null)
            {
                upgradeOxygenButton.onClick.AddListener(OnOxygenUpgradeClicked);
            }

            if (upgradeCapacityButton != null)
            {
                upgradeCapacityButton.onClick.AddListener(OnCapacityUpgradeClicked);
            }

            if (upgradeSpeedButton != null)
            {
                upgradeSpeedButton.onClick.AddListener(OnSpeedUpgradeClicked);
            }
        }

        /// <summary>
        /// 플레이어가 해수면(Z=0)에 복귀해 surfaced 이벤트가 발생하면 상점 패널을 켭니다.
        /// </summary>
        private void HandleSurfaced()
        {
            OpenShop();
        }

        /// <summary>
        /// 플레이어가 특정 한계선 이상으로 깊이 잠수하면 상점 UI를 자동으로 닫습니다.
        /// </summary>
        private void HandleDepthChanged(float depth)
        {
            // 상점이 열려 있고 플레이어가 잠수 중인 경우 UI 자동 해제
            if (shopPanel != null && shopPanel.activeSelf && depth > 0.5f)
            {
                CloseShop();
            }
        }

        private void HandleGoldChanged(int currentGold)
        {
            if (shopPanel != null && shopPanel.activeSelf)
            {
                UpdateShopVisuals();
            }
        }

        private void HandleUpgradeSuccess()
        {
            if (shopPanel != null && shopPanel.activeSelf)
            {
                UpdateShopVisuals();
            }
        }

        public void OpenShop()
        {
            if (shopPanel == null) return;

            // 런타임 다이내믹 생성을 고려한 방어적 종속성 획득
            if (playerSurvival == null)
            {
                FindPlayerDependencies();
            }

            shopPanel.SetActive(true);
            UpdateShopVisuals();
            Debug.Log("[ShopUIPresenter] 상점 패널 활성화 완료.");
        }

        public void CloseShop()
        {
            if (shopPanel == null) return;
            shopPanel.SetActive(false);
            Debug.Log("[ShopUIPresenter] 상점 패널 비활성화 완료.");
        }

        /// <summary>
        /// 상점 내 모든 업그레이드 항목의 레벨 텍스트, 소모 골드 및 버튼 상호작용성(interactable)을 갱신합니다.
        /// </summary>
        private void UpdateShopVisuals()
        {
            if (ShopManager.Instance == null || ShopManager.Instance.PlayerData == null) return;

            var shop = ShopManager.Instance;
            var data = shop.PlayerData;
            int currentGold = shop.CurrentGold;
            int maxLevel = shop.MaxUpgradeLevel;

            // 1. 산소 용량 업그레이드 정보 갱신
            UpdateItemVisual(
                data.oxygenUpgradeLevel,
                maxLevel,
                shop.GetUpgradeCost(data.oxygenUpgradeLevel),
                currentGold,
                upgradeOxygenButton,
                oxygenStatusText,
                "산소통 용량"
            );

            // 2. 가방 무게 한계 업그레이드 정보 갱신
            UpdateItemVisual(
                data.capacityUpgradeLevel,
                maxLevel,
                shop.GetUpgradeCost(data.capacityUpgradeLevel),
                currentGold,
                upgradeCapacityButton,
                capacityStatusText,
                "망사리 용량"
            );

            // 3. 오리발 헤엄 속도 업그레이드 정보 갱신
            UpdateItemVisual(
                data.speedUpgradeLevel,
                maxLevel,
                shop.GetUpgradeCost(data.speedUpgradeLevel),
                currentGold,
                upgradeSpeedButton,
                speedStatusText,
                "오리발 스피드"
            );
        }

        /// <summary>
        /// 업그레이드 항목별 공통 시각화 유틸리티 함수입니다.
        /// </summary>
        private void UpdateItemVisual(
            int currentLevel,
            int maxLevel,
            int cost,
            int currentGold,
            Button button,
            TextMeshProUGUI text,
            string itemName)
        {
            if (text == null) return;

            bool isMaxed = currentLevel >= maxLevel;

            if (isMaxed)
            {
                text.text = $"{itemName}\n레벨: MAX ({currentLevel} Lvl)";
                if (button != null)
                {
                    button.interactable = false;
                    var btnText = button.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null) btnText.text = "최대 레벨";
                }
            }
            else
            {
                text.text = $"{itemName}\n레벨: {currentLevel} -> {currentLevel + 1}";
                if (button != null)
                {
                    bool canAfford = currentGold >= cost;
                    button.interactable = canAfford;

                    var btnText = button.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null) btnText.text = $"{cost} G";
                }
            }
        }

        private void OnOxygenUpgradeClicked()
        {
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.BuyOxygenUpgrade();
            }
        }

        private void OnCapacityUpgradeClicked()
        {
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.BuyCapacityUpgrade();
            }
        }

        private void OnSpeedUpgradeClicked()
        {
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.BuySpeedUpgrade();
            }
        }
    }
}
