using System;
using UnityEngine;
using UnityEngine.LowLevelPhysics2D;

namespace Study3D.MiniGolf
{
    // 방향과 힘을 받아서 움직이고, 현재 공이 운동을 하는지 안 하는지 알려주는 공
    
    public class GolfBall : MonoBehaviour
    {
        [Header("Settings")] 
        public float stopSpeed = 1f;
        public float stopDelay;
        // 공을 다이나믹으로 사용하기 때문에
        // 게임적인 표현을 회전 스피드에 제한을 활용하여 간접적으로 제어 한다
        public float maxAngularSpeed = 150f;

        private Rigidbody rBody;
        private Vector3 lastPosition; // 유저가 마지막으로 공을 건드린 위치
        private Vector3 shotVelocity; // 공의 방향 X 세기가 연산되는 변수
        private bool shotRequested;
        private float slowTime;

        private void Awake()
        {
            rBody = GetComponent<Rigidbody>();
            rBody.maxAngularVelocity = maxAngularSpeed;
            lastPosition = transform.position;
        }

        // 공의 물리를 사용할 예정이라 Update가 아닌 FixedUpdate를 사용한다 
        private void FixedUpdate()
        {
            if (shotRequested)
            {
                shotRequested = false;
                rBody.AddForce(shotVelocity, ForceMode.VelocityChange);
                return;
            }
        }

        // 외부에서 Shoot함수를 호출하여 동작하도록 public으로 선언
        public void Shoot(Vector3 direction, float power)
        {
            shotRequested = true;
            shotVelocity = direction * power;
            lastPosition = transform.position;
        }
    }
}