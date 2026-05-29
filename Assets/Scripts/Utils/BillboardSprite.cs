using UnityEngine;

namespace DeepSea.Utils
{
    /// <summary>
    /// Senior-level Billboard Component.
    /// Ensures 2D sprites always face the active camera plane perfectly.
    /// This prevents perspective distortions (squishing or skewing) in our hybrid 3D-2D scene.
    /// </summary>
    [ExecuteAlways]
    public class BillboardSprite : MonoBehaviour
    {
        public enum BillboardMode
        {
            LookAtCameraPosition, // Point directly at camera's coordinates
            AlignWithCameraPlane  // Align perfectly parallel to the camera's viewport plane (highly recommended for 2D orthographic/flat feel)
        }

        [Header("Settings")]
        [Tooltip("How the billboard behaves. AlignWithCameraPlane is ideal for top-down perspective to avoid angled sprites.")]
        [SerializeField] private BillboardMode mode = BillboardMode.AlignWithCameraPlane;

        [Tooltip("If true, only rotate around the Y axis (great for ground-based objects that should stay upright).")]
        [SerializeField] private bool lockYAxisOnly = false;

        private Camera mainCamera;

        private void Start()
        {
            FindCamera();
        }

        private void LateUpdate()
        {
            if (mainCamera == null)
            {
                FindCamera();
                if (mainCamera == null) return;
            }

            if (mode == BillboardMode.AlignWithCameraPlane)
            {
                // Align with camera viewport rotation
                if (lockYAxisOnly)
                {
                    Vector3 camRotation = mainCamera.transform.eulerAngles;
                    transform.rotation = Quaternion.Euler(0f, camRotation.y, 0f);
                }
                else
                {
                    transform.rotation = mainCamera.transform.rotation;
                }
            }
            else
            {
                // Look directly at camera position
                Vector3 targetDir = mainCamera.transform.position - transform.position;

                if (lockYAxisOnly)
                {
                    targetDir.y = 0f; // Constrain to horizontal plane
                }

                if (targetDir.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(targetDir, mainCamera.transform.up);
                }
            }
        }

        private void FindCamera()
        {
            mainCamera = Camera.main;
        }
    }
}
