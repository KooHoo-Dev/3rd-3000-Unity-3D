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

        public bool IsStopped => (shotRequested == false) && slowTime >= stopDelay;

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

            if (rBody.linearVelocity.magnitude < stopSpeed)
            {
                slowTime += Time.fixedDeltaTime;
            }
            else
            {
                slowTime = 0;
            }
        }

        // 외부에서 Shoot함수를 호출하여 동작하도록 public으로 선언
        public void Shoot(Vector3 direction, float power)
        {
            shotRequested = true;
            shotVelocity = direction * power;
            lastPosition = transform.position;
        }

        // 공을 position의 위치로 옮기고
        // 다시 Shoot을 할 수 있는 상태로 바꿔준다
        private void Teleport(Vector3 position)
        {
            shotRequested = false;
            // 리지드 바디의 운동량(선 운동량, 각 운동량)을 0으로 바꿔준다.
            rBody.linearVelocity = Vector3.zero;
            rBody.angularVelocity = Vector3.zero;
            
            // 공을 물리 처리 하는 개체이므로
            // transform과 리지드 바디의 position을 모두 변경해 준다.
            // (Interpolate를 사용하는 Rigidbody는 transform도 함께 옮겨야
            // 끌려오듯이 안 보인다.)
            rBody.position = position;
            transform.position = position;
        }

        // public으로 선언해서 외부에서 호출 가능하게 한다.
        public void ReturnToLastShotPosition()
        {
            Teleport(lastPosition);
        }

        // 스테이지 시작시 공을 놓기 위한 함수
        public void PlaceAt(Vector3 position)
        {
            lastPosition = position;
            Teleport(position);
        }
    }
}