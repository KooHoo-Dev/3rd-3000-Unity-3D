using System;
using UnityEngine;

namespace Study3D.Rendering
{
    // SunCycle
    // : Directional Light를 회전 시켜서 하루의 해를 구현
    // - Directional Light는 오직 회전만 의미가 있다. 위치는 반영되지 않는다.
    // - X축의 각도가 곧 해의 높이 이다.
    // - Gradient로 시간대 별 빛 색을, 해의 높이로 밝기를 정한다.
    
    public class SunCycle : MonoBehaviour
    {
        [Header("Day Settings")]
        public float dayLightSeconds = 20f;
        public float startAngle = 50f;

        [Header("Sun Settings")] 
        public Gradient colorOverDay = CreateDefaultGradient();
        public float maxIntensity = 1.2f;

        private Light sun;
        private float angle;
        private float yaw; // 일단 y angle 값이라고 생각하면 된다
        
        private static Gradient CreateDefaultGradient()
        {
            var gradient = new Gradient();

            var colorKeys = new[]
            {
                new GradientColorKey(new Color(1f, 0.55f, 0.25f), 0.00f),
                new GradientColorKey(new Color(1f, 0.97f, 0.90f), 0.25f),
                new GradientColorKey(new Color(1f, 0.50f, 0.25f), 0.50f),
                new GradientColorKey(new Color(0.15f, 0.15f, 0.35f), 0.75f),
                new GradientColorKey(new Color(1f, 0.55f, 0.25f), 1.00f),
            };

            var alphaKeys = new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f),
            };

            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }

        // Reset() 함수를 구현해 두면 오브젝트에 스크립트를 새로 붙일 때나
        // 인스펙터에서 Reset 버튼을 누를 때마다
        // 유니티가 자동으로 이 함수를 실행하여 기본값을 깔끔하게 세팅해 준다.
        private void Reset()
        {
            colorOverDay = CreateDefaultGradient();
        }

        private void Awake()
        {
            sun = GetComponent<Light>();
            yaw = transform.eulerAngles.y;
            angle = startAngle;
        }

        private void Update()
        {
            // 현재 angle을 계산해 준다.
            float deltaAngle = (360f / dayLightSeconds) * Time.deltaTime;

            angle = Mathf.Repeat(angle + deltaAngle, 360);
            transform.rotation = Quaternion.Euler(angle, yaw, 0f);
            
            // sun의 color를 가져온다 (현재 각도 / 360 => 백분율(0~1f 갑으로 바꿔줌))
            sun.color = colorOverDay.Evaluate(angle / 360);
            
            // 해가 지평선 아래로 내려가면 밝기를 0으로 낮춘다.
            // 밑의 Dot은 내적이라는 Vector의 곱연산중 하나로, 아래에서는
            // transform.forward와 Vector.down(월드의 Down벡터)이 얼마나 같은가를 체크 한다.
            // 같은 1이 나오고 서로 정 반대이면 0이 나온다
            float height = Vector3.Dot(transform.forward, Vector3.down);
            
            // intensity는 Light의 세기를 의미
            sun.intensity = maxIntensity * Mathf.Clamp01(height);
        }
    }
}