using UnityEngine;

namespace _02._Scripts.Entity
{
    [RequireComponent(typeof(LineRenderer))]
    public class LaunchLine : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float maxDistance = 10f;
        [SerializeField] private LayerMask bounceLayer;

        private LineRenderer _lineRenderer;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.positionCount = 3;
        }

        private void Update()
        {
            DrawReflectingLine();
        }

        private void DrawReflectingLine()
        {
            Vector3 startPos = transform.position;
            Vector3 direction = transform.up;

            RaycastHit hit;
            _lineRenderer.SetPosition(0, startPos);

            // 1차 광선 발사
            if (Physics.Raycast(startPos, direction, out hit, maxDistance, bounceLayer))
            {
                // 벽에 닿았을 경우
                Vector3 hitPoint = hit.point;
                _lineRenderer.SetPosition(1, hitPoint);

                // 반사 방향 계산
                Vector3 reflectDir = Vector3.Reflect(direction, hit.normal);
                float remainingDist = maxDistance - hit.distance;

                // 2차 광선 발사 (반사된 경로)
                if (Physics.Raycast(hitPoint + (reflectDir * 0.01f), reflectDir, out RaycastHit hit2, remainingDist))
                {
                    _lineRenderer.SetPosition(2, hit2.point);
                }
                else
                {
                    _lineRenderer.SetPosition(2, hitPoint + (reflectDir * remainingDist));
                }
            }
            else
            {
                // 벽에 닿지 않았을 경우 직선으로 표시
                _lineRenderer.SetPosition(1, startPos + (direction * maxDistance));
                _lineRenderer.SetPosition(2, startPos + (direction * maxDistance));
            }
        }
    }
}