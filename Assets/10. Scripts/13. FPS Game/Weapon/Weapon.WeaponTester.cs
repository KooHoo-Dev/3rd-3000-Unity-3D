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
            if (SimpleInput.GetMouseButtonDown(SimpleInput.MouseButton.Left))
            {
                weapon.Fire();
            }
            
            if (SimpleInput.GetKeyDown(Key.R))
            {
                weapon.Reload();
            }
        }
    }
}