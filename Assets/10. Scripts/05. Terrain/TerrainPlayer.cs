using System;
using Study.Utilities;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

namespace Study3D.Terrains
{
    public class TerrainPlayer : MonoBehaviour
    {
        [Header("Move Settings")]
        public float moveSpeed = 50f;
        public float rotateSpeed = 12f;

        [Header("Height Settings")]
        public float heightOffset = 7f;
        public float heightSmoothTime = 0.01f; // 보간에 사용되는 값
        
        private Terrain terrain;
        private TerrainData terrainData;
        private float heightVelocity;
        private float yaw; // y축 회전 처리에 사용되는 값
        private Vector3 terrainOrigin;

        [Header("Rotation Settings")]
        public bool alignToSlope = false;
        public float slopeAlignSpeed = 10;
        
        private void Awake()
        {
            terrain = Terrain.activeTerrain;
            terrainData = terrain.terrainData;
            terrainOrigin = terrain.transform.position;
            yaw = terrain.transform.eulerAngles.y;
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
            
            // 이동벡터 = 방향벡터 +이동량(길이)
            // 입력값을 이용해서 길이가 1인 정규화(normalized)된 방향 벡터를 만들어 준다.
            Vector3 direction = new Vector3(input.x, 0, input.y).normalized;
            float moveValue = moveSpeed * Time.deltaTime;
            
            // 방향은 direction과 동일하고 길이는 moveValue와 동일한
            // 이동 벡터를 만들어 준다.
            Vector3 moveVector = direction * moveValue;
            transform.position += moveVector;
        }

        private void UpdateHeight()
        {
            Vector3 currentPosition = transform.position;
            
            // 나의 XZ위치에서 Terrain의 Y값을 구해준다.
            float targetY = terrain.SampleHeight(currentPosition) + 
                            terrain.GetPosition().y + heightOffset;

            currentPosition.y = targetY;
            transform.position = currentPosition;
        }

        private void AlignRotation()
        {
            // 터레인의 표면에 따라서 물체를 회전하게 하려면?
            // - 터레인의 표면의 방향(노말)을 알아야 함
            // - 내 현재 회전량을 알아야 함
            // - 두 수를 이용해서 뭔가를 하면 됨
            
            Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);
            Quaternion targetRotation = yawRotation;

            if (alignToSlope)
            {
                // 물체의 노말을 가져와서 회전량을 생성하는 로직
                Vector3 normal = GetTerrainNormal(transform.position);
                
                // Vector3.up를 기준으로 normal 방향 벡터로 회전하는 회전량을 생성한다.
                // Quaternion.FromToRotation(Vector3 from, vector3 to) ?
                // : from 방향 벡터를 기준으로 to 방향 벡터의 회전량을 생성한다.
                //   (from 방향 벡터에서 to 방향 벡터로 회전하는 Quaternion을 만든다.)
                Quaternion normalRotation = Quaternion.FromToRotation(Vector3.up, normal);
                targetRotation *= normalRotation;
            }

            // 회전을 적용 하는 로직
            float lerpSpeed = slopeAlignSpeed * Time.deltaTime;
            
            // Slerp ? 
            // : 구(Sphere)면 선형 보간. 두 값 사이를 구의 곡면(호)에 따라서 부드럽게 보간하는 방법.
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lerpSpeed);
        }

        private Vector3 GetTerrainNormal(Vector3 worldPosition)
        {
            Vector3 size = terrainData.size;
            
            // InverseLerp ? 
            // : 본래의 Lerp가 (시작, 끝, t(시간 또는 진행도))로 값을 추정하는 계산법이라면
            //   InverseLerp는 (시작, 끝, 시간 또는 진행도)를 넣어서 t를 추정하는 계산법
            //   반환이 0~1값으로 나온다.(퍼센트, 비율로 나온다)
            float normalizedX = Mathf.InverseLerp(0f, size.x, worldPosition.x);
            float normalizedZ = Mathf.InverseLerp(0f, size.z, worldPosition.z);

            // GetInterpolatedNormal ?
            // : 
            return terrainData.GetInterpolatedNormal(normalizedX, normalizedZ);
        }
    }
}