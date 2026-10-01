using System;
using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Study3D.MiniGolf
{
    public class GolfOrbitCamera : MonoBehaviour
    {
        [Header("Settings")]
        public float degreesPerPixel = 0.2f; // 1픽셀당 0.2도씩 움직이도록 디폴트값 설정
        public float zoomStep = 0.5f;
        public float minRadius = 1f;
        public float maxRadius = 10f;
        
        private GolfPlainCamera plainCamera;

        private void Awake()
        {
            plainCamera = GetComponent<GolfPlainCamera>();
        }

        private void Update()
        {
            // 마우스 휠의 이번 프레임 이동량을 가져온다.
            float scroll = SimpleInput.GetMouseScrollDelta();
            float zoomValue = scroll * zoomStep;
            plainCamera.Radius -= Mathf.Clamp(zoomValue, minRadius, maxRadius);
            
            if(SimpleInput.GetMouseButton(SimpleInput.MouseButton.Right) == false) return;

            // 마우스의 이번 프레임 이동량을 가져 온다.
            Vector2 delta = SimpleInput.GetMouseDelta();
            plainCamera.Yaw += delta.x * degreesPerPixel;
            plainCamera.Pitch -= delta.y * degreesPerPixel;
        }
    }
}