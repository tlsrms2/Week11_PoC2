using UnityEngine;

namespace DeepSea.Utils
{
    /// <summary>
    /// 시니어 레벨의 빌보드 컴포넌트.
    /// 2D 스프라이트가 항상 액티브 카메라 평면을 바라보도록 정렬해 줍니다.
    /// 이를 통해 하이브리드 3D-2D 씬에서 스프라이트가 각도에 따라 비틀리거나 찌그러지는 perspective distortion을 원천 방어합니다.
    /// 오브젝트 자체의 고유 Z축(Roll) 회전값은 그대로 보존하여 플레이어나 몬스터의 360도 이동방향 회전을 방해하지 않습니다.
    /// </summary>
    [ExecuteAlways]
    public class BillboardSprite : MonoBehaviour
    {
        public enum BillboardMode
        {
            LookAtCameraPosition, // 카메라의 3D 좌표를 정면으로 바라봅니다.
            AlignWithCameraPlane  // 카메라의 뷰포트 평면과 완벽히 평행하도록 정렬합니다. (탑뷰/하이브리드 2D 연출에 적극 추천)
        }

        [Header("빌보드 설정")]
        [Tooltip("빌보드가 동작하는 기준 모드입니다. 탑뷰에서는 카메라도 뒤집혀 서있으므로 AlignWithCameraPlane이 왜곡 없이 스프라이트를 띄워줍니다.")]
        [SerializeField] private BillboardMode mode = BillboardMode.AlignWithCameraPlane;

        [Tooltip("Y축 회전만 카메라에 맞추고 X, Z축은 고정합니다. (바닥에 수직으로 똑바로 서있어야 하는 지상 배치물에 유용합니다)")]
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
                // 오브젝트 고유의 360도 롤(Z축) 회전 각도를 보존합니다.
                float currentRoll = transform.eulerAngles.z;

                if (lockYAxisOnly)
                {
                    Vector3 camRotation = mainCamera.transform.eulerAngles;
                    transform.rotation = Quaternion.Euler(0f, camRotation.y, currentRoll);
                }
                else
                {
                    // 카메라 평면의 회전율(X, Y)에 오브젝트 자체의 고유 회전율(Z)을 결합합니다.
                    Vector3 camRotation = mainCamera.transform.eulerAngles;
                    transform.rotation = Quaternion.Euler(camRotation.x, camRotation.y, currentRoll);
                }
            }
            else
            {
                // 카메라의 위치를 직접 바라보게 합니다.
                Vector3 targetDir = mainCamera.transform.position - transform.position;

                if (lockYAxisOnly)
                {
                    targetDir.y = 0f; // 수평 제약
                }

                if (targetDir.sqrMagnitude > 0.001f)
                {
                    // LookRotation에 오브젝트 고유의 롤 회전을 살릴 수 있게 카메라 Up 벡터를 결합해 줍니다.
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
