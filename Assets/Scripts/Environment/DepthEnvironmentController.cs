using UnityEngine;
#if UNITY_POST_PROCESSING_STACK_V2 || UNITY_2019_1_OR_NEWER
using UnityEngine.Rendering;
#endif
using DeepSea.Environment;

namespace DeepSea.Environment
{
    /// <summary>
    /// Senior-level Environmental Visual Controller.
    /// Uses DepthManager events to dynamically adjust Unity Fog settings and URP Post-Processing Volume weights.
    /// Simulates rising turbidity (fog density) and shifts the color scheme based on water depth:
    /// - Shallow (0m-30m): Sunny blue
    /// - Mid (30m-80m): Dense dark teal
    /// - Deep (80m+): Faintly lit navy-black
    /// </summary>
    public class DepthEnvironmentController : MonoBehaviour
    {
#if UNITY_POST_PROCESSING_STACK_V2 || UNITY_2019_1_OR_NEWER
        [Header("URP Post-Processing Volume")]
        [Tooltip("The URP Volume associated with deep sea effects (e.g. Vignette, film grain, darkness).")]
        [SerializeField] private Volume deepSeaVolume;
#endif

        [Header("Fog Settings (Turbidity)")]
        [Tooltip("Enable Unity built-in fog control.")]
        [SerializeField] private bool manageFog = true;

        [Tooltip("Fog color at the sunny surface (Shallow).")]
        [SerializeField] private Color shallowFogColor = new Color(0.2f, 0.5f, 0.8f); // Bright Cyan Blue

        [Tooltip("Fog color in the twilight zone (Mid).")]
        [SerializeField] private Color midFogColor = new Color(0.05f, 0.2f, 0.3f);    // Deep Teal

        [Tooltip("Fog color in the midnight zone (Deep).")]
        [SerializeField] private Color deepFogColor = new Color(0.01f, 0.02f, 0.05f); // Navy Black

        [Space]
        [Tooltip("Fog density at the surface (clear water).")]
        [SerializeField] private float shallowFogDensity = 0.01f;

        [Tooltip("Fog density at maximum turbidity in the deep sea (extremely low visibility).")]
        [SerializeField] private float maxDeepFogDensity = 0.12f;

        private void OnEnable()
        {
            if (DepthManager.Instance != null)
            {
                DepthManager.Instance.OnDepthChanged += OnPlayerDepthChanged;
                // Run initial setup
                OnPlayerDepthChanged(DepthManager.Instance.CurrentDepth);
            }
        }

        private void OnDisable()
        {
            if (DepthManager.Instance != null)
            {
                DepthManager.Instance.OnDepthChanged -= OnPlayerDepthChanged;
            }
        }

        private void Start()
        {
            if (manageFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
            }

            // 시니어 팁: OnEnable 시점 싱글톤 null 레이스 컨디션 방어를 위해 여기서 재구독 및 초기 상태 동기화를 진행합니다.
            if (DepthManager.Instance != null)
            {
                DepthManager.Instance.OnDepthChanged -= OnPlayerDepthChanged;
                DepthManager.Instance.OnDepthChanged += OnPlayerDepthChanged;
                OnPlayerDepthChanged(DepthManager.Instance.CurrentDepth);
            }
        }

        private void OnPlayerDepthChanged(float depth)
        {
            float midThreshold = DepthManager.Instance != null ? DepthManager.Instance.MidZoneThreshold : 30f;
            float deepThreshold = DepthManager.Instance != null ? DepthManager.Instance.DeepZoneThreshold : 80f;

            // 1. Calculate color blending and fog density based on depth
            Color activeFogColor;
            float activeFogDensity;
            float deepVolumeWeight = 0f;

            if (depth < midThreshold)
            {
                // Shallow -> Mid transition
                float t = Mathf.Clamp01(depth / midThreshold);
                activeFogColor = Color.Lerp(shallowFogColor, midFogColor, t);
                activeFogDensity = Mathf.Lerp(shallowFogDensity, shallowFogDensity * 2f, t);
                deepVolumeWeight = 0f;
            }
            else if (depth < deepThreshold)
            {
                // Mid -> Deep transition
                float range = deepThreshold - midThreshold;
                float t = Mathf.Clamp01((depth - midThreshold) / range);
                activeFogColor = Color.Lerp(midFogColor, deepFogColor, t);
                activeFogDensity = Mathf.Lerp(shallowFogDensity * 2f, maxDeepFogDensity * 0.5f, t);
                deepVolumeWeight = Mathf.Lerp(0f, 0.6f, t);
            }
            else
            {
                // Deep midnight zone (80m+)
                // Fog gets extremely thick up to a maximum limit of 150m.
                float t = Mathf.Clamp01((depth - deepThreshold) / 70f); // Interpolate between 80m and 150m
                activeFogColor = deepFogColor;
                activeFogDensity = Mathf.Lerp(maxDeepFogDensity * 0.5f, maxDeepFogDensity, t);
                deepVolumeWeight = Mathf.Lerp(0.6f, 1.0f, t);
            }

            // 2. Apply Fog changes
            if (manageFog)
            {
                RenderSettings.fogColor = activeFogColor;
                RenderSettings.fogDensity = activeFogDensity;

                // Sync main camera's background clear color with fog for flawless immersion
                if (Camera.main != null)
                {
                    Camera.main.backgroundColor = activeFogColor;
                }
            }

            // 3. Apply URP Post Processing volume weights
#if UNITY_POST_PROCESSING_STACK_V2 || UNITY_2019_1_OR_NEWER
            if (deepSeaVolume != null)
            {
                deepSeaVolume.weight = deepVolumeWeight;
            }
#endif
        }
    }
}
