using Study3D.FpsGame;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Study3D.Mecanim
{
    public class ComboAttack : MonoBehaviour
    {
        private const string InputStartSignal = "Input_Start";
        private const string InputEndSignal = "Input_End";
        private static readonly int AttackHash = Animator.StringToHash("Attack");

        private Animator animator;
        private AnimStateEventListener animEventListener;
        private bool isAttackAble;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            animEventListener = GetComponent<AnimStateEventListener>();
            isAttackAble = true;
        }

        private void OnEnable()
        {
            animEventListener.OnOccursAnimStateEvent += OnTriggerAnim;
        }

        private void OnDisable()
        {
            animEventListener.OnOccursAnimStateEvent -= OnTriggerAnim;
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (isAttackAble && mouse.leftButton.wasPressedThisFrame)
            {
                animator.SetTrigger(AttackHash);
                isAttackAble = false;
            }
        }

        private void OnTriggerAnim(string eventName, string parameter)
        {
            if (eventName.Equals(InputStartSignal)) isAttackAble = true;
            else if (eventName.Equals(InputEndSignal)) isAttackAble = false;

            Debug.Log(parameter);
        }

        private void Hit()
        {
        }
    }
}
