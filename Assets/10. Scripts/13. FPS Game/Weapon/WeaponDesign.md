# 추가 총기 기획서

> 기준 코드: `Weapon.cs`, `Weapon.Data.cs`, `Weapon.AssaultRifle.cs`, `Weapon.Shotgun.cs`
> 작성일: 2026-10-20

---

## 0. 기존 총기 분석

새 총기가 "기존과 다르다"는 걸 판단하려면 먼저 기준선을 정해야 합니다. 현재 구현된 두 총기는 다음 세 축에서 사실상 **같은 구조**를 공유합니다.

| 축 | Assault Rifle | Shotgun | 공통점 |
|---|---|---|---|
| **핵심 로직** | 클릭 1회 → `IsAbleFire()` → 화면 중앙 Ray 1개 | 클릭 1회 → `IsAbleFire()` → 원뿔 안 랜덤 Ray N개 | 단발 입력, 탄창(Ammo) 소비, 즉시 판정 |
| **탄환과 오브젝트의 상호작용** | `Physics.Raycast`로 첫 번째 적 1명만 맞힘 | 펠릿마다 첫 번째 적 1명만 맞힘 | 히트스캔(Hitscan), 관통/반사/물리력 없음, 적 레이어만 검사 |
| **데미지 방식** | 맞으면 고정 10 | 펠릿마다 고정 10 (맞은 수만큼 누적) | 즉발·고정 데미지, `CombatEvent` 1건 = 피격 1회 |

그래서 새 총기는 아래 세 가지를 **모두** 기존과 다르게 가져갑니다.

1. **핵심 로직** – 단발 클릭 + 탄창 이외의 입력/자원 구조 (차지, 지속 사격, 과열, 연료, 2단 입력 등)
2. **오브젝트 상호작용** – "첫 번째 적에 닿으면 끝"이 아닌 동작 (관통, 반사, 연쇄, 범위, 부착, 물리력)
3. **데미지 방식** – "즉시 고정 10"이 아닌 공식 (차지 비례, 반사 보너스, 연쇄 감쇠, 지속 피해, 거리 감쇠, 충돌 속도 비례)

> GrenadeLauncher는 현재 스텁 상태이며 "착탄 즉시 폭발하는 투사체"로 구현될 예정으로 보고, 아래 기획은 그것과도 겹치지 않도록 설계했습니다.

---

## 1. 한눈에 보기

| # | 이름 | 핵심 로직 | 오브젝트 상호작용 | 데미지 방식 | 난이도 |
|---|---|---|---|---|---|
| 1 | **Railgun** (레일건) | 누르고 있다가 떼면 발사 (차지) | 적을 **관통**, 풀차지 시 얇은 벽도 관통 | 차지량 비례 × 관통할수록 감쇠 | ★★ |
| 2 | **Ricochet Rifle** (도탄 소총) | 단발, 탄이 벽에서 **반사** 경로 계산 | 환경 표면에 **반사**(최대 3회) | 반사 횟수만큼 데미지 **증가** | ★★ |
| 3 | **Tesla Coil** (전격총) | 누르는 동안 지속 사격, **과열 게이지** (탄창 없음) | 맞은 적에서 주변 적으로 **연쇄 전이**, 금속 소품이 중계 | 점프할 때마다 **감쇠** | ★★★ |
| 4 | **Flamethrower** (화염방사기) | 누르는 동안 분사, **연료** 소모, Ray 미사용 | 원뿔 **범위** 판정, 가연성 오브젝트 **점화·확산** | 즉발 약함 + **화상 중첩 DoT** | ★★★ |
| 5 | **Sticky Charge Launcher** (점착 폭탄 발사기) | 발사(좌클릭)와 **기폭(우클릭)** 분리, 최대 4개 설치 | 물리 투사체가 벽·적에 **부착** | 폭심 **거리 감쇠** 범위 피해 + 부착 대상 보너스 | ★★★ |
| 6 | **Harpoon Gun** (작살총) | 1발 장전식, 발사 후 **회수(우클릭)** | 적을 **꿰뚫어 끌고 가서 벽에 고정** | 관통 피해 + **충돌 속도 비례** 피해 | ★★★★ |

---

## 2. 공통 구조 확장 제안

새 총기를 넣기 전에 `Weapon` 베이스에 손봐야 할 부분입니다. (현재 구조로는 차지/지속 사격/커스텀 데미지가 불가능)

### 2-1. 입력 확장 – `Fire()` 하나로는 부족

현재 `WeaponTester`는 `GetMouseButtonDown`에서만 `Fire()`를 부릅니다. 차지·지속 사격·기폭을 위해 입력 단계를 나눕니다.

```csharp
public abstract partial class Weapon
{
    public abstract bool Fire();                // 기존: 눌린 순간

    public virtual void OnFireHeld() { }        // 누르고 있는 동안 매 프레임 (Tesla, Flamethrower, Railgun 차지)
    public virtual void OnFireReleased() { }    // 뗀 순간 (Railgun 발사)
    public virtual void AltFire() { }           // 우클릭 (Sticky 기폭, Harpoon 회수)
}
```

```csharp
// WeaponTester.Update()
if (SimpleInput.GetMouseButtonDown(SimpleInput.MouseButton.Left)) weapon.Fire();
if (SimpleInput.GetMouseButton(SimpleInput.MouseButton.Left))     weapon.OnFireHeld();
if (SimpleInput.GetMouseButtonUp(SimpleInput.MouseButton.Left))   weapon.OnFireReleased();
if (SimpleInput.GetMouseButtonDown(SimpleInput.MouseButton.Right)) weapon.AltFire();
```

### 2-2. 데미지 이벤트 생성 함수 분리

현재 `FireRay`는 `Damage = 10`, `Receiver = LocalPlayer`가 하드코딩되어 있어 데미지 공식을 바꿀 수 없습니다. 이벤트 생성만 공통 함수로 뽑아냅니다.

