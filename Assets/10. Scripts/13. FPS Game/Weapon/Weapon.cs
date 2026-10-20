using System;
using UnityEngine;

namespace Study3D.FpsGame.Weapon
{
    public abstract partial class Weapon : MonoBehaviour
    {
        [Header("손에 쥐었을때의 위치")]
        [SerializeField] protected Pivot pivot;

        [Header("총구 화염")] 
        [SerializeField] protected GameObject muzzleFlash;
        [SerializeField] private float muzzleFlashDuration = 0.1f; // 총구 화염의 재생시간

        public int CurrentAmmo { get; protected set; }
        public float CurrentFireRate { get; protected set; }
        protected float CurrentMuzzleFlashDuration { get; set; }

        // 순수가상함수 = 추상함수
        // - 상속받은 클래스들이 재정의 해야 합니다.
        public abstract bool Fire();

        private void Awake()
        {
            muzzleFlash.SetActive(false);
            CurrentAmmo = TotalAmmo;
        }

        private void Update()
        {
            // 다음 발사 가능 시간을 체크하는 변수는 계속 더해 줍니다.
            CurrentFireRate += Time.deltaTime;

            // 총구 화염이 꺼져있다면 종료합니다.
            if (muzzleFlash.activeSelf == false) return;
            // 켜져있다면 재생시간을 더해줍니다
            CurrentMuzzleFlashDuration += Time.deltaTime;
            
            // 재생기간을 초과했다면
            if(CurrentMuzzleFlashDuration > muzzleFlashDuration) 
                muzzleFlash.SetActive(false); //꺼줍니다
        }

        public void Reload()
        {
            CurrentAmmo = totalAmmo;
        }
        
        // 데이터와 멤버 필드들을 조회해서
        // 발사 가능한 상태인지 체크합니다.
        protected bool IsAbleFire()
        {
            if(CurrentAmmo <= 0) return false; // 잔여 탄환이 없으면 발사 불가능입니다.
            
            // 내가 대기한 시간이 설정된 FireRate(왜 Rate지? Term, Duration)보다 적다면 발사 불가능 상태입니다.
            if (CurrentFireRate < FireRate) return false;
            // 발사시간은 통과. 대기한 시간을 0.0f로 바꿔줍니다.
            CurrentFireRate = 0.0f;
            // Ammo도 있습니다. 발사했으니 차감해줍니다.
            CurrentAmmo--;
            return true;
        }
        
        // Ray를 이용한 사격 판정 진행 함수 
        protected void FireRay(Ray ray)
        {
            // Raycast 검사 결과가 false라면 종료
            if (Physics.Raycast(
                    ray, out RaycastHit hit, 
                    Range, CombatSystem.Instance.EnemyMask) == false) return;
            Debug.DrawRay(ray.origin, ray.direction * Range, Color.red, 10f);
            
            CombatEvent combatEvent = new CombatEvent();
            combatEvent.Sender = Player.Player.LocalPlayer;
            combatEvent.Receiver = Player.Player.LocalPlayer; // 나중에 몬스터로
            combatEvent.Damage = 10;
            combatEvent.HitPosition = hit.point;
            combatEvent.Collider = hit.collider;
            
            CombatSystem.Instance.AddInGameEvent(combatEvent);
        }
        
        // 총구 화염을 제어하는 함수 입니다.
        protected void PlayMuzzleFlash()
        {
            // 총구 화염이 밋밋하기에 랜덤한 회전값을 부여하여 연속으로 사격 하더라도
            // 다른 모양으로 표현 될 수 있게 효과를 줍니다.
            float randomRoll = UnityEngine.Random.Range(0.0f, 360.0f);
            muzzleFlash.transform.localRotation *= Quaternion.AngleAxis(randomRoll, Vector3.right);
            
            muzzleFlash.SetActive(true);
            CurrentMuzzleFlashDuration = 0.0f;
        }
    }
}