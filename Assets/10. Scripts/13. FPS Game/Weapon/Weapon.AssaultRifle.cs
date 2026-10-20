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
    }
}