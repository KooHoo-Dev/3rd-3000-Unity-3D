using System.Collections.Generic;
using UnityEngine;

namespace Study3D.FpsGame.Weapon
{
    // 레일건은 "누르고 있다가(차지) 떼면 발사"하는 저격형 무기입니다.
    // 기존 총기(AssaultRifle, Shotgun)와 다른 점 세 가지
    // 1. 핵심 로직 : 누름 → 유지(차지) → 뗌(발사)의 3단계 입력. 차지하는 동안에는 탄을 소모하지 않습니다.
    // 2. 상호작용  : 경로상의 적을 모두 '관통'합니다. 풀차지면 벽도 1장 뚫습니다.
    // 3. 데미지    : 차지량에 비례하고, 적을 하나 관통할 때마다 감쇠합니다.
    //
    // 입력 흐름 (PlayerController 기준)
    // - 왼쪽 버튼을 누르고 있는 동안 매 프레임 Fire()가 호출됩니다. → 여기서 차지를 쌓습니다.
    // - 버튼을 떼는 순간 ReleaseFire()가 호출됩니다.              → 여기서 실제로 발사합니다.
    // - Fire()/ReleaseFire()가 true를 돌려주면 발사 애니메이션이 재생되므로
    //   "실제로 탄이 나간 순간"에만 true를 돌려줍니다.

    public class Railgun : Weapon
    {
        [Header("차지")]
        [SerializeField, Min(0.01f)] private float chargeTime = 1.2f;        // 풀차지(1.0)까지 걸리는 시간
        [SerializeField] private int minDamage = 20;                          // 차지량 0일 때의 데미지
        [SerializeField] private int maxDamage = 150;                         // 풀차지일 때의 데미지
        [SerializeField, Range(0.0f, 1.0f)] private float minChargeToFire = 0.2f; // 이보다 적게 모으고 떼면 불발(탄 소모 없음)
        [SerializeField, Min(0.0f)] private float overchargeHoldTime = 2.0f;  // 풀차지 후 이만큼 더 들고 있으면 강제 발사(과충전)

        [Header("관통")]
        [SerializeField, Range(0.0f, 1.0f)] private float penetrationFalloff = 0.7f; // 적 1명 관통마다 곱해지는 감쇠율
        [SerializeField, Min(1)] private int maxPenetration = 5;                     // 한 발로 맞힐 수 있는 최대 적 수

        [Header("궤적 (선택)")]
        [Tooltip("비워두면 궤적을 그리지 않습니다")]
        [SerializeField] private LineRenderer trailLine;
        [Tooltip("궤적의 시작점. 비워두면 카메라(화면 중앙)에서 시작합니다")]
        [SerializeField] private Transform trailStartPoint;
        [SerializeField, Min(0.01f)] private float trailDuration = 0.3f;      // 궤적이 남아있는 시간

        // UI(차지 게이지, 크로스헤어 수축 등)에서 읽어갈 수 있도록 읽기 전용으로 열어둡니다.
        public float ChargeAmount => chargeAmount;   // 0 ~ 1
        public bool IsCharging => isCharging;

        // RaycastAll은 호출할 때마다 새 배열을 만듭니다(GC 발생).
        // 미리 만들어둔 배열을 재사용하는 RaycastNonAlloc을 사용합니다.
        private const int MaxRayHits = 32;
        private readonly RaycastHit[] rayHits = new RaycastHit[MaxRayHits];

