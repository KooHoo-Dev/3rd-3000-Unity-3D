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
    }
}