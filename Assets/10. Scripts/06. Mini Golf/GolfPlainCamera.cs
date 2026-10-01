using System;
using UnityEngine;

namespace Study3D.MiniGolf
{
    // 오른쪽 마우스 버튼을 드래그 입력을 이용해서 볼을 중심으로 공전하고
    // 휠로 ZoomIn/ZoomOut 하는 역할의 카메라 컴포넌트
    
    public class GolfPlainCamera : MonoBehaviour
    {
        [Header("Target")]
        public Transform target;

        [Header("Settings")] 
        [SerializeField] private float radius; // 반지름 : target과의 거리가 된다.
        [SerializeField] private float yaw; // y축 회전
        [SerializeField] private float pitch; // x축 회전
        [SerializeField] private float minPitch;// x축 회전 최소값
        [SerializeField] private float maxPitch;// x축 회전 최댓값
        [SerializeField] private float followSpeed; // target을 따라가는 속도
        [SerializeField] private Vector3 offset;

        private Vector3 trackedPoint;

        public float Yaw
        {
            get => yaw;
            // 유니티 각도 범위 (-180 ~ 180)를 맞추기 위해 Repeat 처리를 해준다.
            set => yaw = Mathf.Repeat(value + 180, 360) - 180;
        }

        public float Pitch
        {
            get => pitch;
            // 각도 제한을 위해 clamp를 걸어준다.
            set => pitch = Mathf.Clamp(value, minPitch, maxPitch);
        }
        
        public float Radius
        {
            get => radius;
            set => radius = value;
        }
        
        private void Start()
        {
            if (target == null)
            {
                Debug.Log("Target is Null!");
                return;
            }

            trackedPoint = target.position;
        }

        // 모든 Update의 로직이나 값 설정들이 갱신된 후
        // 마지막에 카메라의 위치를 갱신하기 위해 LateUpdate를 사용
        private void LateUpdate()
        {
            if (target == null)
            {
                Debug.Log("Target is Null!");
                return;
            }

            Vector3 currentTargetPosition = target.position;
            
            trackedPoint = 
                Vector3.Lerp(trackedPoint, currentTargetPosition, followSpeed * Time.deltaTime);
            
            // 자신의 프로퍼티들을 이용해서 회전량(쿼터니언)을 생성하고 3차원 좌표 위치(벡터)를 계산한다.
            // 결과는 원점에서부터 Quaternion.Euler(pitch, yaw, 0) 만큼 회전하여
            // Vector3(0f, 0f, -radius) 만큼 떨어진
            // 위치 좌표가 생성(offset)된다.
            Quaternion camRotation = Quaternion.Euler(pitch, yaw, 0); // 나(카메라)의 현재 회전량
            Vector3 distance = new Vector3(0f, 0f, -radius) + offset; // 궤도의 반지름
            // 나의 회전량으로부터 궤도의 반지름 만큼 떨어진 궤도 위치 좌표
            Vector3 orbitPoint = camRotation * distance;
            
            // trackedPoint = ball의 좌표
            // trackedPoint와 offset을 더하면 ball로부터 회전하여 떨어져 있는 위치 좌표가 결과로 나온다.
            transform.position = trackedPoint + orbitPoint;

            /* 아래 내용과 LookAt 함수는 동일한 내용이 나온다.
             Vector3 toTarget = (target.position - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(toTarget, Vector3.up);*/
            transform.LookAt(target);
            
        }
    }
}