```csharp
// 판정 방식(Ray, Overlap, 충돌)과 상관없이 "누가 얼마나 어디서 맞았는지"만 넘기면 되도록 합니다.
protected void SendDamage(Collider target, Vector3 hitPoint, int damage, DamageType type = DamageType.Normal)
{
    IDamageable receiver = CombatSystem.Instance.GetMonsterOrNull(target);
    if (receiver == null) return;

    CombatEvent combatEvent = new CombatEvent();
    combatEvent.Sender = Player.Player.LocalPlayer;
    combatEvent.Receiver = receiver;
    combatEvent.Damage = damage;
    combatEvent.HitPosition = hitPoint;
    combatEvent.Collider = target;
    combatEvent.DamageType = type;

    CombatSystem.Instance.AddInGameEvent(combatEvent);
}
```

### 2-3. `CombatEvent`에 데미지 타입 추가

`HitEffectController`가 타입별로 다른 피드백(전기 스파크, 불꽃, 데미지 텍스트 색상)을 재생할 수 있게 합니다.

```csharp
public enum DamageType { Normal, Pierce, Ricochet, Electric, Fire, Explosion, Impact }

public class CombatEvent : InGameEvent
{
    public int Damage { get; set; }
    public DamageType DamageType { get; set; }   // 추가
    ...
}
```

### 2-4. 레이어 추가

반사·차폐 판정을 위해 `CombatSystem`에 `EnvironmentMask`(벽/바닥)를 추가합니다. 현재는 `EnemyMask`만 있어서 Ray가 벽을 그냥 통과합니다.

### 2-5. `WeaponType` / `WeaponController` 확장

```csharp
public enum WeaponType
{
    Unknown, AssaultRifle, GrenadeLauncher, Shotgun,
    Railgun, RicochetRifle, TeslaCoil, Flamethrower, StickyLauncher, HarpoonGun,
}
```

`WeaponController`의 개별 필드 + switch는 총이 10종이 되면 유지가 어렵습니다. `[SerializeField] Weapon[] weapons` 배열 + 각 Weapon이 자기 `WeaponType`을 들고 있는 구조로 `Dictionary<WeaponType, Weapon>`을 만드는 방식을 권장합니다.

---

## 3. 총기 상세 기획

### 3-1. Railgun (레일건)

> "한 발에 모든 걸 건다. 일렬로 선 적은 한 번에 꿰뚫는다."

#### 컨셉
에너지를 모았다가 한 번에 방출하는 저격형 무기. 연사력 대신 **타이밍**과 **적 배치를 읽는 능력**을 요구합니다.

#### 수치
| 항목 | 값 | 비고 |
|---|---|---|
| TotalAmmo | 5 | 차지 1회 = 1발 |
| FireRate | 0.8s | 발사 후 다음 차지 시작까지 |
| chargeTime | 1.2s | 풀차지 도달 시간 |
| minDamage / maxDamage | 20 / 150 | |
| penetrationFalloff | 0.7 | 적 1명 관통마다 ×0.7 |
| maxPenetration | 5 | |
| Range | 300 | |

#### 핵심 로직 (기존과 다른 점: **누름 → 유지 → 뗌**의 3단계 입력)
1. `Fire()` (눌림) : 발사 가능 여부만 확인하고 차지 시작. **이 시점에는 탄을 소모하지 않습니다.**
2. `OnFireHeld()` : `chargeAmount`를 0→1로 증가. 총구 이펙트 크기/사운드 피치를 차지량에 연동.
3. `OnFireReleased()` : 탄 소모 후 발사. 차지량 0.2 미만이면 불발(탄 소모 없음) 처리해서 실수 클릭을 보호.
4. 풀차지 후 2초 이상 유지하면 **과충전**으로 강제 발사 → 계속 들고 있는 플레이를 막음.

#### 오브젝트 상호작용 (기존과 다른 점: **관통**)
- `Physics.RaycastAll`로 경로상 모든 콜라이더를 얻고 **거리순 정렬**.
- 적이면 데미지를 주고 계속 진행, **환경(벽)에 닿으면 정지**.
- 단, **풀차지(1.0)일 때는 벽 1장까지 관통**. 벽 너머 적을 저격하는 플레이 유도.
- 궤적은 `LineRenderer`로 0.3초간 남겨서 관통 경로를 시각화.

#### 데미지 방식 (기존과 다른 점: **차지 비례 + 관통 감쇠**)
```
baseDamage = Lerp(minDamage, maxDamage, chargeAmount²)   // 제곱 → 풀차지 가치를 높임
damage(n)  = baseDamage × penetrationFalloff^n           // n = 앞서 관통한 적 수
```
예) 풀차지로 3명 관통 → 150 → 105 → 73

#### 클래스 스케치
```csharp
public class Railgun : Weapon
{
    [Header("차지")]
    [SerializeField] private float chargeTime = 1.2f;
    [SerializeField] private int minDamage = 20;
    [SerializeField] private int maxDamage = 150;
    [SerializeField, Range(0f, 1f)] private float minChargeToFire = 0.2f;

    [Header("관통")]
    [SerializeField, Range(0f, 1f)] private float penetrationFalloff = 0.7f;
    [SerializeField] private int maxPenetration = 5;

    private Camera mainCam;
    private bool isCharging;
    private float chargeAmount;   // 0 ~ 1

    private void Start() => mainCam = Camera.main;

    public override bool Fire()
    {
        if (CurrentAmmo <= 0 || CurrentFireRate < FireRate) return false;
        isCharging = true;
        chargeAmount = 0f;
        return true;
    }

    public override void OnFireHeld()
    {
        if (isCharging == false) return;
        chargeAmount = Mathf.Min(1f, chargeAmount + Time.deltaTime / chargeTime);
    }

    public override void OnFireReleased()
    {
        if (isCharging == false) return;
        isCharging = false;
        if (chargeAmount < minChargeToFire) return;   // 불발, 탄 소모 없음

        if (IsAbleFire() == false) return;
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        FirePiercingRay(ray, chargeAmount);
        PlayMuzzleFlash();
    }

    private void FirePiercingRay(Ray ray, float charge)
    {
        int mask = CombatSystem.Instance.EnemyMask | CombatSystem.Instance.EnvironmentMask;
        RaycastHit[] hits = Physics.RaycastAll(ray, Range, mask);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        float damage = Mathf.Lerp(minDamage, maxDamage, charge * charge);
        int pierced = 0;
        bool canBreakWall = charge >= 1f;

        foreach (RaycastHit hit in hits)
        {
            bool isEnemy = (CombatSystem.Instance.EnemyMask & (1 << hit.collider.gameObject.layer)) != 0;
            if (isEnemy == false)
            {
                if (canBreakWall == false) break;
                canBreakWall = false;      // 벽은 1장만
                continue;
            }

            SendDamage(hit.collider, hit.point, Mathf.RoundToInt(damage), DamageType.Pierce);
            damage *= penetrationFalloff;
            if (++pierced >= maxPenetration) break;
        }
    }
}
```

