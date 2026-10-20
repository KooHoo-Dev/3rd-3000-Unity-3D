using UnityEngine;

namespace Study3D.FpsGame.Weapon
{
    // 샷건은 한 번 발사(탄환 1발)에 여러 개의 작은 탄환(펠릿)을 흩뿌리는 무기입니다.
    // - 한 탄창의 발사 횟수 : 부모(Weapon.Data)의 Total Ammo 값을 인스펙터에서 설정합니다. (예: 8)
    // - 작은 탄환의 총갯수 : pelletCount
    // - 탄 퍼짐 정도      : spreadAngle (조준선 기준 원뿔의 최대 각도)

    public class Shotgun : Weapon
    {
        [Header("산탄")]
        [SerializeField, Min(1)] private int pelletCount = 12;            // 탄환 1발을 구성하는 작은 탄환의 수
        [SerializeField, Range(0.0f, 45.0f)] private float spreadAngle = 5.0f; // 탄 퍼짐 각도(도)

        public int PelletCount => pelletCount;
        public float SpreadAngle => spreadAngle;

        private Camera mainCam;

        private void Start()
        {
            mainCam = Camera.main;
        }

        public override bool Fire()
        {
            // 발사 가능 여부 체크 + 탄환 차감은 부모가 처리합니다.
            // 펠릿이 몇 개든 탄창에서는 1발만 소모됩니다.
            if (IsAbleFire() == false) return false;

            // 어썰트 라이플과 동일하게 화면 중앙(Crosshair)에서 출발하는 Ray를 기준으로 삼습니다.
            Ray centerRay = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Transform camTransform = mainCam.transform;

            // 각도를 거리 1 기준의 원 반지름으로 바꿉니다. (tan(각도) = 반지름 / 거리)
            float spreadRadius = Mathf.Tan(spreadAngle * Mathf.Deg2Rad);

            for (int i = 0; i < pelletCount; i++)
            {
                // Random.insideUnitCircle
                // : 단위원(반지름 1)에서 랜덤한 점의 위치를 하나 추출합니다.
                // 이 점을 카메라의 오른쪽/위쪽 방향으로 펼쳐서 조준 방향에 더해주면
                // 원뿔 모양으로 퍼지는 방향을 얻을 수 있습니다.
                Vector2 circlePoint = Random.insideUnitCircle * spreadRadius;
                Vector3 direction = centerRay.direction
                                    + camTransform.right * circlePoint.x
                                    + camTransform.up * circlePoint.y;

                // 판정, CombatEvent 생성은 부모의 FireRay를 그대로 사용합니다.
                // → 맞은 펠릿마다 CombatSystem.OnCombatEvent가 발생하고
                //   HitEffectController가 ObjectPoolManager에서 피 이펙트/데미지 텍스트를 꺼내 재생합니다.
                FireRay(new Ray(centerRay.origin, direction.normalized));
            }

            PlayMuzzleFlash();

            return true;
        }
    }
}
