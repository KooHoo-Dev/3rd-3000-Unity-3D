using UnityEngine;

namespace Study3D.FpsGame
{
    public class TriggerItem : MonoBehaviour
    {
        public enum TriggerItemType
        {
            Unknown = 0,
            AssaultRifle = 100,
            GrenadeLauncher,
            Shotgun,
            SniperRifle,
            LightningGun,
            PlasmaGun,
            FlameThrower,
            HealPack = 200,
        }

        [Header("아이템")]
        [SerializeField] private float rotateSpeed = 60.0f;
        [SerializeField] private TriggerItemType type = TriggerItemType.Unknown;

        public TriggerItemType Type => type;

        private void Update()
        {
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
        }
    }

    public static class Mapper
    {
        public static Weapon.Weapon.WeaponType GetWeaponType(TriggerItem.TriggerItemType triggerItemType)
        {
            switch (triggerItemType)
            {
                case TriggerItem.TriggerItemType.AssaultRifle:
                    return Weapon.Weapon.WeaponType.AssaultRifle;
                case TriggerItem.TriggerItemType.GrenadeLauncher:
                    return Weapon.Weapon.WeaponType.GrenadeLauncher;
                case TriggerItem.TriggerItemType.Shotgun:
                    return Weapon.Weapon.WeaponType.Shotgun;
                default:
                    return Weapon.Weapon.WeaponType.Unknown;
            }
        }
    }
}
