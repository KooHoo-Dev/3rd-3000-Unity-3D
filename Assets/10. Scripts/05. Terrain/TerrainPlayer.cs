using System;
using Study.Utilities;
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
        }

        private void Move(Vector2 input)
        {
            if (input == Vector2.zero) return;
            // 이동벡터 = 방향벡터 + 이동량(길이)
            // 방향벡터를 만들어 줍니다. 입력값을 이용해서
            // 길이가 1인 벡터로 정규화(normalize)하여 direction에 저장해 줍니다
            Vector3 direction = new Vector3(input.x, 0.0f, input.y).normalized;
            float moveValue = moveSpeed * Time.deltaTime;
            
            // 방향은 direction과 동일하고 길이는 moveValue와 동일한
            // 이동 벡터를 만들어 줍니다.
            Vector3 moveVector = direction * moveValue;
            transform.position += moveVector;
        }

        private void UpdateHeight()
        {
            Vector3 currentPosition = transform.position;
            
            // 나의 XZ위치에서 Terrain의 y값을 구해줍니다.
            float targetY = terrain.SampleHeight(currentPosition) 
                            + terrain.GetPosition().y + heightOffset;
            
            currentPosition.y = targetY;
            transform.position = currentPosition;
        }
    }
}