#### 밸런스 / 카운터 플레이
- 차지 중 이동속도 -40% → 노출 시간 증가가 리스크.
- 차지 중에는 크로스헤어가 수축하는 UI로 정확도를 보상.
- 흩어진 적에게는 효율이 낮으므로 Shotgun/Flamethrower와 역할이 겹치지 않음.

---

### 3-2. Ricochet Rifle (도탄 소총)

> "벽을 맞혀라. 튕길수록 강해진다."

#### 컨셉
엄폐물 뒤에 숨은 적을 **각도 계산**으로 잡는 기교형 무기. 직접 맞히면 약하고, 반사시켜 맞히면 강합니다.

#### 수치
| 항목 | 값 | 비고 |
|---|---|---|
| TotalAmmo | 12 | |
| FireRate | 0.35s | 반자동 |
| baseDamage | 15 | 직격 시 |
| bonusPerBounce | 0.6 | 반사 1회당 +60% |
| maxBounces | 3 | |
| rangePerSegment | 60 | 반사마다 남은 거리 차감 |

#### 핵심 로직 (기존과 다른 점: **한 번의 사격이 여러 구간(Segment)의 경로로 계산됨**)
1. 화면 중앙 Ray로 시작.
2. 환경 또는 적을 대상으로 `Raycast`.
3. **환경에 맞으면** `Vector3.Reflect(direction, hit.normal)`로 새 방향을 구해 `hit.point`에서 다시 Ray. 남은 사거리는 소모한 거리만큼 줄어듭니다.
4. **적에 맞으면** 데미지 처리 후 종료.
5. `maxBounces` 초과 또는 사거리 소진 시 종료.
6. 조준 중(우클릭 유지) **첫 반사 경로만 점선으로 미리 보여주는** 조준 보조 UI. (2번째 반사부터는 감으로)

#### 오브젝트 상호작용 (기존과 다른 점: **표면 반사**)
- 반사는 환경 레이어에서만 일어납니다.
- `PhysicMaterial` 대신 **`RicochetSurface` 컴포넌트**로 표면 특성을 정의:
  - 금속: 반사 O, 데미지 보너스 +0.2 추가
  - 콘크리트: 반사 O (기본)
  - 나무/흙: **반사 X**, 탄이 박히고 종료 (탄흔 이펙트)
- 반사 지점마다 스파크 이펙트를 ObjectPool에서 꺼내 재생 → 플레이어가 경로를 학습할 수 있게.

#### 데미지 방식 (기존과 다른 점: **반사 횟수에 비례해 증가**)
```
damage = baseDamage × (1 + bonusPerBounce × bounceCount + Σ surfaceBonus)
```
| 반사 횟수 | 데미지 |
|---|---|
| 0 (직격) | 15 |
| 1 | 24 |
| 2 | 33 |
| 3 | 42 |

데미지 텍스트에 `×2 BOUNCE!` 같은 표기를 붙여 보상감을 줍니다.

#### 클래스 스케치
```csharp
public class RicochetRifle : Weapon
{
    [SerializeField] private int baseDamage = 15;
    [SerializeField] private float bonusPerBounce = 0.6f;
    [SerializeField] private int maxBounces = 3;

    private Camera mainCam;
    private void Start() => mainCam = Camera.main;

    public override bool Fire()
    {
        if (IsAbleFire() == false) return false;

        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        FireRicochet(ray);
        PlayMuzzleFlash();
        return true;
    }

    private void FireRicochet(Ray ray)
    {
        int enemyMask = CombatSystem.Instance.EnemyMask;
        int mask = enemyMask | CombatSystem.Instance.EnvironmentMask;

        float remainRange = Range;
        float multiplier = 1f;

        for (int bounce = 0; bounce <= maxBounces; bounce++)
        {
            if (Physics.Raycast(ray, out RaycastHit hit, remainRange, mask) == false) return;
            Debug.DrawLine(ray.origin, hit.point, Color.yellow, 3f);

            if ((enemyMask & (1 << hit.collider.gameObject.layer)) != 0)
            {
                SendDamage(hit.collider, hit.point,
                    Mathf.RoundToInt(baseDamage * multiplier), DamageType.Ricochet);
                return;
            }

            // 반사 불가능한 표면이면 종료
            var surface = hit.collider.GetComponent<RicochetSurface>();
            if (surface != null && surface.CanRicochet == false) return;

            multiplier += bonusPerBounce + (surface != null ? surface.Bonus : 0f);
            remainRange -= hit.distance;

            Vector3 reflected = Vector3.Reflect(ray.direction, hit.normal);
            // 같은 면에 다시 맞는 것을 방지하기 위해 법선 방향으로 살짝 띄웁니다.
            ray = new Ray(hit.point + hit.normal * 0.01f, reflected);
        }
    }
}
```

#### 밸런스 / 카운터 플레이
- 직격 데미지가 AR 수준보다 낮아 "그냥 쏘기"는 비효율.
- 좁은 실내 맵에서 강하고 개활지에서는 약함 → 맵 설계와 연계.
- 자기 자신에게 튕겨 돌아오는 탄은 플레이어에게 50% 피해 (리스크) – 선택 사항.

---

### 3-3. Tesla Coil (전격총)

> "한 명에게 쏘면, 모두가 맞는다."

#### 컨셉
지속형 전기 빔. 탄창이 없고 **과열 게이지**로 관리합니다. 뭉쳐 있는 적 무리에 강하며, 맵의 금속 오브젝트를 이용해 전기를 **중계**할 수 있습니다.

