using System;
using UnityEngine;

namespace Study3D.FpsGame.Weapon
{
    public partial class Weapon
    {
        [Header("Settings")] 
        [SerializeField] private string weaponName;
        [SerializeField] private float fireRate = 0.05f;
        [SerializeField] private int damage = 10;
        [SerializeField] private int totalAmmo = 30; //탄창에 들거간 총알의 양. 탄창량
        [SerializeField] private float range = 100.0f; // 유효 공격 범위거리

        public string WeaponName => weaponName;
        public float FireRate => fireRate;
        public int Damage => damage;
        public int TotalAmmo => totalAmmo;
        public float Range => range;
        
        [Serializable]
        public class Pivot
        {
            [SerializeField] private Vector3 localPosition;
            [SerializeField] private Vector3 localRotation;
            [SerializeField] private Vector3 localScale;

            public Vector3 LocalPosition => localPosition;
            public Vector3 LocalRotation => localRotation;
            public Vector3 LocalScale => localScale;
        }

        public enum WeaponType
        {
            Unknown,
            AssaultRifle,
            GrenadeLauncher,
            Shotgun,
        }
    }
}