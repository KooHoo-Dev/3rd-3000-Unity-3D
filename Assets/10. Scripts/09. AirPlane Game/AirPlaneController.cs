using System;
using Study.Utilities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Study3D.AirPlaneGame
{
    // 종이 비행기의 입력 처리와 SkinnedMeshRenderer, 리깅, 회전 등을 이용해서
    // 비주얼적인 애니메이션 처리를 담당하는 클래스
    
    public class AirPlaneController : MonoBehaviour
    {
        public static AirPlaneController LocalAirPlane { get; private set; }

        private Rigidbody rBody;

        [Header("Rotation And Move Settings")]
        [SerializeField] private float pitchAngle = 1.0f;
        [SerializeField] private float yawAngle = 1.0f;

        [SerializeField] private float pitchSpeed = 1.0f;
        [SerializeField] private float yawSpeed = 1.0f;
        [SerializeField] private float moveSpeed = 1.0f;

        private Vector2 inputAxis;
        
        // yawSpeed가 있는 이유?
        // airplane이 보기에는 pitch, roll 회전으로 구현되어 보이지만
        // 실제 게임에 적용되는 회전 축은 pitch, yaw로 구현해야 자연스러워 진다.

        #region Unity Event Methods
        
        private void Awake()
        {
            rBody = GetComponent<Rigidbody>();

            // 약식 싱글톤
            if (LocalAirPlane == null) LocalAirPlane = this;
            else Destroy(gameObject);
        }

        private void Update()
        {
            UpdateInput();
            UpdateModeVisual();
        }

        private void FixedUpdate()
        {
            if(rBody.useGravity) return;
            
            UpdateRotation();
            UpdateVelocity();
        }
        
        #endregion

        #region Main Study

        private void UpdateInput()
        {
            inputAxis = SimpleInput.GetMoveAxisRaw();
        }

        // 비행기 자체의 회전을 계산해 준다. (pitch, roll)
        private void UpdateRotation()
        {
         
            // 아래처럼 Rotate를 사용해도 가능하다.
            /*Vector3 pAngle = new Vector3(inputAxis.y, 0, 0) * (pitchAngle * pitchSpeed * Time.deltaTime);
            Vector3 yAngle = new Vector3(0, inputAxis.x, 0) * (yawAngle * yawSpeed * Time.deltaTime);
            transform.Rotate(pAngle, Space.Self);
            transform.Rotate(yAngle, Space.Self);*/

            // pitch와 yaw 각각의 로테이션을 생성해 준다.
            float pAngle = inputAxis.y * pitchAngle * pitchSpeed * Time.deltaTime;
            float yAngle = inputAxis.x * yawAngle * yawSpeed * Time.deltaTime;

            Quaternion pitchRotation = Quaternion.AngleAxis(pAngle, transform.right);
            Quaternion yawRotation = Quaternion.AngleAxis(yAngle, transform.up);
            
            // 쿼터니언을 곱할 때 결과는 비슷하다
            transform.rotation = transform.rotation * pitchRotation * yawRotation;
        }

        private void UpdateVelocity()
        {
            
        }

        private void UpdateModeVisual()
        {
            
        }

        #endregion
    }
}