#### 수치
| 항목 | 값 | 비고 |
|---|---|---|
| TotalAmmo | 사용 안 함 | 과열 게이지로 대체 |
| tickInterval | 0.1s | 데미지 판정 주기 (FireRate 재활용) |
| tickDamage | 6 | 1차 대상 |
| chainCount | 4 | 최대 점프 횟수 |
| chainRadius | 6m | |
| chainDecay | 0.75 | 점프마다 ×0.75 |
| heatPerSecond / coolPerSecond | 25 / 35 | 최대 100 |
| overheatLockTime | 2.0s | 과열 시 강제 쿨다운 |

#### 핵심 로직 (기존과 다른 점: **지속 사격 + 과열 자원**)
1. `OnFireHeld()`에서 매 프레임 열을 쌓고, `tickInterval`마다 판정.
2. 열이 100에 도달하면 **과열** → `overheatLockTime` 동안 사격 불가, 게이지가 0까지 내려가야 재사용.
3. 사격하지 않을 때는 자동 냉각. → `Reload()`는 "강제 배출"로 재정의: 즉시 0으로 만들지만 1초간 사격 불가.
4. 과열 직전(80 이상)에는 데미지 +30% → **한계까지 끌고 가는 리스크/리턴**.

#### 오브젝트 상호작용 (기존과 다른 점: **연쇄 전이**)
1. 화면 중앙 Ray로 1차 대상 판정 (`SphereCast` 반지름 0.3 – 빔이라 약간 관대하게).
2. 1차 대상 위치에서 `Physics.OverlapSphere(chainRadius, EnemyMask | ConductiveMask)`.
3. 이미 맞은 대상(`HashSet<Collider>`)을 제외하고 **가장 가까운 대상**으로 점프.
4. 점프 경로 사이에 벽이 있으면(`Linecast`) 그 대상은 건너뜀.
5. **`Conductive` 오브젝트(금속 드럼통, 철제 펜스)** 는 데미지를 받지 않지만 연쇄를 **감쇠 없이 중계**하고, 중계 반경이 1.5배.
6. **물 웅덩이(`Wet` 상태)** 에 서 있는 적은 연쇄 횟수 소모 없이 추가로 맞음.

#### 데미지 방식 (기존과 다른 점: **점프 감쇠 + 틱 데미지**)
```
damage(n) = tickDamage × chainDecay^n × (heat ≥ 80 ? 1.3 : 1)
```
초당 데미지(1차 대상) = 6 × 10틱 = 60 DPS, 4번째 연쇄 대상은 약 19 DPS.
지속적으로 맞는 적은 **감전(Shock)** 상태 → 이동속도 -30% (데미지 외 가치).

#### 클래스 스케치
```csharp
public class TeslaCoil : Weapon
{
    [Header("연쇄")]
    [SerializeField] private int tickDamage = 6;
    [SerializeField] private int chainCount = 4;
    [SerializeField] private float chainRadius = 6f;
    [SerializeField, Range(0f, 1f)] private float chainDecay = 0.75f;

    [Header("과열")]
    [SerializeField] private float maxHeat = 100f;
    [SerializeField] private float heatPerSecond = 25f;
    [SerializeField] private float coolPerSecond = 35f;
    [SerializeField] private float overheatLockTime = 2f;

    public float Heat { get; private set; }
    public bool IsOverheated { get; private set; }

    private readonly HashSet<Collider> visited = new HashSet<Collider>();
    private readonly Collider[] overlapBuffer = new Collider[16];
    private Camera mainCam;
    private bool firedThisFrame;

    private void Start() => mainCam = Camera.main;

    public override bool Fire() => IsOverheated == false;   // 눌린 순간은 체크만

    public override void OnFireHeld()
    {
        if (IsOverheated) return;
        firedThisFrame = true;

        Heat += heatPerSecond * Time.deltaTime;
        if (Heat >= maxHeat) { StartCoroutine(OverheatRoutine()); return; }

        if (CurrentFireRate < FireRate) return;  // FireRate = tickInterval
        CurrentFireRate = 0f;
        FireChain();
    }

    private void LateUpdate()
    {
        if (firedThisFrame == false && IsOverheated == false)
            Heat = Mathf.Max(0f, Heat - coolPerSecond * Time.deltaTime);
        firedThisFrame = false;
    }

    private void FireChain()
    {
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.SphereCast(ray, 0.3f, out RaycastHit hit, Range, CombatSystem.Instance.EnemyMask) == false) return;

        visited.Clear();
        float bonus = Heat >= maxHeat * 0.8f ? 1.3f : 1f;
        Collider current = hit.collider;
        Vector3 point = hit.point;
        int jump = 0;

        while (current != null && jump <= chainCount)
        {
            visited.Add(current);
            int damage = Mathf.RoundToInt(tickDamage * Mathf.Pow(chainDecay, jump) * bonus);
            SendDamage(current, point, damage, DamageType.Electric);
            // (Conductive 중계 처리는 생략 – 데미지 없이 jump를 증가시키지 않음)

            current = FindNearestUnvisited(point);
            if (current != null) point = current.bounds.center;
            jump++;
        }
    }

    private Collider FindNearestUnvisited(Vector3 from)
    {
        int count = Physics.OverlapSphereNonAlloc(from, chainRadius, overlapBuffer, CombatSystem.Instance.EnemyMask);
        Collider nearest = null;
        float best = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            Collider c = overlapBuffer[i];
            if (visited.Contains(c)) continue;
            if (Physics.Linecast(from, c.bounds.center, CombatSystem.Instance.EnvironmentMask)) continue;
            float d = (c.bounds.center - from).sqrMagnitude;
            if (d < best) { best = d; nearest = c; }
        }
        return nearest;
    }

    private System.Collections.IEnumerator OverheatRoutine()
    {
        IsOverheated = true;
        yield return new WaitForSeconds(overheatLockTime);
        while (Heat > 0f) { Heat -= coolPerSecond * Time.deltaTime; yield return null; }
        Heat = 0f;
        IsOverheated = false;
    }
}
```

> 주의: 틱마다 최대 5건의 `CombatEvent`가 발생하므로 `CombatSystem.maxEventsPerFrame(10)`을 넘기기 쉽습니다. Tesla 사용 시 처리량을 올리거나, 데미지 텍스트를 **대상별로 합산 표시**하는 방식이 필요합니다.

