using System;
using UnityEngine;

namespace Study3D.FpsGame.Player
{
    public partial class PlayerController
    {
        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Start()
        {
            LockCursor();
            initialCameraRotation = cameraTransform.localRotation;
        }

        private void OnEnable()
        {
            // 싱글톤 개체를 못찾으니까, 컴포넌트로 가져와 줍시다.
            var localPlayer = GetComponent<LocalPlayer>();
            localPlayer.OnStartChangedWeapon += PlayWeaponChangeAnimation;
            localPlayer.OnRequestedReload += PlayReloadAnimation;
        }

        private void OnDisable()
        {
            Player.LocalPlayer.OnStartChangedWeapon -= PlayWeaponChangeAnimation;
            Player.LocalPlayer.OnRequestedReload -= PlayReloadAnimation;
        }
        
        private void Update()
        {
            UpdateMovement();
            UpdateCameraRotation();
            UpdatePlayerInput();
        }
        
        private void LockCursor()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}