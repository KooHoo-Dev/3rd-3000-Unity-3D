using UnityEngine;

namespace Study3D.FpsGame.Weapon
{
    public class GrenadeLauncher : Weapon
    {
        [SerializeField] private Transform firePoint;
        [SerializeField] private string grenadePoolKey = "Grenade";
        [SerializeField] private float force = 100f;
        
        public override bool Fire()
        {
            if(IsAbleFire() == false) return false;
            PlayMuzzleFlash();
            Grenade grenade = ObjectPoolManager.Instance.GetObjectOrNull(grenadePoolKey) as Grenade;
            
            grenade.transform.SetPositionAndRotation(firePoint.position, firePoint.rotation);
            grenade.Damage = Damage;
            grenade.LaunchGrenade(force);

            return true;
        }
    }
}