#### 밸런스 / 카운터 플레이
- 단일 대상 DPS는 AR보다 약간 낮게 → 무리 상대 전용으로 포지셔닝.
- 사거리 25m로 짧게 제한 (빔 시각 효과도 이 거리에서 끊김).

---

### 3-4. Flamethrower (화염방사기)

> "조준할 필요 없다. 태우고, 지나가라."

#### 컨셉
Ray를 쓰지 않는 유일한 근거리 범위 무기. 즉발 피해는 약하지만 **화상이 중첩**되어 시간이 지날수록 강해지고, 맵의 가연성 오브젝트에 불을 붙여 **지역을 장악**합니다.

#### 수치
| 항목 | 값 | 비고 |
|---|---|---|
| TotalAmmo | 100 (연료) | 초당 20 소모 → 5초 분사 |
| tickInterval | 0.15s | |
| coneAngle / Range | 30° / 8m | |
| directDamage | 2 | 틱당 즉발 |
| burnDamagePerStack | 4 / s | |
| maxBurnStack | 5 | |
| burnDuration | 3s | 새로 맞을 때마다 갱신 |

#### 핵심 로직 (기존과 다른 점: **Ray 없음, 연료 연속 소모**)
1. `OnFireHeld()` 동안 연료를 `fuelPerSecond × deltaTime`만큼 차감 (정수 Ammo 대신 float 연료 → `CurrentAmmo`는 UI 표시용 반올림값).
2. `tickInterval`마다 **원뿔 범위 판정**:
   - `Physics.OverlapSphereNonAlloc(총구, Range, EnemyMask | FlammableMask)`
   - `Vector3.Angle(forward, target - 총구) ≤ coneAngle / 2` 인 것만 통과
   - 총구→대상 `Linecast`로 벽 뒤 대상 제외
3. 파티클은 시각용일 뿐 판정에 쓰지 않음 (성능·결정성 확보).
4. 분사 시작 0.3초 동안은 사거리가 0→8m로 늘어나는 **점화 지연** → 탭 분사로 남용 방지.

#### 오브젝트 상호작용 (기존과 다른 점: **범위 점화 + 환경 확산**)
- **`Flammable` 컴포넌트**를 가진 오브젝트(나무 상자, 풀숲, 기름통)는 불에 닿으면 `ignitionThreshold`(누적 열량) 초과 시 점화.
- 점화된 오브젝트는 일정 시간 **화염 장판**이 되어 주변 적에게 화상 스택을 부여 → 2차 확산.
- 기름통은 점화 2초 후 폭발 (Sticky Charge와 같은 범위 피해 로직 재사용).
- 물 웅덩이(`Wet`) 상태의 적은 화상에 면역, 대신 Tesla 연쇄에 취약 → **무기 간 시너지/상성**.

#### 데미지 방식 (기존과 다른 점: **상태 이상 기반 지속 피해(DoT)**)
```
틱마다: directDamage 즉시 + burnStack = min(burnStack + 1, maxBurnStack), 지속시간 갱신
초마다: burnDamagePerStack × burnStack (화상이 끝날 때까지)
```
- 5스택 화상 = 초당 20, 지속시간 3초 → 불에서 벗어나도 최대 60의 잔여 피해.
- 화상 데미지는 무기가 아닌 **대상 쪽 `StatusEffectController`** 가 1초마다 `HealEvent`처럼 `CombatEvent`를 생성 (Sender는 원래 공격자 유지 → 킬 판정용).

#### 클래스 스케치
```csharp
public class Flamethrower : Weapon
{
    [SerializeField] private float coneAngle = 30f;
    [SerializeField] private float fuelPerSecond = 20f;
    [SerializeField] private int directDamage = 2;
    [SerializeField] private BurnData burn = new BurnData(4, 5, 3f);
    [SerializeField] private Transform muzzle;

    private float fuel;
    private readonly Collider[] buffer = new Collider[32];

    public override bool Fire() => fuel > 0f;

    public override void OnFireHeld()
    {
        if (fuel <= 0f) return;
        fuel -= fuelPerSecond * Time.deltaTime;
        CurrentAmmo = Mathf.CeilToInt(fuel);

        if (CurrentFireRate < FireRate) return;
        CurrentFireRate = 0f;

        int mask = CombatSystem.Instance.EnemyMask;   // | FlammableMask
        int count = Physics.OverlapSphereNonAlloc(muzzle.position, Range, buffer, mask);
        for (int i = 0; i < count; i++)
        {
            Vector3 toTarget = buffer[i].bounds.center - muzzle.position;
            if (Vector3.Angle(muzzle.forward, toTarget) > coneAngle * 0.5f) continue;
            if (Physics.Linecast(muzzle.position, buffer[i].bounds.center,
                                 CombatSystem.Instance.EnvironmentMask)) continue;

            SendDamage(buffer[i], buffer[i].ClosestPoint(muzzle.position), directDamage, DamageType.Fire);
            buffer[i].GetComponentInParent<StatusEffectController>()?.ApplyBurn(burn, Player.Player.LocalPlayer);
        }
    }
}
```

#### 필요한 신규 시스템
- `StatusEffectController` (몬스터에 부착): 화상/감전/젖음 등 상태 관리, 스택·지속시간·틱 처리.
- `Flammable` (환경): 누적 열량, 점화 상태, 화염 장판 생성.

#### 밸런스 / 카운터 플레이
- 8m 사거리 → 근접 강요. 원거리 적에게 무력.
- 연료 재충전은 `Reload()` 2.5초로 다른 총보다 길게.

---

### 3-5. Sticky Charge Launcher (점착 폭탄 발사기)

> "쏘는 것과 터뜨리는 것은 별개다."

#### 컨셉
**발사와 기폭이 분리된** 함정형 무기. 적이 지나갈 길목에 미리 설치하거나, 적 몸에 직접 붙여 타이밍을 골라 터뜨립니다. 히트스캔이 아닌 **실제 물리 투사체**를 사용합니다.

