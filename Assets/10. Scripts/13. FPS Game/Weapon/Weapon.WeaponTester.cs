using System;
using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Study3D.FpsGame.Weapon
{
    public class WeaponTester : MonoBehaviour
    {
        private Weapon weapon;

        private void Awake()
        {
            weapon = GetComponent<Weapon>();
        }

        private void Update()
        {
            // PlayerController와 같은 방식으로 누르고 있는 동안 매 프레임 Fire()를 호출합니다.
            // (차지, 지속 사격 무기를 테스트하려면 '누르고 있는 동안' 호출되어야 합니다)
            if (SimpleInput.GetMouseButton(SimpleInput.MouseButton.Left))
            {
                weapon.Fire();
            }

            if (SimpleInput.GetMouseButtonUp(SimpleInput.MouseButton.Left))
            {
                weapon.ReleaseFire();
            }
            
            if (SimpleInput.GetKeyDown(Key.R))
            {
                weapon.Reload();
            }
        }
    }
}