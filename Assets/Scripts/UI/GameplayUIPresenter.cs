using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeepSea.Environment;
using DeepSea.Player;

namespace DeepSea.UI
{
    /// <summary>
    /// Senior-level Gameplay UI Presenter (MVP pattern).
    /// Decouples Player Survival and Depth states from specific UI Rendering components.
    /// Real-time updates for:
    /// 1. Depth UI: Displays "Current Depth / Max Depth Limit" in the top-center screen.
    /// 2. Oxygen UI: Dynamic Vertical Oxygen Slider on the right with numerical precision, 
    ///    plus dynamic color warning (Green -> Yellow -> Red) when oxygen drops critically.
    /// </summary>
    public class GameplayUIPresenter : MonoBehaviour
    {
        [Header("Depth UI References")]
        [Tooltip("TextMeshPro text showing: 'Current Depth m / Max Depth m' at top center.")]
        [SerializeField] private TextMeshProUGUI depthText;

        [Header("Oxygen UI References")]
        [Tooltip("Vertical Slider showing oxygen levels on the right screen.")]
        [SerializeField] private Slider oxygenSlider;

        [Tooltip("TextMeshPro text showing precise oxygen values below the slider.")]
        [SerializeField] private TextMeshProUGUI oxygenText;

        [Header("Oxygen Color Warning (Optional Polish)")]
        [Tooltip("Image component of the slider fill area to change color dynamically.")]
        [SerializeField] private Image sliderFillImage;

        [SerializeField] private Color normalOxygenColor = new Color(0.2f, 0.8f, 0.3f);   // Clean Green
        [SerializeField] private Color warningOxygenColor = new Color(0.9f, 0.7f, 0.1f);  // Caution Yellow
        [SerializeField] private Color criticalOxygenColor = new Color(0.9f, 0.1f, 0.1f); // Danger Red

        // Cached values
        private PlayerSurvival playerSurvival;
        private PlayerController playerController;
        private float maxDepthLimitInMeters = 200f;

        private void Start()
        {
            FindPlayerDependencies();
        }

        private void OnEnable()
        {
            // Subscribe to DepthManager events
            if (DepthManager.Instance != null)
            {
                DepthManager.Instance.OnDepthChanged += HandleDepthChanged;
            }

            // Sync with PlayerSurvival events
            if (playerSurvival != null)
            {
                playerSurvival.OnOxygenChanged += HandleOxygenChanged;
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
        }

        private void FindPlayerDependencies()
        {
            playerSurvival = FindFirstObjectByType<PlayerSurvival>();
            playerController = FindFirstObjectByType<PlayerController>();

            if (playerController != null)
            {
                // Retrieve max Z-depth constraint dynamically from PlayerController
                maxDepthLimitInMeters = playerController.MaxDepthLimitInMeters;
            }

            // Re-subscribe if found dynamically
            if (playerSurvival != null)
            {
                playerSurvival.OnOxygenChanged -= HandleOxygenChanged; // Avoid double subscription
                playerSurvival.OnOxygenChanged += HandleOxygenChanged;
                
                // Initial display sync
                HandleOxygenChanged(playerSurvival.CurrentOxygen, 100f); // Max will be overridden on event callback
            }

            if (DepthManager.Instance != null)
            {
                HandleDepthChanged(DepthManager.Instance.CurrentDepth);
            }
        }

        private void HandleDepthChanged(float currentDepth)
        {
            if (depthText == null) return;

            // Display "Current Depth / Max Depth" (e.g. "42.5m / 200m")
            depthText.text = $"{currentDepth:F1}m / {maxDepthLimitInMeters:F0}m";
        }

        private void HandleOxygenChanged(float currentOxygen, float maxOxygen)
        {
            if (oxygenSlider == null || oxygenText == null) return;

            // 1. Sync Slider levels
            oxygenSlider.maxValue = maxOxygen;
            oxygenSlider.value = currentOxygen;

            // 2. Sync numerical precise text below the slider
            oxygenText.text = $"{Mathf.CeilToInt(currentOxygen)} / {Mathf.CeilToInt(maxOxygen)}";

            // 3. Dynamic Color Warning polish (Green -> Yellow -> Red)
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
                    
                    // Simple text warning pulse color
                    oxygenText.color = criticalOxygenColor;
                }

                if (oxygenPercent > 0.2f)
                {
                    oxygenText.color = Color.white; // Restore clean white
                }
            }
        }

        /// <summary>
        /// Call this to force update UI if dependencies were changed.
        /// </summary>
        public void RefreshUI()
        {
            FindPlayerDependencies();
        }
    }
}