#### 수치
| 항목 | 값 | 비고 |
|---|---|---|
| TotalAmmo | 8 | |
| FireRate | 0.5s | |
| launchSpeed | 22 m/s | 중력 영향 받음 (포물선) |
| maxActiveCharges | 4 | 초과 시 가장 오래된 것 자동 기폭 |
| armTime | 0.6s | 부착 후 기폭 가능까지 |
| explosionRadius | 5m | |
| maxDamage | 90 | 폭심 |
| stuckBonus | 1.5 | 붙어 있는 대상에게 배율 |

#### 핵심 로직 (기존과 다른 점: **2단 입력 + 설치물 관리**)
1. `Fire()` : `IsAbleFire()` 통과 시 ObjectPool에서 `StickyCharge`를 꺼내 총구 위치에 놓고 `Rigidbody.velocity = 조준방향 × launchSpeed`.
2. 발사된 폭탄은 `List<StickyCharge> activeCharges`에 등록. 4개 초과 시 가장 오래된 것을 자동 기폭.
3. `AltFire()` (우클릭) : `armTime`이 지난 폭탄을 **전부 동시 기폭**. 아직 무장 안 된 것은 무장 즉시 터지도록 예약.
4. 무기 교체 시에도 설치된 폭탄은 유지 → 다른 무기로 적을 몰아넣고 다시 꺼내 기폭하는 플레이 가능.

#### 오브젝트 상호작용 (기존과 다른 점: **표면/대상에 부착**)
- 투사체의 `OnCollisionEnter`에서:
  - `rigidbody.isKinematic = true`로 정지
  - `transform.SetParent(collision.transform)`로 대상에 붙음 → **움직이는 적에 붙으면 함께 이동**
  - 접촉 법선 방향으로 회전시켜 표면에 박힌 것처럼 연출
- 적에 붙으면 적은 "폭탄이 붙었다"는 경고 아이콘을 머리 위에 띄움 (AI가 도망가는 행동 패턴의 트리거로 활용 가능).
- 폭발 시 반경 안의 **Rigidbody에 `AddExplosionForce`** → 물리 소품이 날아감.
- 붙어 있던 적이 죽으면 폭탄은 바닥으로 떨어짐(`SetParent(null)`, `isKinematic = false`).

#### 데미지 방식 (기존과 다른 점: **거리 감쇠 범위 피해 + 부착 보너스 + 중첩**)
```
falloff  = 1 - (distance / explosionRadius)²      // 중심부는 완만, 외곽에서 급감
damage   = maxDamage × falloff × (붙은 대상이면 stuckBonus)
```
- 폭심과 대상 사이 `Linecast`로 **엄폐물 뒤 대상은 50% 감소**.
- 폭탄 여러 개를 한 적에게 붙이면 각각 따로 계산되어 **합산** → 4개 = 즉사급 (최대 540).
- 플레이어 본인도 피해를 받음 (자폭 30%) → 근거리 남용 억제.

#### 클래스 스케치
```csharp
public class StickyLauncher : Weapon
{
    [SerializeField] private string chargePoolKey = "StickyCharge";
    [SerializeField] private Transform muzzle;
    [SerializeField] private float launchSpeed = 22f;
    [SerializeField] private int maxActiveCharges = 4;

    private readonly List<StickyCharge> activeCharges = new List<StickyCharge>();
    private Camera mainCam;

    private void Start() => mainCam = Camera.main;

    public override bool Fire()
    {
        if (IsAbleFire() == false) return false;

        if (activeCharges.Count >= maxActiveCharges)
        {
            activeCharges[0].Detonate();   // 가장 오래된 것
            activeCharges.RemoveAt(0);
        }

        var charge = ObjectPoolManager.Instance.GetObjectOrNull(chargePoolKey) as StickyCharge;
        Vector3 dir = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)).direction;
        charge.Launch(muzzle.position, dir * launchSpeed, onDetonated: c => activeCharges.Remove(c));
        activeCharges.Add(charge);

        PlayMuzzleFlash();
        return true;
    }

    public override void AltFire()
    {
        // 순회 중 리스트가 바뀌므로 복사본으로 처리합니다.
        foreach (var charge in activeCharges.ToArray()) charge.Detonate();
    }
}

// 투사체 쪽 (IObjectPoolItem 구현)
public class StickyCharge : MonoBehaviour, IObjectPoolItem
{
    [SerializeField] private float armTime = 0.6f;
    [SerializeField] private float radius = 5f;
    [SerializeField] private int maxDamage = 90;
    [SerializeField] private float stuckBonus = 1.5f;

    private Rigidbody rb;
    private Collider stuckTarget;
    private float stuckTime;
    ...

    private void OnCollisionEnter(Collision collision)
    {
        if (rb.isKinematic) return;
        rb.isKinematic = true;
        transform.SetParent(collision.transform, worldPositionStays: true);
        transform.rotation = Quaternion.LookRotation(-collision.GetContact(0).normal);
        stuckTarget = collision.collider;
        stuckTime = Time.time;
    }

    public void Detonate()
    {
        // armTime 미만이면 무장될 때까지 대기 후 폭발 (코루틴, 생략)
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, CombatSystem.Instance.EnemyMask);
        foreach (var hit in hits)
        {
            float dist = Vector3.Distance(transform.position, hit.ClosestPoint(transform.position));
            float falloff = 1f - Mathf.Pow(dist / radius, 2f);
            float bonus = hit == stuckTarget ? stuckBonus : 1f;
            if (Physics.Linecast(transform.position, hit.bounds.center, CombatSystem.Instance.EnvironmentMask))
                bonus *= 0.5f;
            // Weapon.SendDamage와 동일한 CombatEvent 생성 (static 유틸로 분리 권장)
            DamageUtil.Send(hit, hit.ClosestPoint(transform.position),
                            Mathf.RoundToInt(maxDamage * falloff * bonus), DamageType.Explosion);
        }
        // 물리 소품 밀어내기, 폭발 이펙트, ReturnToPool()
    }
}
```

> 설계 포인트: 데미지 생성이 Weapon 밖(투사체)에서도 일어나므로 2-2의 `SendDamage`를 `static DamageUtil`로 빼는 것이 좋습니다. GrenadeLauncher 구현 시에도 그대로 재사용됩니다.

