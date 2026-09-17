using System;
using UnityEngine;

namespace Study3D.Transforms
{
    // ScalePulse
    // : 사인(Sin) 곡선을 이용해서 크기를 키웠다 줄였다 하는 로직
    // - localScale : 부모 기준의 배율. 부모가 커지면 자식도 함께 커진다.
    // - lossyScale : 부모 배율까지 곱한 월드 기준 크기이고 읽기 전용이다.
    
    public class ScalePulse : MonoBehaviour
    {
        public float amplitude = 0.2f;
        public float pulsePerSecond = 0.5f;

        private Vector3 startScale;

        private void Awake()
        {
            startScale = transform.localScale;
        }

        private void Update()
        {
            // Mathf.PI * 2f = 유니티에서 원을 표현하는 방식
            
            float wave = Mathf.Sin(Time.time * pulsePerSecond * Mathf.PI * 2f);
            Debug.Log(wave);
            transform.localScale = startScale * (1f + wave * amplitude);
        }
    }
}