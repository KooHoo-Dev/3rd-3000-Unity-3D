using System;
using UnityEngine;

namespace Study3D.FpsGame.Weapon
{
    // 두번에 걸쳐서 공통함수를 뽑아내는 과정을 다시 보여드릴겁니다.
    
    public class AssaultRifle : Weapon
    {
        private Camera mainCam;

        private void Start()
        {
            mainCam = Camera.main;
            
        }

        public override bool Fire()
        {
            // 발사 가능한지 체크해줍니다.
            // 함수를 호출하는쪽에서 False를 받으면 발사 불가능에 대한 연출(피드백, 사운드) 처리를 합니다.
            if (IsAbleFire() == false) return false;
            
            // 발사 가능 상태입니다. => 사격처리를 하면 됩니다.
            // 어썰트 라이플의 사격은 화면의 중앙(Crosshair 위치)을 기준으로
            // 카메라 -> 월드 -> Ray까지 이어지는 좌표 변환을 활용하여
            // Ray를 생성해서 처리합니다.
            
            // 우리가 이런 처리를 뷰포트 좌표를 활용한 변환이라고 합니다.
            // 화면 좌표계의 비율을 왼쪽 아래가 (0, 0). 오른쪽 위가 (1, 1)인 비율 좌표 입니다.
            // (0.5, 0.5)로(화면 중앙 비율) 연산하면 언제나 한가운데에서 출발하는 Ray를 생성할 수 있습니다.
            Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            FireRay(ray);
            PlayMuzzleFlash();
            
            return true;
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
            //Debug.DrawRay(ray.origin, ray.direction * Range, Color.red, 10f);
            
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