#### 밸런스 / 카운터 플레이
- 기폭 전까지는 0 데미지 → 반응 속도가 아닌 **예측력** 싸움.
- 붙은 적이 플레이어에게 달려오면 자폭 위험 → 긴장감.

---

### 3-6. Harpoon Gun (작살총)

> "꿰뚫고, 끌고 가고, 벽에 박는다."

#### 컨셉
물리 엔진을 데미지 공식의 일부로 쓰는 무기. 작살이 적을 꿰뚫은 뒤 **계속 날아가 벽에 적을 박아버리고**, 그 충돌 속도만큼 추가 피해를 줍니다. 회수 시 적을 플레이어 쪽으로 끌어당길 수도 있습니다.

#### 수치
| 항목 | 값 | 비고 |
|---|---|---|
| TotalAmmo | 1 | 매 발 장전 (Reload 1.2s) |
| projectileSpeed | 40 m/s | 중력 약하게 적용 |
| pierceDamage | 35 | 꿰뚫는 순간 |
| impactDamagePerSpeed | 3 | 벽 충돌 시 속도(m/s)당 |
| pinDuration | 3s | 벽에 고정되는 시간 |
| maxDragMass | 150kg | 이보다 무거운 적은 끌고 가지 못하고 박히기만 함 |
| tetherLength | 30m | 회수 가능 거리 |

#### 핵심 로직 (기존과 다른 점: **발사 → 비행 → 회수의 상태 머신, 1발 장전**)
작살은 무기와 **줄(Tether)로 연결된 상태**를 가지며, 무기 자체가 상태 머신으로 동작합니다.

```
Ready ──Fire()──▶ Flying ──충돌──▶ Embedded ──AltFire()──▶ Retracting ──도착──▶ Empty ──Reload()──▶ Ready
                     │                                                          ▲
                     └──── tetherLength 초과 ─────────── 자동 Retracting ─────────┘
```
- `Ready` 외 상태에서는 `Fire()` 불가. 회수하지 않으면 재장전도 불가 → **한 발의 무게감**.
- `LineRenderer`로 총구와 작살 사이 줄을 그림.

#### 오브젝트 상호작용 (기존과 다른 점: **대상을 운반하고 환경에 고정**)
1. **적에 맞음** (`maxDragMass` 이하): 작살이 멈추지 않고 적을 **자식으로 붙여 함께 비행**. 적의 AI/NavMeshAgent는 일시 정지.
2. **운반 중 벽에 충돌**: 작살과 적 모두 벽에 고정 (`pinDuration` 동안 행동 불가). 이때 충돌 속도로 추가 피해.
3. **적이 무거움**: 그냥 박히고 작살은 그 자리에서 정지 (일반 투사체처럼).
4. **회수 (`AltFire`)**:
   - 작살이 벽/바닥에 박혀 있으면 → **플레이어가 그 지점으로 끌려감** (그래플링 이동기)
   - 작살이 적에 박혀 있으면 → **적이 플레이어 쪽으로 끌려옴** (근접전으로 연계)
   - 작살이 무거운 적에 박혀 있으면 → 플레이어가 적에게 끌려감
5. 물리 소품(상자, 드럼통)도 꿰뚫어 운반 가능 → 소품을 다른 적에게 박아 충돌 피해를 줄 수 있음.

#### 데미지 방식 (기존과 다른 점: **물리 충돌 속도 비례 피해**)
```
1단계 (꿰뚫음)  : pierceDamage                           = 35
2단계 (벽 충돌) : impactDamagePerSpeed × 충돌 시점 속도   = 3 × v
3단계 (회수 시)  : 끌려온 거리 × 0.5 (찢어지는 피해, 선택)
```
- 이동하면서 속도가 공기저항으로 감소하므로, **적과 벽 사이 거리가 짧을수록 강함**.
  - 적 바로 뒤에 벽(5m) → 속도 ≈ 38 → 35 + 114 = **149**
  - 벽이 멀리(25m) → 속도 ≈ 25 → 35 + 75 = **110**
  - 뒤에 벽 없음 → **35**만
- 벽에 박힌 적은 `Pinned` 상태로 **받는 피해 +25%** → 다른 무기로 마무리하는 콤보 유도.
- 운반 중인 적이 다른 적과 부딪히면, 부딪힌 적도 같은 공식의 50% 피해 (볼링).

