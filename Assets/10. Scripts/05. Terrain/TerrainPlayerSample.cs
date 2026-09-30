using Study.Utilities;
using UnityEngine;

namespace Study3D.Temp
{
    public class TerrainPlayerSample : MonoBehaviour
    {
        [Header("Terrain")]
        [SerializeField] private Terrain terrain;

        [Header("Move")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float turnSpeed = 12f;

        [Header("Height")]
        [SerializeField] private float heightOffset = 0f;
        [SerializeField] private float heightSmoothTime = 0.08f;

        [Header("Slope")]
        [SerializeField] private bool alignToSlope = true;
        [SerializeField] private float slopeAlignSpeed = 10f;

        private TerrainData terrainData;
        private Vector3 terrainOrigin;
        private float heightVelocity;
        private float yaw;

        private void Awake()
        {
            if (terrain == null)
                terrain = Terrain.activeTerrain;

            terrainData = terrain.terrainData;
            terrainOrigin = terrain.transform.position;
            yaw = transform.eulerAngles.y;
        }

        private void Update()
        {
            Move(SimpleInput.GetMoveAxisRaw());
            UpdateHeight();
            AlignRotation();
        }

        private void Move(Vector2 input)
        {
            if (input == Vector2.zero) return;

            var direction = new Vector3(input.x, 0f, input.y);
            transform.position = transform.position + direction * (moveSpeed * Time.deltaTime);

            // 이동 방향을 바라보도록 yaw 갱신
            var targetYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            yaw = Mathf.LerpAngle(yaw, targetYaw, turnSpeed * Time.deltaTime);
        }

        private void UpdateHeight()
        {
            var position = transform.position;

            // SampleHeight는 Terrain 기준 상대 높이를 반환하므로 Terrain의 Y 위치를 더해준다
            var targetY = terrain.SampleHeight(position) + terrainOrigin.y + heightOffset;

            position.y = Mathf.SmoothDamp(position.y, targetY, ref heightVelocity, heightSmoothTime);
            transform.position = position;
        }

        private void AlignRotation()
        {
            var yawRotation = Quaternion.Euler(0f, yaw, 0f);
            var targetRotation = yawRotation;

            if (alignToSlope)
            {
                var normal = GetTerrainNormal(transform.position);
                targetRotation = Quaternion.FromToRotation(Vector3.up, normal) * yawRotation;
            }

            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRotation, slopeAlignSpeed * Time.deltaTime);
        }
        
        private Vector3 GetTerrainNormal(Vector3 worldPosition)
        {
            var size = terrainData.size;
            var normalizedX = Mathf.InverseLerp(0f, size.x, worldPosition.x - terrainOrigin.x);
            var normalizedZ = Mathf.InverseLerp(0f, size.z, worldPosition.z - terrainOrigin.z);

            return terrainData.GetInterpolatedNormal(normalizedX, normalizedZ);
        }
    }
}