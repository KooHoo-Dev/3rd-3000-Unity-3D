using System;
using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Study3D.ThreeDPhysics
{
    // ForceMode
    // - Force          : 연속적으로 작용하는 힘. 질량의 영향을 받음
    // - Acceleration   : 연속적으로 작용하는 힘. 질량의 영향을 받지 않음
    // - Impulse        : 순간적으로 작용하는 힘. 질량의 영향을 받음
    // - VelocityChange : 순간적으로 작용하는 힘. 질량의 영향을 받지 않음
    
    // 적용 사례
    // Force : 자동차 엔진, 바람 등등
    // Acceleration : 커스텀 중력, 에스컬레이터, 엘레베이터(사람이 많아도 일정 속도라고 가정) 등등
    // Impulse : 포발, 점프, 발사체, 타격 등등 보통 가장 많이 쓴다.
    // VelocityChange : 대시, 즉각적인 정지 등등 게임적인 움직임에 많이 쓴다.
    
    public class ForceModeLauncher : MonoBehaviour
    {
        public ForceMode forceMode = ForceMode.Force;
        public Vector3 direction = Vector3.forward;
        public float amount = 5f;

        public Key key = Key.Digit1;

        private Rigidbody body;
        private bool holding;
        private bool pushRequested;
        
        private void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            bool isPressed = SimpleInput.GetKey(key);
            bool isContinuous = forceMode is ForceMode.Force or ForceMode.Acceleration;
            holding = isPressed && isContinuous;

            // 그냥 미는 개체 처리
            if (SimpleInput.GetKeyDown(key) && isContinuous == false)
            {
                pushRequested = true;
            }
        }

        private void FixedUpdate()
        {
            // 힘(방향 X 세기)을 구해준다
            Vector3 force = direction.normalized * amount;

            if (holding)
            {
                // holding = 연속 개체 처리 입력
                // 연속 모드(Force or Acceleration)는 매 스텝(FixedUpdate)마다
                // 다시 AddForce를 해줘야 힘이 이어진다. 
                body.AddForce(force, forceMode);
            }

            if (pushRequested)
            {
                // 단발성 모드(Impulse or VelocityChange)는 한 번만 처리.
                pushRequested = false;
                body.AddForce(force, forceMode);
            }
        }
    }
}