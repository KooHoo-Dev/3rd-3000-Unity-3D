using UnityEngine;

namespace Study3D.FpsGame.Weapon
{
    // 도탄 소총(RicochetRifle)의 탄이 부딪힌 표면의 특성을 정의하는 컴포넌트입니다.
    // - 환경 레이어(CombatSystem.EnvironmentMask)에 속한 오브젝트에 붙여서 사용합니다.
    // - 이 컴포넌트가 없는 환경 오브젝트는 "기본 표면(콘크리트)"으로 취급합니다. → 반사 O, 보너스 0
    //
    // 예시 설정
    // - 금속     : canRicochet = true,  bonus = 0.2  (반사 + 데미지 보너스)
    // - 콘크리트 : 컴포넌트를 붙이지 않거나 canRicochet = true, bonus = 0
    // - 나무/흙  : canRicochet = false               (탄이 박히고 경로 종료)
    public class RicochetSurface : MonoBehaviour
    {
        [Header("도탄 표면")]
        [Tooltip("이 표면에서 탄이 튕겨 나갈 수 있는지 여부. false면 탄이 박히고 경로가 끝납니다")]
        [SerializeField] private bool canRicochet = true;

        [Tooltip("이 표면에서 반사될 때 데미지 배율에 추가로 더해지는 값 (예: 금속 0.2 = +20%)")]
        [SerializeField] private float bonus = 0.0f;

        // 외부(무기)에서는 읽기만 할 수 있도록 프로퍼티로 공개합니다.
        public bool CanRicochet => canRicochet;
        public float Bonus => bonus;
    }
}
