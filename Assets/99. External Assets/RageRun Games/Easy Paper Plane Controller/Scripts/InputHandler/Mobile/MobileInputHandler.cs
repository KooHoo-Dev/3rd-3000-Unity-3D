using System;
using UnityEngine;

namespace RageRunGames.EasyPaperPlaneController
{
    public class MobileInputHandler : BaseInputHandler
    {
        [SerializeField] private MobileController pitchAndYawController;
        [SerializeField] private MobileController rollController;
        [SerializeField] private MobileBoostInputController mobileBoostInputController;
        [SerializeField] private LateralForceInputController rightForceInputController;
        [SerializeField] private LateralForceInputController leftForceInputController;
        [SerializeField] private MobileFlipInputController mobileFlipInputController;
        [SerializeField] bool invertPitch = false;

        public override Vector2 ControlInput { get; set; }
        public override float RollInput { get; set; }
        public override bool IsBoosting { get; set; }

        public override bool IsRightThrustDown { get; set; }
        public override bool IsLeftThrustDown { get; set; }
        public override bool BackFlipInput { get; set; }

        public override void UpdateInputs()
        {
            if (rightForceInputController.IsActive)
            {
                IsRightThrustDown = true;
                rightForceInputController.SetLateralForce(false);
            }

            if (leftForceInputController.IsActive)
            {
                IsLeftThrustDown = true;
                leftForceInputController.SetLateralForce(false);
            }

            ControlInput = new(pitchAndYawController.Horizontal,
                invertPitch ? pitchAndYawController.Vertical : -pitchAndYawController.Vertical);
            
            RollInput = rollController.Horizontal;

            IsBoosting = mobileBoostInputController.IsActive;
            
            if (mobileFlipInputController.IsActive)
            {
                BackFlipInput = true;
                mobileFlipInputController.SetFlipping(false);
            }
        }

        public override bool IsRollInputDown { get; set; }
        public override int RollDirection { get; set; }
    }
}

