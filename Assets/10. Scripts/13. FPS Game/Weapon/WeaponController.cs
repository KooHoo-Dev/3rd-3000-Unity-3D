using System;
using UnityEngine;

namespace Study3D.FpsGame.Weapon
{
    // 여러 Weapon을 묶어서
    // LocalPlayer, PlayerController와 엮어주는 용도로 사용하는 클래스 입니다.
    // 단순 중계를 위한 클래스라고 생각하시면 됩니다.
    
    // (총을 묶주는 용도의 클래스가 되어버렸네..?)
    public class WeaponController : MonoBehaviour
    {
        [Header("총")] 
        [SerializeField] private Weapon assaultRifle;
        [SerializeField] private Weapon grenadeLauncher;
        [SerializeField] private Weapon shotgun;
        [SerializeField] private Weapon railgun;
        [SerializeField] private Weapon ricochetRifle;
        [SerializeField] private Weapon teslaCoil;

        private void OnEnable()
        {
            // 시작하면 일단 꺼둡니다. 켜는건 외부 클래스에서
            assaultRifle.gameObject.SetActive(false);
            grenadeLauncher?.gameObject.SetActive(false);
            shotgun?.gameObject.SetActive(false);
            // ?.는 UnityEngine.Object의 '파괴됨/미할당' 체크를 하지 않으므로 if로 검사합니다.
            if (railgun != null) railgun.gameObject.SetActive(false);
            if (ricochetRifle != null) ricochetRifle.gameObject.SetActive(false);
            if (teslaCoil != null) teslaCoil.gameObject.SetActive(false);
        }

        public Weapon GetWeaponOrNull(Weapon.WeaponType type)
        {
            switch (type)
            {
                case Weapon.WeaponType.AssaultRifle:
                    return assaultRifle;
                case Weapon.WeaponType.GrenadeLauncher:
                    return grenadeLauncher;
                case Weapon.WeaponType.Shotgun:
                    return shotgun;
                case Weapon.WeaponType.Railgun:
                    return railgun;
                case Weapon.WeaponType.RicochetRifle:
                    return ricochetRifle;
                case Weapon.WeaponType.TeslaCoil:
                    return teslaCoil;
                default:
                    return null;
            }
        }
    }
}