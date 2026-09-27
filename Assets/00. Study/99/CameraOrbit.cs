using UnityEngine;

public class CameraOrbit : MonoBehaviour
{
    [Header("타겟 설정")]
    [Tooltip("카메라가 조망할 중심 오브젝트")]
    public Transform target;
    [Tooltip("타겟과의 거리")]
    public float distance = 10f;
    [Tooltip("카메라의 높이")]
    public float height = 5f;

    [Header("회전 설정")]
    [Tooltip("초당 회전 속도 (도/초)")]
    public float orbitSpeed = 20f;

    private float currentAngle = 0f;

    private void LateUpdate()
    {
        if (target == null) return;

        // 시간에 따른 회전 각도 계산
        currentAngle += orbitSpeed * Time.deltaTime;
        float radians = currentAngle * Mathf.Deg2Rad;

        // 삼각함수로 원형 궤도 좌표 계산
        float x = target.position.x + Mathf.Cos(radians) * distance;
        float z = target.position.z + Mathf.Sin(radians) * distance;
        float y = target.position.y + height;

        // 카메라 위치 적용 및 타겟 바라보기
        transform.position = new Vector3(x, y, z);
        transform.LookAt(target.position);
    }
}