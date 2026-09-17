using System;
using UnityEngine;

namespace Study3D.Transforms
{
    public class RotationMethodsDemo : MonoBehaviour
    {
        public enum Method
        {
            EulerAngles, // 각도 누적
            Rotate, // 축을 이용한 회전
            QuaternionEuler // 각도를 축으로 변환
        }

        public Method method;
        public Vector3 axis;
        public float degreesPerSecond = 60f;

        private Vector3 sumEulerAngles;

        private void Update()
        {
            float step = degreesPerSecond * Time.deltaTime;

            switch (method)
            {
                case Method.EulerAngles:
                    // 일부러 문제가 드러나게 만든 방식
                    // X가 90도 같은 특정 임계점을 넘으면 다른 표기로 자동 전환 돼서 누적이 막힌다.
                    
                    Vector3 euler = transform.eulerAngles; // 현재 회전값을 가지고 와서
                    euler += axis * step; // 더해준다.
                    transform.eulerAngles = euler; // 넣어준다.
                    break;
                case Method.Rotate:
                    transform.Rotate(axis * step);
                    break;
                case Method.QuaternionEuler:
                    sumEulerAngles += axis * step; // 누적량을 가지고 온다.
                    transform.rotation = Quaternion.Euler(sumEulerAngles); // 대입해 준다.
                    break;
            }
        }
    }
}