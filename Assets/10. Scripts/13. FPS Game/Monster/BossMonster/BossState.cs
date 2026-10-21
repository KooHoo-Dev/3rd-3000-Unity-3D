using UnityEngine;
using UnityEngine.Serialization;

namespace Study3D.FpsGame
{
    public abstract class BossState : MonoBehaviour
    {
        public enum StateName
        {
            IdleState,
            ScratchState,
            BreathState,
            SummonState,
            DeadState,
        }

        [Header("애니메이터 상태와 끝나는 시점")]
        [FormerlySerializedAs("AnimatorStateName")]
        [SerializeField] protected string animatorStateName;
        [Tooltip("이 진행률(normalizedTime)을 넘으면 다음 상태로 넘어갑니다")]
        [FormerlySerializedAs("ExitTime")]
        [SerializeField] protected float exitTime;

        protected BossMonster bossMonster;
        protected Animator animator;

        public abstract StateName Name { get; }

        public virtual bool IsAttack => false;

        public virtual string AnimatorTrigger => null;

        public virtual void Initialize(BossMonster bossMonster)
        {
            this.bossMonster = bossMonster;
            animator = bossMonster.Animator;
        }

        public virtual void Enter()
        {
        }

        public virtual void Exit()
        {
        }

        private void Update()
        {
            var currentState = animator.GetCurrentAnimatorStateInfo(0);
            if (currentState.IsName(animatorStateName) == false) return;

            if (currentState.normalizedTime > exitTime)
            {
                OnAnimationEnd();
            }
        }

        protected virtual void OnAnimationEnd()
        {
            bossMonster.ChangeState(StateName.IdleState);
        }
    }
}
