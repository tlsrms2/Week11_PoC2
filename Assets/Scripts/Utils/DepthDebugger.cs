using UnityEngine;
using DeepSea.Player;
using DeepSea.Core;

namespace DeepSea.Utils
{
    /// <summary>
    /// Senior-level Developer Debug Utility.
    /// Exposes a clean, interactive screen-space OnGUI panel to speed up QA and balance testing.
    /// Provides shortcuts for God Mode (infinite oxygen), spawning gold, teleporting between depth layers, and spawning resources.
    /// Safely wraps functionality so it can only compile/run in Editor or Development Builds.
    /// </summary>
    public class DepthDebugger : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Header("Debug Settings")]
        [Tooltip("The hotkey toggle for opening the Debug GUI panel.")]
        [SerializeField] private KeyCode debugPanelToggleKey = KeyCode.F12;

        [Tooltip("Prefab to spawn when pressing Debug Spawn.")]
        [SerializeField] private GameObject resourcePrefabToSpawn;

        private bool showPanel = false;
        private bool godModeActive = false;

        private PlayerSurvival playerSurvival;
        private PlayerController playerController;

        private void Start()
        {
            FindPlayerDependencies();
        }

        private void Update()
        {
            if (Input.GetKeyDown(debugPanelToggleKey))
            {
                showPanel = !showPanel;
            }

            if (godModeActive && playerSurvival != null)
            {
                playerSurvival.RefillOxygen();
            }
        }

        private void FindPlayerDependencies()
        {
            playerSurvival = FindFirstObjectByType<PlayerSurvival>();
            playerController = FindFirstObjectByType<PlayerController>();
        }

        private void OnGUI()
        {
            if (!showPanel) return;

            // Simple styled GUI box
            GUI.Box(new Rect(10, 10, 240, 310), "💎 DEEP SEA DEVELOPMENT DEBUGGER");

            if (playerSurvival == null || playerController == null)
            {
                if (GUI.Button(new Rect(20, 40, 220, 30), "Find Player in Scene"))
                {
                    FindPlayerDependencies();
                }
                return;
            }

            // 1. Survival Controls
            string godBtnLabel = godModeActive ? "Disable GOD MODE (Infinite Oxygen)" : "Enable GOD MODE (Infinite Oxygen)";
            if (GUI.Button(new Rect(20, 40, 220, 30), godBtnLabel))
            {
                godModeActive = !godModeActive;
                Debug.Log($"[DepthDebugger] God Mode toggled: {godModeActive}");
            }

            // 2. Economy Controls
            if (GUI.Button(new Rect(20, 80, 220, 30), "Add +1000 Gold"))
            {
                if (ShopManager.Instance != null)
                {
                    ShopManager.Instance.AddGold(1000);
                }
            }

            // 3. Teleportation Controls
            GUI.Label(new Rect(20, 120, 220, 20), "Teleport to Depth Layer:");
            if (GUI.Button(new Rect(20, 140, 220, 30), "Surface (0m - Shallow)"))
            {
                TeleportPlayer(0f);
            }
            if (GUI.Button(new Rect(20, 180, 220, 30), "Twilight Zone (40m - Mid)"))
            {
                TeleportPlayer(-40f);
            }
            if (GUI.Button(new Rect(20, 220, 220, 30), "Midnight Zone (90m - Deep)"))
            {
                TeleportPlayer(-90f);
            }

            // 4. Cheat Spawn Controls
            if (GUI.Button(new Rect(20, 260, 220, 30), "Spawn Test Resource in front"))
            {
                SpawnTestResource();
            }

            // Status display
            GUI.Label(new Rect(20, 292, 220, 20), $"Current Z Depth: {playerController.transform.position.z:F2}m");
        }

        private void TeleportPlayer(float zCoordinate)
        {
            if (playerController == null) return;
            Vector3 pos = playerController.transform.position;
            pos.z = zCoordinate;
            playerController.transform.position = pos;
            
            // Re-sync camera instantly
            var cam = FindFirstObjectByType<DepthCameraController>();
            if (cam != null)
            {
                cam.SetTarget(playerController.transform);
            }
            
            Debug.Log($"[DepthDebugger] Teleported player to Z: {zCoordinate}");
        }

        private void SpawnTestResource()
        {
            if (playerController == null) return;
            if (resourcePrefabToSpawn == null)
            {
                Debug.LogWarning("[DepthDebugger] No resource prefab assigned to debugger!");
                return;
            }

            // Spawn 2 units in front (Y+) of player, on same Z layer
            Vector3 spawnPos = playerController.transform.position;
            spawnPos.y += 2f; 
            
            Instantiate(resourcePrefabToSpawn, spawnPos, Quaternion.identity);
            Debug.Log($"[DepthDebugger] Spawned test resource at {spawnPos}");
        }
#endif
    }
}
