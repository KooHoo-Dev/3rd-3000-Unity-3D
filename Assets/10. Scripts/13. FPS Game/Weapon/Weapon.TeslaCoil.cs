using System.Collections.Generic;
using UnityEngine;

namespace Study3D.FpsGame.Weapon
{
    // 테슬라 코일(전격총)은 버튼을 누르고 있는 동안 전기 빔을 계속 쏘는 무기입니다.
    // "한 명에게 쏘면, 모두가 맞는다." → 1차 대상에서 주변 적에게 전기가 연쇄로 튀어 나갑니다.
    //
    // 기존 무기와 다른 점
    // 1) 지속 사격 : PlayerController가 버튼을 누르고 있는 동안 매 프레임 Fire()를 호출합니다.
    //               열은 매 프레임 쌓이고, 데미지 판정(틱)은 FireRate 초마다 한 번씩만 합니다.
    // 2) 과열 자원 : 탄창 대신 '열(Heat)'을 관리합니다. 최대치에 닿으면 과열되어 잠시 사격할 수 없습니다.
    // 3) 연쇄 전이 : 맞은 적 주변의 가장 가까운 적에게 전기가 점프합니다. 점프할수록 데미지가 줄어듭니다.
    //
    // 인스펙터 권장값 (부모 Weapon.Data)
    // - Fire Rate : 0.1  → 틱 간격(tickInterval)으로 재활용합니다.
    // - Damage    : 6    → 1차 대상의 틱 데미지(tickDamage)로 재활용합니다.
    // - Total Ammo: 100  → 실제 탄환은 쓰지 않고, 남은 열 여유분을 UI에 보여주는 용도로만 씁니다.
    // - Range     : 30   → 1차 대상을 찾는 빔의 사거리입니다.
    //
    // TODO: Conductive 오브젝트(금속 드럼통, 철제 펜스) 중계 - 데미지 없이 감쇠 없이 연쇄를 이어주고 중계 반경 1.5배.
    //       (아직 해당 오브젝트/레이어가 없어서 구현하지 않습니다.)
    // TODO: 물 웅덩이(Wet 상태)의 적은 연쇄 횟수를 소모하지 않고 추가로 맞도록. (Wet 상태 시스템이 아직 없습니다.)
    // TODO: 지속적으로 맞는 적에게 감전(Shock) 상태 - 이동속도 -30%. (몬스터 AI가 아직 없습니다.)

    public class TeslaCoil : Weapon
    {
        [Header("연쇄")]
        [SerializeField, Min(0)] private int chainCount = 4;                    // 최대 점프 횟수 (1차 대상 제외)
        [SerializeField, Min(0.0f)] private float chainRadius = 6.0f;            // 한 번 점프할 수 있는 거리
        [SerializeField, Range(0.0f, 1.0f)] private float chainDecay = 0.75f;    // 점프마다 곱해지는 감쇠율
        [SerializeField, Min(0.0f)] private float beamRadius = 0.3f;             // 1차 대상을 찾는 SphereCast 반지름 (빔이라 조금 관대하게)

        [Header("과열")]
        [SerializeField, Min(1.0f)] private float maxHeat = 100.0f;
        [SerializeField, Min(0.0f)] private float heatPerSecond = 25.0f;         // 사격 중 초당 쌓이는 열
        [SerializeField, Min(0.0f)] private float coolPerSecond = 35.0f;         // 사격하지 않을 때 초당 식는 열
        [SerializeField, Min(0.0f)] private float overheatLockTime = 2.0f;       // 과열 시 강제로 사격이 잠기는 시간
        [SerializeField, Range(0.0f, 1.0f)] private float highHeatRatio = 0.8f;  // 이 비율 이상 열이 차면 데미지 보너스
        [SerializeField, Min(1.0f)] private float highHeatDamageBonus = 1.3f;    // 고열 데미지 배율

        [Header("연출 (선택)")]
        [Tooltip("연쇄 경로를 그릴 LineRenderer. 비워두어도 무기는 정상 동작합니다")]
        [SerializeField] private LineRenderer chainLine;

        public float Heat { get; private set; }
        public float MaxHeat => maxHeat;
        public bool IsOverheated { get; private set; }

        // 매 틱마다 new를 하면 GC(가비지)가 쌓이기 때문에 미리 만들어두고 재사용합니다.
        private readonly HashSet<Collider> visited = new HashSet<Collider>();   // 이번 틱에 이미 맞은 대상
        private readonly Collider[] overlapBuffer = new Collider[16];          // OverlapSphereNonAlloc 결과 버퍼
        private Vector3[] chainPoints;                                         // LineRenderer에 넘길 점들

