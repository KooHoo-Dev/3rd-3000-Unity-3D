using System;
using UnityEngine;

namespace Study3D.ThreeDPhysics
{
    // BumperFeedback
    // : 무언가 부딪히면 충격량과 상대 속도를 이용해서 피드백(잠깐 부푸는 표현)을 표현
    // - OncollisionEnter : 닿는 순간 한 번 호출되고, Collision에 충돌 정보가 담겨 온다.
    // - 리지드 바디가 없는 벽이나 범퍼 등도 부딪혀 오는 쪽에 리지드 바디가 있으면 이벤트가 들어온다.
    
    public class BumperFeedback : MonoBehaviour
    {
        [Header("감지 설정")]
        public float minImpulse = 0.5f;
        private float currentImpulse;
        
        [Header("팽창 설정")]
        public float pulseScale = 1.15f;
        public float pulseDuration = 0.2f;

        private Vector3 startScale;
        private float pulseTimer;

        private void Awake()
        {
            startScale = transform.localScale;
        }

        private void Update()
        {
            // 펄스(팽창) 애니메이션 처리
            if (pulseTimer <= 0f) return;
            pulseTimer += Time.deltaTime;

            // pulseTimer의 값으로 전체 펄스 애니메이션 시간 중 얼마나 진행 되었는지 계산.
            float t = Mathf.Clamp01(pulseTimer / pulseDuration);
            // pulseScale이 1.15이니까 진행 시간(t)이 만약 0.5f(50% 정도 진행)라면 1.075가 반환 된다.
            // 결국 startScale * 1.075가 된다.
            
            transform.localScale = startScale * Mathf.Lerp(1f, pulseScale, t);
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Collision 클래스
            // : 두 리지드바디 또는 콜라이더가 물리적으로 충돌 했을 때,
            //   그 충돌 상황에 대한 상세 데이터를 담은 클래스
            // 아래는 자주 사용하는 필드들
            // - .gameObject : 충돌한 게임 오브젝트
            // - .collider : 직접 맞닿은 콜라이더 컴포넌트
            // - .impulse : 충격 정보. 벡터(방향, 크기)이며, 물리 엔진이 이 충돌을 밀어내기 위해
            //              수행한 연산 정보. (충격량)
            // - .relativeVelocity : 두 물체가 부딪힐 당시의 상대 속도.
            //                      Ex) (3)-> <-(2) 이렇게 부딪히면, 상대 속도는 (1)-> <-(0) 
            
            // 펄스(팽창) 트리거 처리

            // collision에 접근해서 충격(impulse)의 양(길이)를 가져 온다.
            float impulse = collision.impulse.magnitude;

            // 최소 충격량보다 작다면 종료
            if (impulse < minImpulse) return;

            // 충돌한 개체의 상대적 속도(방향과 크기)중 크기만 가져 온다.
            float speed = collision.relativeVelocity.magnitude;
            currentImpulse = Mathf.Max(currentImpulse, impulse); // 더 큰 쪽을 선택해서 반환.
            
            Debug.Log($"[Bumper : {gameObject.name}] 최대 충격량 : {currentImpulse}, 상대 속도 : {speed}");
            
            pulseTimer = pulseDuration;
        }
    }
}