        // 거리순 정렬에 사용할 비교자. 람다를 매번 만들지 않도록 한 번만 만들어 둡니다.
        private static readonly IComparer<RaycastHit> DistanceComparer =
            Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));

        // 같은 적 콜라이더를 한 발에 두 번 맞히지 않도록 기록합니다. (매 발사마다 비웁니다)
        private readonly HashSet<Collider> damagedColliders = new HashSet<Collider>();

        private Camera mainCam;

        private bool isCharging;          // 차지 중인가?
        private float chargeAmount;       // 현재 차지량 0 ~ 1
        private float overchargeTimer;    // 풀차지 상태로 버틴 시간
        private bool waitForRelease;      // 과충전으로 강제 발사된 뒤, 버튼을 뗄 때까지 다시 차지하지 않도록 막습니다.
        private float trailTimer;         // 궤적이 켜진 뒤 흐른 시간

        protected override void Awake()
        {
            base.Awake(); // 부모의 초기화(총구 화염 끄기, 탄창 채우기)를 꼭 먼저 호출합니다.

            if (trailLine != null)
            {
                trailLine.useWorldSpace = true; // 발사 지점/도착 지점을 월드 좌표로 찍습니다.
                trailLine.positionCount = 2;
                trailLine.enabled = false;
            }
        }

        private void Start()
        {
            mainCam = Camera.main;
        }

        protected override void Update()
        {
            base.Update(); // FireRate 누적, 총구 화염 시간 처리는 부모가 합니다.

            // 코루틴 대신 타이머 변수로 궤적을 끕니다.
            // (무기를 바꾸면 GameObject가 꺼지면서 코루틴이 중간에 멈춰버리기 때문입니다)
            if (trailLine == null || trailLine.enabled == false) return;
            trailTimer += Time.deltaTime;
            if (trailTimer >= trailDuration) trailLine.enabled = false;
        }

        // 차지 도중 무기를 바꾸면 GameObject가 비활성화되면서 여기로 들어옵니다.
        // 다시 꺼냈을 때 차지가 남아있지 않도록 상태를 초기화합니다.
        private void OnDisable()
        {
            ResetCharge();
            waitForRelease = false;
            if (trailLine != null) trailLine.enabled = false;
        }

        // 버튼을 누르고 있는 동안 매 프레임 호출됩니다.
        public override bool Fire()
        {
            // 과충전으로 이미 쏘았다면, 버튼을 한 번 뗄 때까지는 아무것도 하지 않습니다.
            if (waitForRelease) return false;

            if (isCharging == false)
            {
                // 차지 시작 조건 : 탄이 있고, 발사 간격(FireRate)이 지났을 때
                // 주의 : 여기서는 IsAbleFire()를 부르지 않습니다. (IsAbleFire는 탄을 소모하기 때문)
                if (CurrentAmmo <= 0 || CurrentFireRate < FireRate) return false;

                isCharging = true;
                chargeAmount = 0.0f;
                overchargeTimer = 0.0f;
            }

            // 차지량을 0 → 1로 채웁니다. chargeTime초가 지나면 1.0(풀차지)이 됩니다.
            chargeAmount = Mathf.Min(1.0f, chargeAmount + Time.deltaTime / chargeTime);

            // 아직 풀차지가 아니라면 계속 모으는 중입니다. 발사하지 않았으니 false.
            if (chargeAmount < 1.0f) return false;

            // 풀차지 상태로 계속 들고 있으면 과충전 시간을 잽니다.
            overchargeTimer += Time.deltaTime;
            if (overchargeTimer < overchargeHoldTime) return false;

            // 과충전! 강제로 발사합니다. 버튼을 뗄 때까지 다음 차지는 막아둡니다.
            waitForRelease = true;
            return Discharge();
        }

        // 버튼을 뗀 순간 호출됩니다.
        public override bool ReleaseFire()
        {
            // 버튼을 뗐으니 다음에 누르면 다시 차지할 수 있습니다.
            waitForRelease = false;

            // 차지 중이 아니었다면(탄이 없었거나, 이미 과충전으로 쏜 경우) 할 일이 없습니다.
            if (isCharging == false) return false;

            return Discharge();
        }

        // 모아둔 차지량으로 실제 발사를 시도합니다. 발사했으면 true.
        private bool Discharge()
        {
            float charge = chargeAmount;
            ResetCharge(); // 쏘든 불발이든 차지 상태는 비웁니다.

            // 너무 짧게 누르고 뗀 경우는 불발 처리합니다. (실수 클릭 보호, 탄 소모 없음)
            if (charge < minChargeToFire) return false;

            // 이제서야 탄을 소모합니다. (FireRate 초기화도 IsAbleFire가 처리)
            if (IsAbleFire() == false) return false;

            Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Vector3 endPoint = FirePiercingRay(ray, charge);
            ShowTrail(ray.origin, endPoint);
            PlayMuzzleFlash();

            return true;
        }

        private void ResetCharge()
        {
            isCharging = false;
            chargeAmount = 0.0f;
            overchargeTimer = 0.0f;
        }

        // 관통 판정을 진행하고, 탄이 멈춘 지점(궤적의 끝점)을 돌려줍니다.
        private Vector3 FirePiercingRay(Ray ray, float charge)
        {
            // 적과 환경(벽, 바닥)을 함께 검사해야 "벽에 막혔는지"를 알 수 있습니다.
            int mask = CombatSystem.Instance.EnemyMask | CombatSystem.Instance.EnvironmentMask;

            // 경로상의 모든 콜라이더를 얻습니다.
            // 주의 : RaycastAll / RaycastNonAlloc의 결과는 거리순이 아닙니다! 직접 정렬해야 합니다.
            int hitCount = Physics.RaycastNonAlloc(ray, rayHits, Range, mask);
            System.Array.Sort(rayHits, 0, hitCount, DistanceComparer);

            // 데미지 공식 : 차지량을 제곱해서 Lerp → 풀차지의 가치를 더 높여줍니다.
            // 예) 차지 0.5 → 0.25 지점의 데미지, 차지 1.0 → 최대 데미지
            float damage = Mathf.Lerp(minDamage, maxDamage, charge * charge);
            int pierced = 0;

            // 풀차지일 때만 벽을 1장 뚫을 수 있습니다.
            bool canBreakWall = charge >= 1.0f;
            Collider brokenWall = null; // 이미 뚫고 지나간 벽

            // 아무것에도 막히지 않으면 사거리 끝까지 궤적을 그립니다.
            Vector3 endPoint = ray.GetPoint(Range);

            damagedColliders.Clear();

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = rayHits[i];
                Collider hitCollider = hit.collider;

                if (CombatSystem.Instance.IsEnemy(hitCollider) == false)
                {
                    // 환경(벽)에 닿았습니다.
                    // Raycast는 콜라이더에 '들어가는 면'만 보고하므로, 벽 1장은 보통 결과 1개입니다.
                    // 혹시 같은 벽이 또 나오더라도 서로 다른 콜라이더 1개를 벽 1장으로 셉니다.
                    if (hitCollider == brokenWall) continue;

                    if (canBreakWall && brokenWall == null)
                    {
                        brokenWall = hitCollider; // 벽 1장 관통! 다음 벽부터는 막힙니다.
                        continue;
                    }

                    // 더 이상 뚫을 수 없는 벽 → 탄이 여기서 멈춥니다.
                    endPoint = hit.point;
                    break;
                }

                // 같은 적 콜라이더를 두 번 맞히지 않습니다. (Add가 false = 이미 들어있음)
                if (damagedColliders.Add(hitCollider) == false) continue;

                // n번째로 맞은 적의 데미지 = baseDamage × falloff^n
                // 매번 falloff를 곱해 나가면 거듭제곱과 같은 결과가 됩니다.
                SendDamage(hitCollider, hit.point, Mathf.RoundToInt(damage), DamageType.Pierce);
                damage *= penetrationFalloff;

                // 최대 관통 수에 도달하면 탄이 이 적에서 멈춥니다.
                if (++pierced >= maxPenetration)
                {
                    endPoint = hit.point;
                    break;
                }
            }

            Debug.DrawLine(ray.origin, endPoint, Color.cyan, 1.0f);
            return endPoint;
        }

        // 관통 경로를 LineRenderer로 잠깐 보여줍니다. (연결되지 않았다면 아무것도 하지 않습니다)
        private void ShowTrail(Vector3 rayOrigin, Vector3 endPoint)
        {
            if (trailLine == null) return;

            Vector3 startPoint = trailStartPoint != null ? trailStartPoint.position : rayOrigin;
            trailLine.SetPosition(0, startPoint);
            trailLine.SetPosition(1, endPoint);
            trailLine.enabled = true;
            trailTimer = 0.0f;
        }
    }
}
