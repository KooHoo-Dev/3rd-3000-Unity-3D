using UnityEngine;

namespace Study3D.FpsGame.Weapon
{
    public class Shotgun : Weapon
    {
        [Header("산탄")]
        [SerializeField] private Transform fireTransform;
        [SerializeField] private float distance = 0.5f;
        [SerializeField] private float radius = 0.5f;
        [SerializeField] private int bulletCount = 12;
        
        public override bool Fire()
        {
            if(IsAbleFire() == false) return false;
            // Fire함수가 호출 되었을때 12번의 Ray를 발사하는 로직만 만들면
            // 샷건 완성입니다.
            
            Vector3 firePoint = fireTransform.position;

            for (int i = 0; i < bulletCount; i++)
            {
                // Random.insideUnitCircle
                // : 단위원(반지름 1)에서 랜덤한 점의 위치를 하나 추출합니다.
                Vector2 circlePoint = Random.insideUnitCircle * radius;
                Vector3 spreadPoint = fireTransform.TransformPoint(
                    new Vector3(circlePoint.x, circlePoint.y, distance)
                );
                Vector3 direction = (spreadPoint - firePoint).normalized;
                FireRay(new Ray(firePoint, direction));
            }

            PlayMuzzleFlash();
            
            return true;
        }
    }
}