#### 클래스 스케치
```csharp
public class HarpoonGun : Weapon
{
    public enum State { Ready, Flying, Embedded, Retracting, Empty }

    [SerializeField] private HarpoonProjectile harpoon;   // 단일 인스턴스 재사용 (풀 불필요)
    [SerializeField] private Transform muzzle;
    [SerializeField] private LineRenderer tether;
    [SerializeField] private float projectileSpeed = 40f;
    [SerializeField] private float tetherLength = 30f;

    public State CurrentState { get; private set; } = State.Ready;
    private Camera mainCam;

    private void Start()
    {
        mainCam = Camera.main;
        harpoon.OnEmbedded += () => CurrentState = State.Embedded;
        harpoon.OnRetracted += () => CurrentState = State.Empty;
    }

    public override bool Fire()
    {
        if (CurrentState != State.Ready) return false;
        if (IsAbleFire() == false) return false;

        Vector3 dir = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)).direction;
        harpoon.Launch(muzzle.position, dir * projectileSpeed);
        CurrentState = State.Flying;
        PlayMuzzleFlash();
        return true;
    }

    public override void AltFire()
    {
        if (CurrentState != State.Embedded && CurrentState != State.Flying) return;
        harpoon.Retract(toward: transform);   // 박힌 대상 질량에 따라 누가 끌려갈지 결정
        CurrentState = State.Retracting;
    }

    public new void Reload()   // 기반 Reload를 virtual로 바꾸는 것을 권장
    {
        if (CurrentState != State.Empty) return;  // 회수하지 않으면 재장전 불가
        base.Reload();
        CurrentState = State.Ready;
    }

    private void LateUpdate()
    {
        bool visible = CurrentState is State.Flying or State.Embedded or State.Retracting;
        tether.enabled = visible;
        if (visible) tether.SetPositions(new[] { muzzle.position, harpoon.transform.position });

        if (CurrentState == State.Flying &&
            Vector3.Distance(muzzle.position, harpoon.transform.position) > tetherLength)
            AltFire();   // 줄 길이 초과 → 자동 회수
    }
}

public class HarpoonProjectile : MonoBehaviour
{
    [SerializeField] private int pierceDamage = 35;
    [SerializeField] private float impactDamagePerSpeed = 3f;
    [SerializeField] private float maxDragMass = 150f;

    public event System.Action OnEmbedded;
    public event System.Action OnRetracted;

    private Rigidbody rb;
    private Transform carried;   // 운반 중인 적
    ...

    private void OnTriggerEnter(Collider other)
    {
        bool isEnemy = (CombatSystem.Instance.EnemyMask & (1 << other.gameObject.layer)) != 0;

        if (isEnemy && carried == null)
        {
            DamageUtil.Send(other, transform.position, pierceDamage, DamageType.Pierce);
            Rigidbody enemyRb = other.attachedRigidbody;
            if (enemyRb != null && enemyRb.mass <= maxDragMass)
            {
                carried = other.transform;
                carried.SetParent(transform);   // 함께 비행 (AI 정지는 StatusEffect로)
                return;                         // 멈추지 않고 계속 날아감
            }
            Embed(other.transform);
            return;
        }

        // 환경 충돌
        if (carried != null)
        {
            float speed = rb.velocity.magnitude;
            Collider carriedCol = carried.GetComponent<Collider>();
            DamageUtil.Send(carriedCol, transform.position,
                            Mathf.RoundToInt(impactDamagePerSpeed * speed), DamageType.Impact);
            carried.GetComponentInParent<StatusEffectController>()?.ApplyPinned(3f);
        }
        Embed(other.transform);
    }

    private void Embed(Transform surface)
    {
        rb.isKinematic = true;
        transform.SetParent(surface, true);
        OnEmbedded?.Invoke();
    }
}
```

#### 밸런스 / 카운터 플레이
- 1발 + 회수 필수 → 빗나가면 큰 공백. 대신 회수 자체가 이동기라서 "빗맞아도 쓸모 있는" 무기.
- 보스(`mass > maxDragMass`)에게는 운반이 안 되므로 꿰뚫기 + 그래플링 용도로만.
- 벽에 박힌 적 근처는 다른 적이 구출(박힌 적을 끌어냄)하는 AI 패턴을 넣으면 더 재미있음.

---

## 4. 무기 간 시너지 / 상성 맵

상태 이상과 환경 상호작용이 겹치도록 설계해서, 무기 교체 자체가 전략이 되게 합니다.

| 선행 무기 → | 후속 무기 | 효과 |
|---|---|---|
| Harpoon (벽에 고정) | Railgun | 움직이지 않는 적 → 풀차지 확정 명중, Pinned +25% |
| Harpoon (적 끌어오기) | Shotgun / Flamethrower | 원거리 적을 근거리 무기 사거리로 |
| Flamethrower (기름통 점화) | Sticky Charge | 기름통에 폭탄 부착 → 연쇄 폭발 |
| 물 웅덩이 (Wet) | Tesla Coil | 연쇄 횟수 소모 없이 추가 전이 |
| 물 웅덩이 (Wet) | Flamethrower | **화상 면역** (상성 불리) |
| Sticky Charge (적 뭉치기 – 도망 AI 유도) | Tesla Coil | 뭉친 적에게 연쇄 극대화 |
| Ricochet (금속 표면) | Tesla Coil | 같은 금속 소품이 Ricochet 보너스 + Tesla 중계 |

---

## 5. 구현 우선순위 제안

수업/학습 흐름 기준으로, 기존 구조를 조금씩 확장하는 순서입니다.

| 순서 | 무기 | 새로 배우는 것 | 선행 작업 |
|---|---|---|---|
| 1 | Ricochet Rifle | `Vector3.Reflect`, 루프로 Ray 이어 쏘기, 법선 | `EnvironmentMask`, `SendDamage` 분리 |
| 2 | Railgun | `RaycastAll` + 정렬, 입력 3단계(Down/Held/Up) | `OnFireHeld/Released` 추가 |
| 3 | Tesla Coil | `OverlapSphereNonAlloc`, 그래프 탐색(최근접), 코루틴 쿨다운 | `CombatSystem` 처리량 조정 |
| 4 | Sticky Charge | Rigidbody 투사체, `OnCollisionEnter`, `SetParent`, 오브젝트 풀 재사용 | `DamageUtil` static 분리, `AltFire` |
| 5 | Flamethrower | 원뿔 판정(`Vector3.Angle`), 상태 이상(DoT) 시스템 | `StatusEffectController` 신규 |
| 6 | Harpoon Gun | 상태 머신, 물리 속도 기반 데미지, 이동기 | 위 전부 + `Reload` virtual화 |

---

## 6. 요구사항 검증표

| 무기 | ① 핵심 로직이 다른가 | ② 오브젝트 작용이 다른가 | ③ 데미지 방식이 다른가 |
|---|---|---|---|
| Railgun | ✅ 누름-유지-뗌 차지 | ✅ 다중 관통 + 풀차지 벽 관통 | ✅ 차지² 비례 × 관통 감쇠 |
| Ricochet Rifle | ✅ 다구간 경로 계산 | ✅ 표면 반사 / 재질별 반응 | ✅ 반사 횟수 비례 증가 |
| Tesla Coil | ✅ 지속 사격 + 과열 자원 | ✅ 적 간 연쇄 + 도체 중계 | ✅ 틱 데미지 × 점프 감쇠 |
| Flamethrower | ✅ Ray 없는 범위 판정 + 연료 | ✅ 원뿔 범위 + 환경 점화·확산 | ✅ 중첩 화상 DoT |
| Sticky Charge | ✅ 발사/기폭 분리 + 설치물 관리 | ✅ 물리 투사체 부착 + 폭발 물리력 | ✅ 거리 감쇠 범위 + 부착 보너스 + 합산 |
| Harpoon Gun | ✅ 상태 머신 + 회수 필수 1발 장전 | ✅ 대상 운반 + 벽 고정 + 그래플 | ✅ 충돌 속도 비례 물리 피해 |
