using UnityEngine;
using UnityEngine.InputSystem;

namespace Study3D.Mecanim
{
    public class HandIKCharacter : MonoBehaviour
    {
        private const int WeaponLayerIndex = 1;

        [Header("무기")]
        [SerializeField] private Equipment assaultRifle;

        private Animator animator;
        private Equipment currentEquipment;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.digit1Key.wasPressedThisFrame) SwitchWeapon(null);
            if (keyboard.digit2Key.wasPressedThisFrame) SwitchWeapon(assaultRifle);
        }

        private void SwitchWeapon(Equipment equipment)
        {
            currentEquipment = equipment;
            animator.SetLayerWeight(WeaponLayerIndex, equipment == null ? 0.0f : 1.0f);
            assaultRifle.gameObject.SetActive(true);
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (currentEquipment == null) return;

            UpdateIK(AvatarIKGoal.RightHand, currentEquipment.RightHandTarget, currentEquipment.IkWeight);
            UpdateIK(AvatarIKGoal.LeftHand, currentEquipment.LeftHandTarget, currentEquipment.IkWeight);
        }

        private void UpdateIK(AvatarIKGoal goal, Transform target, float ikWeight)
        {
            animator.SetIKPosition(goal, target.position);
            animator.SetIKPositionWeight(goal, ikWeight);
            animator.SetIKRotation(goal, target.rotation);
            animator.SetIKRotationWeight(goal, ikWeight);
        }
    }
}