        private Camera mainCam;
        private int lastFireFrame = -10;     // 마지막으로 Fire()가 호출된 프레임 번호
        private float overheatLockTimer;     // 과열 잠금 남은 시간

        protected override void Awake()
        {
            base.Awake();

            // 총구(1점) + 1차 대상(1점) + 연쇄 대상(chainCount점)
            chainPoints = new Vector3[chainCount + 2];
            HideChainLine();
            RefreshAmmoDisplay();
        }

        private void Start()
        {
            mainCam = Camera.main;
        }

        private void OnDisable()
        {
            // 무기 교체로 비활성화될 때 빔이 켜진 채로 남지 않게 꺼줍니다.
            HideChainLine();
        }

        public override bool Fire()
        {
            if (mainCam == null) return false;
            // 과열 중에는 사격할 수 없습니다. (잠금 시간이 끝나고 열이 0까지 식어야 해제됩니다)
            if (IsOverheated) return false;

            // "이번 프레임에 사격 중이었다"는 표시를 남깁니다. Update에서 냉각 여부를 판단할 때 씁니다.
            // 코루틴/플래그 대신 프레임 번호를 쓰면 호출 순서(Player Update ↔ Weapon Update)에 덜 민감합니다.
            lastFireFrame = Time.frameCount;

            // 누르고 있는 동안 매 프레임 열이 쌓입니다.
            Heat += heatPerSecond * Time.deltaTime;
            if (Heat >= maxHeat)
            {
                EnterOverheat();
                return false;
            }
            RefreshAmmoDisplay();

            // 데미지 판정은 FireRate(= tickInterval)마다 한 번만 합니다.
            // 부모의 IsAbleFire()는 탄환을 차감하므로 쓰지 않고, 시간 체크만 직접 합니다.
            if (CurrentFireRate < FireRate) return false;
            CurrentFireRate = 0.0f;

            FireChain();
            PlayMuzzleFlash();

            // 실제로 틱이 발생한 프레임에만 true → 발사 애니메이션 재생
            return true;
        }

        protected override void Update()
        {
            base.Update();

            // 지난 프레임이나 이번 프레임에 Fire()가 호출되었다면 '사격 중'으로 봅니다.
            // (PlayerController와 Weapon의 Update 실행 순서가 정해져 있지 않아서 1프레임 여유를 둡니다)
            bool isFiring = Time.frameCount - lastFireFrame <= 1;

            if (isFiring == false) HideChainLine();

            if (IsOverheated)
            {
                // 1단계: 강제 잠금 시간 동안은 열이 그대로 유지됩니다.
                if (overheatLockTimer > 0.0f)
                {
                    overheatLockTimer -= Time.deltaTime;
                    return;
                }

                // 2단계: 잠금이 끝나면 식기 시작하고, 0까지 내려가야 과열이 해제됩니다.
                Cool();
                if (Heat <= 0.0f) IsOverheated = false;
                return;
            }

            // 사격하지 않는 동안에는 자동으로 식습니다.
            if (isFiring == false) Cool();
        }

        // Reload는 '강제 배출(Vent)'로 재정의합니다.
        // 주의: LocalPlayer.ChangeWeapon()이 무기를 바꿀 때마다 Reload()를 호출합니다.
        // 그래서 설계서의 "배출 후 1초간 사격 불가"를 넣으면 무기를 꺼낼 때마다 1초씩 못 쏘게 됩니다.
        // → 결정: 배출은 열을 즉시 0으로 만들기만 하고, 추가 잠금은 걸지 않습니다.
        //   단, 과열 잠금 시간(overheatLockTimer)은 그대로 남겨서
        //   무기 교체(=배출)로 과열 패널티를 건너뛰지는 못하게 합니다.
        public override void Reload()
        {
            Heat = 0.0f;
            RefreshAmmoDisplay();
        }

        private void EnterOverheat()
        {
            Heat = maxHeat;
            IsOverheated = true;
            overheatLockTimer = overheatLockTime;
            HideChainLine();
            RefreshAmmoDisplay();
        }

        private void Cool()
        {
            if (Heat <= 0.0f) return;
            Heat = Mathf.Max(0.0f, Heat - coolPerSecond * Time.deltaTime);
            RefreshAmmoDisplay();
        }

        // 기존 UI가 CurrentAmmo를 보여주고 있으므로
        // "남은 열 여유분"을 TotalAmmo 기준으로 환산해서 넣어줍니다. (열 0 = 가득, 열 최대 = 0)
        private void RefreshAmmoDisplay()
        {
            float remainRatio = 1.0f - Mathf.Clamp01(Heat / maxHeat);
            CurrentAmmo = Mathf.RoundToInt(TotalAmmo * remainRatio);
        }

