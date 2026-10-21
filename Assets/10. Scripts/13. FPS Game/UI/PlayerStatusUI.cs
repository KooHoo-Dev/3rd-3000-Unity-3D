using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Study3D.FpsGame.Player;

namespace Study3D.FpsGame
{
    public class PlayerStatusUI : MonoBehaviour
    {
        [Header("채울 이미지 (Image Type: Filled)")]
        [SerializeField] private Image fillImage;

        [Header("무기 정보")]
        [SerializeField] private TMP_Text weaponNameText;
        [SerializeField] private TMP_Text ammoText;

        private LocalPlayer localPlayer;
        private Weapon.Weapon shownWeapon;
        private int shownAmmo;
        private bool isAmmoTextDirty = true;

        private void Start()
        {
            localPlayer = Player.Player.LocalPlayer;
        }

        private void Update()
        {
            Weapon.Weapon weapon = localPlayer.CurrentWeapon;

            if (weapon != shownWeapon)
            {
                shownWeapon = weapon;
                isAmmoTextDirty = true;
                weaponNameText.text = weapon.WeaponName;
            }

            if (isAmmoTextDirty || weapon.CurrentAmmo != shownAmmo)
            {
                isAmmoTextDirty = false;
                shownAmmo = weapon.CurrentAmmo;
                ammoText.text = $"{shownAmmo} / {weapon.TotalAmmo}";
            }

            fillImage.fillAmount = (float)localPlayer.Health.HP / localPlayer.Health.MaxHP;
        }
    }
}
