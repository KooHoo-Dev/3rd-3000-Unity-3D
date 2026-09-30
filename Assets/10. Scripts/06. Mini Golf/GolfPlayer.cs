using System;
using Study.Utilities;
using Unity.Android.Gradle;
using UnityEngine;
using UnityEngine.InputSystem;

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
        
        private float chargeTime;
        private Camera mainCam;
        private float power01; // 0~1 까지로 세기 표현
        
        private void Awake()
        {
            mainCam = Camera.main;
        }

        private void Update()
        {
            if (SimpleInput.GetKey(Key.Space)) // power charge
            {
                chargeTime += Time.deltaTime;
                
                // PingPong 함수를 사용해 왔다 갔다 하는 표현을 해준다.
                power01 = Mathf.PingPong(chargeTime / cycleTime, 1f);
            }
            else if (SimpleInput.GetKeyUp(Key.Space)) // 입력 전달
            {
                Vector3 camDirection = mainCam.transform.forward;
                float power = Mathf.Lerp(minPower, maxPower, power01);
                
                ball.Shoot(camDirection, power);
            }
        }
    }
}