        // 1차 대상을 찾고, 주변 적에게 연쇄로 데미지를 전달합니다.
        private void FireChain()
        {
            int enemyMask = CombatSystem.Instance.EnemyMask;
            int environmentMask = CombatSystem.Instance.EnvironmentMask;

            // 고열(80% 이상)일 때는 데미지 보너스 → 한계까지 끌고 가는 리스크/리턴
            float heatBonus = Heat >= maxHeat * highHeatRatio ? highHeatDamageBonus : 1.0f;

            Ray centerRay = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            int pointCount = 0;
            chainPoints[pointCount++] = muzzleFlash.transform.position; // 빔의 시작점은 총구

            // 벽까지의 거리를 먼저 구해 둡니다. 벽 뒤의 적은 맞지 않아야 하니까요.
            float maxDistance = Range;
            if (Physics.Raycast(centerRay, out RaycastHit wallHit, Range, environmentMask))
                maxDistance = wallHit.distance;

            // 1. 화면 중앙에서 SphereCast로 1차 대상을 찾습니다.
            if (Physics.SphereCast(centerRay, beamRadius, out RaycastHit hit, maxDistance, enemyMask) == false)
            {
                // 아무도 맞지 않았다면 빔만 허공(또는 벽)까지 그려줍니다.
                chainPoints[pointCount++] = centerRay.GetPoint(maxDistance);
                DrawChainLine(pointCount);
                return;
            }

            visited.Clear();
            Collider current = hit.collider;
            visited.Add(current);

            // SphereCast는 시작 위치에서 이미 겹쳐 있으면 hit.point가 (0,0,0)으로 나옵니다.
            // 그럴 때는 대상 콜라이더의 중심을 맞은 위치로 사용합니다.
            Vector3 firstPoint = hit.distance > 0.0f ? hit.point : current.bounds.center;
            SendDamage(current, firstPoint, CalcDamage(0, heatBonus), DamageType.Electric);
            chainPoints[pointCount++] = firstPoint;

            // 2. 마지막으로 맞은 대상 주변에서 다음 대상을 찾아 점프합니다.
            for (int jump = 1; jump <= chainCount; jump++)
            {
                Vector3 from = current.bounds.center;
                Collider next = FindNextTarget(from, enemyMask, environmentMask);
                if (next == null) break; // 더 튈 곳이 없으면 연쇄 종료

                visited.Add(next);
                Vector3 nextPoint = next.bounds.center;
                SendDamage(next, nextPoint, CalcDamage(jump, heatBonus), DamageType.Electric);
                chainPoints[pointCount++] = nextPoint;
                current = next;
            }

            DrawChainLine(pointCount);
        }

        // from 주변 chainRadius 안에서 아직 맞지 않았고, 벽에 가로막히지 않은 가장 가까운 적을 찾습니다.
        private Collider FindNextTarget(Vector3 from, int enemyMask, int environmentMask)
        {
            // NonAlloc 버전은 결과를 미리 만든 배열에 채워주고 개수만 돌려줍니다. (매 틱 배열 생성 X)
            int count = Physics.OverlapSphereNonAlloc(from, chainRadius, overlapBuffer, enemyMask);

            Collider nearest = null;
            float nearestSqrDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider candidate = overlapBuffer[i];
                if (visited.Contains(candidate)) continue; // 이미 맞은 대상은 제외

                Vector3 to = candidate.bounds.center;
                // 거리 비교만 할 때는 제곱 거리(sqrMagnitude)를 쓰면 제곱근 계산을 아낄 수 있습니다.
                float sqrDistance = (to - from).sqrMagnitude;
                if (sqrDistance >= nearestSqrDistance) continue;

                // 점프 경로 사이에 벽이 있으면 그 대상은 건너뜁니다.
                if (Physics.Linecast(from, to, environmentMask)) continue;

                nearest = candidate;
                nearestSqrDistance = sqrDistance;
            }

            return nearest;
        }

        // damage(n) = Damage × chainDecay^n × heatBonus
        private int CalcDamage(int jump, float heatBonus)
        {
            float damage = Damage * Mathf.Pow(chainDecay, jump) * heatBonus;
            return Mathf.Max(1, Mathf.RoundToInt(damage));
        }

        private void DrawChainLine(int pointCount)
        {
            if (chainLine == null) return;

            chainLine.enabled = true;
            chainLine.positionCount = pointCount;
            for (int i = 0; i < pointCount; i++)
                chainLine.SetPosition(i, chainPoints[i]);
        }

        private void HideChainLine()
        {
            if (chainLine == null) return;
            chainLine.enabled = false;
        }
    }
}
