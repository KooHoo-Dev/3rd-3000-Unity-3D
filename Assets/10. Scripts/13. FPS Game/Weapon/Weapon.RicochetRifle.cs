using UnityEngine;

namespace Study3D.FpsGame.Weapon
{
    // 도탄 소총은 벽에 맞은 탄이 튕겨 나가서(반사) 엄폐물 뒤의 적을 맞힐 수 있는 무기입니다.
    // - 한 번의 사격이 여러 개의 구간(Segment)으로 이루어진 경로로 계산됩니다.
    //   [화면 중앙] → (벽) → (벽) → [적]
    // - 튕긴 횟수가 많을수록 데미지가 커집니다.
    //   damage = Damage × (1 + bonusPerBounce × 반사횟수 + Σ 표면 보너스)
    //
    // 인스펙터 권장 값 (WeaponDesign.md 3-2)
    // - Weapon.Data : Fire Rate 0.35 / Damage 15 (직격 데미지) / Total Ammo 12 / Range 60
    // - 이 클래스   : bonusPerBounce 0.6 / maxBounces 3

    public class RicochetRifle : Weapon
    {
        [Header("도탄")]
        [Tooltip("반사 1회당 데미지 배율에 더해지는 값 (0.6 = +60%)")]
        [SerializeField, Min(0.0f)] private float bonusPerBounce = 0.6f;

        [Tooltip("최대 반사 횟수. 이 횟수만큼 튕긴 뒤에도 적을 못 맞히면 경로가 끝납니다")]
        [SerializeField, Min(0)] private int maxBounces = 3;

        [Tooltip("반사 지점에서 표면 법선 방향으로 띄워서 다음 Ray를 쏘는 거리 (같은 면에 다시 맞는 것을 방지)")]
        [SerializeField, Min(0.001f)] private float surfaceOffset = 0.01f;

        [Header("탄 경로 표시 (선택)")]
        [Tooltip("튕긴 경로 전체를 잠깐 보여줄 LineRenderer. 비워두면 표시하지 않습니다")]
        [SerializeField] private LineRenderer pathLine;
        [SerializeField, Min(0.0f)] private float pathLineDuration = 0.1f;

        public float BonusPerBounce => bonusPerBounce;
        public int MaxBounces => maxBounces;

        // hit.distance가 이 값보다 작으면 "Ray가 표면에 붙어서/안에서 출발했다"고 보고 경로를 끝냅니다.
        // (모서리 등에서 거리 0으로 계속 같은 곳에 맞는 무한 반사를 막기 위한 안전장치)
        private const float MinSegmentDistance = 0.001f;

        private Camera mainCam;

        // 경로의 꺾이는 점들을 담아둘 버퍼입니다.
        // 매 사격마다 new 하지 않도록 한 번 만들어두고 재사용합니다. (GC 방지)
        // 점의 최대 갯수 = 출발점 1 + 구간 끝점 (maxBounces + 1)
        private Vector3[] pathPoints;
        private int pathPointCount;
        private float currentPathLineTime;

        protected override void Awake()
        {
            base.Awake();

            pathPoints = new Vector3[maxBounces + 2];

            if (pathLine != null)
            {
                pathLine.useWorldSpace = true; // 경로 좌표는 월드 좌표입니다.
                pathLine.enabled = false;
            }
        }

        private void Start()
        {
            mainCam = Camera.main;
        }

        protected override void Update()
        {
            base.Update();

            // 경로 표시선이 켜져 있다면 시간이 지나면 꺼줍니다.
            // (무기 교체 시 GameObject가 꺼지면 코루틴이 멈추므로, 타이머 변수로 처리합니다)
            if (pathLine == null || pathLine.enabled == false) return;

            currentPathLineTime += Time.deltaTime;
            if (currentPathLineTime > pathLineDuration)
                pathLine.enabled = false;
        }

        private void OnDisable()
        {
            // 무기를 바꾸는 순간 경로 표시선이 남아있지 않도록 꺼줍니다.
            if (pathLine != null) pathLine.enabled = false;
        }

        public override bool Fire()
        {
            // 반자동 사격 : 발사 가능 여부(FireRate, 탄환) 체크 + 탄환 차감은 부모가 처리합니다.
            if (IsAbleFire() == false) return false;

            Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            FireRicochet(ray);
            PlayMuzzleFlash();

            return true;
        }

