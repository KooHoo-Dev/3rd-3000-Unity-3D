using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Study3D.Mecanim
{
    public class BlendTreeMover : MonoBehaviour
    {
        private static readonly int InputXHash = Animator.StringToHash("InputX");
        private static readonly int InputYHash = Animator.StringToHash("InputY");
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int HitXHash = Animator.StringToHash("HitX");
        private static readonly int HitYHash = Animator.StringToHash("HitY");
        private static readonly int AttackHash = Animator.StringToHash("Attack");

        private const float LegacyAxisResponse = 3f;

        [Header("이동")]
        [FormerlySerializedAs("Speed")]
        [SerializeField] private float speed = 3.0f;
        [FormerlySerializedAs("daming")]
        [Range(0, 1)]
        [SerializeField] private float damping;

        [Header("피격")]
        [FormerlySerializedAs("HitTransform")]
        [SerializeField] private Transform hitTransform;

        private Animator animator;
        private Vector2 smoothedInput;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            UpdateMove(keyboard);
            if (keyboard.spaceKey.wasPressedThisFrame) PlayHit();

            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.leftButton.wasPressedThisFrame) animator.SetTrigger(AttackHash);
        }

        private void UpdateMove(Keyboard keyboard)
        {
            Vector2 rawInput = new Vector2(
                Axis(keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed, keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed),
                Axis(keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed, keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed));
            smoothedInput = Vector2.MoveTowards(smoothedInput, rawInput, LegacyAxisResponse * Time.deltaTime);

            Vector2 goalInput = smoothedInput;
            if (keyboard.leftShiftKey.isPressed) goalInput *= 2.0f;

            Vector2 currentAnimatorInput = new Vector2(animator.GetFloat(InputXHash), animator.GetFloat(InputYHash));
            Vector2 applyInput = Vector2.Lerp(currentAnimatorInput, goalInput, damping);
            animator.SetFloat(InputXHash, applyInput.x);
            animator.SetFloat(InputYHash, applyInput.y);

            transform.Translate(new Vector3(goalInput.x, 0f, goalInput.y) * speed * Time.deltaTime);
        }

        private void PlayHit()
        {
            Vector3 localPosition = transform.InverseTransformPoint(hitTransform.position);

            animator.SetTrigger(HitHash);
            animator.SetFloat(HitXHash, localPosition.normalized.x);
            animator.SetFloat(HitYHash, localPosition.normalized.z);
        }

        private static float Axis(bool positive, bool negative)
        {
            return (positive ? 1f : 0f) - (negative ? 1f : 0f);
        }

        private void FootL()
        {
        }

        private void FootR()
        {
        }

        private void Hit()
        {
        }
    }
}
