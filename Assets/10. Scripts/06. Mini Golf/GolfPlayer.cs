using System;
using Study.Utilities;
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
        private float power01; // 0~1 까지로 치환한 세기

        private void Awake()
        {
            mainCam = Camera.main;
        }
        
        private void Update()
        {
            if (SimpleInput.GetKey(Key.Space)) // power charge
            {
                chargeTime += Time.deltaTime;
                // PingPong 함수를 사용해서 왔다리 갔다리 하는 표현을 해줍시다.
                power01 = Mathf.PingPong(chargeTime / cycleTime, 1);
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