        // 하나의 탄이 벽에 튕기며 나아가는 경로를 구간(Segment) 단위로 계산합니다.
        private void FireRicochet(Ray ray)
        {
            int enemyMask = CombatSystem.Instance.EnemyMask;
            int mask = enemyMask | CombatSystem.Instance.EnvironmentMask;

            float remainRange = Range;  // 남은 사거리. 구간을 지날 때마다 줄어듭니다.
            float multiplier = 1.0f;    // 데미지 배율. 튕길 때마다 커집니다.

            // 인스펙터에서 maxBounces를 플레이 중에 늘렸을 때를 대비해 버퍼 크기를 확인합니다.
            if (pathPoints.Length < maxBounces + 2) pathPoints = new Vector3[maxBounces + 2];

            // 경로 표시선의 출발점은 총구(있다면)로, 실제 판정은 화면 중앙 Ray로 합니다.
            pathPointCount = 0;
            pathPoints[pathPointCount++] = muzzleFlash != null ? muzzleFlash.transform.position : ray.origin;

            // bounceCount : 지금까지 튕긴 횟수 (0이면 아직 한 번도 안 튕긴 직격 구간)
            for (int bounceCount = 0; bounceCount <= maxBounces; bounceCount++)
            {
                // 이번 구간에 아무것도 맞지 않았다면 남은 사거리 끝까지 날아가고 끝납니다.
                if (Physics.Raycast(ray, out RaycastHit hit, remainRange, mask) == false)
                {
                    Vector3 endPoint = ray.origin + ray.direction * remainRange;
                    Debug.DrawLine(ray.origin, endPoint, Color.gray, 3f);
                    pathPoints[pathPointCount++] = endPoint;
                    break;
                }

                Debug.DrawLine(ray.origin, hit.point, bounceCount == 0 ? Color.yellow : Color.cyan, 3f);
                pathPoints[pathPointCount++] = hit.point;

                // 1) 적에 맞았다면 : 데미지를 주고 경로를 끝냅니다.
                if (CombatSystem.Instance.IsEnemy(hit.collider))
                {
                    int damage = Mathf.RoundToInt(Damage * multiplier);
                    // 한 번이라도 튕겨서 맞혔다면 도탄 피해로 구분해 피드백(색상 등)을 다르게 보여줄 수 있습니다.
                    DamageType damageType = bounceCount > 0 ? DamageType.Ricochet : DamageType.Normal;
                    SendDamage(hit.collider, hit.point, damage, damageType);
                    break;
                }

                // 2) 환경(벽, 바닥)에 맞았다면 : 반사할지 결정합니다.
                // 표면에 붙은 채로 출발해서 거리 0으로 맞은 경우는 더 진행하지 않습니다. (무한 반사 방지)
                if (hit.distance < MinSegmentDistance) break;

                // 이미 최대 반사 횟수만큼 튕겼다면 더 튕기지 않고 끝납니다.
                if (bounceCount == maxBounces) break;

                // RicochetSurface가 없으면 기본 표면(반사 O, 보너스 0)으로 취급합니다.
                RicochetSurface surface = hit.collider.GetComponent<RicochetSurface>();
                if (surface != null && surface.CanRicochet == false) break; // 나무/흙 : 탄이 박히고 종료

                float surfaceBonus = surface != null ? surface.Bonus : 0.0f;
                multiplier += bonusPerBounce + surfaceBonus;

                // 이번 구간에서 날아간 거리만큼 사거리를 차감합니다.
                remainRange -= hit.distance;
                if (remainRange <= 0.0f) break;

                // Vector3.Reflect(입사 방향, 법선) : 거울에 비친 것처럼 반사된 방향을 구해줍니다.
                Vector3 reflected = Vector3.Reflect(ray.direction, hit.normal);
                // 맞은 면에서 바로 출발하면 같은 면에 다시 맞을 수 있으므로 법선 방향으로 살짝 띄웁니다.
                ray = new Ray(hit.point + hit.normal * surfaceOffset, reflected);
            }

            ShowPathLine();
        }

        // 계산한 경로 전체를 LineRenderer로 잠깐 보여줍니다. (LineRenderer가 없으면 아무것도 안 합니다)
        private void ShowPathLine()
        {
            if (pathLine == null) return;

            pathLine.positionCount = pathPointCount;
            for (int i = 0; i < pathPointCount; i++)
                pathLine.SetPosition(i, pathPoints[i]);

            pathLine.enabled = true;
            currentPathLineTime = 0.0f;
        }
    }
}
