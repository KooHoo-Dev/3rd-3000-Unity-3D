using System;
using Study.Utilities;
using Unity.Android.Gradle;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Study3D.MiniGolf
{
    public class GolfPlayer : MonoBehaviour
    {
        [Header("Settings")]
        public float minPower;
        public float maxPower;
        public float cycleTime;

        [Header("Ref")] 
        public GolfBall ball;
        public GolfAimIndicator aimIndicator;
        
        private float chargeTime;
        private Camera mainCam;
        private float power01; // 0~1 까지로 세기 표현
        
        private void Awake()
        {
            mainCam = Camera.main;
        }

        private void Update()
        {
            Vector3 aimDirection = mainCam.transform.forward;
            aimDirection.y = 0.0f;
            aimDirection.Normalize();

            bool canShoot = ball.IsStopped;

            if (canShoot)
            {
                if (SimpleInput.GetKey(Key.Space)) // power charge
                {
                    chargeTime += Time.deltaTime;
                
                    // PingPong 함수를 사용해 왔다 갔다 하는 표현을 해준다.
                    power01 = Mathf.PingPong(chargeTime / cycleTime, 1f);
                }
                else if (SimpleInput.GetKeyUp(Key.Space)) // 입력 전달
                {
                    float power = Mathf.Lerp(minPower, maxPower, power01);
                    ball.Shoot(aimDirection, power);
                    chargeTime = 0.0f;
                    power01 = 0.0f;
                }   
            }
            
            UpdateFeedback(aimDirection,canShoot);
        }

        // 보통 플레이어의 입력정보들에 관련된 UI나 피드백들은
        // 하나의 함수로 모아두는게 좋다.
        // (색각보다 엄청 많이 바뀌니 찢어놓지 말 것)
        private void UpdateFeedback(Vector3 direction, bool canShot)
        {
            if (canShot)
                aimIndicator.Show(ball.transform.position, direction, power01);
            else
                aimIndicator.Hide();
            
        }
    }
}