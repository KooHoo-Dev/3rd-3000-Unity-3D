using UnityEngine;

namespace Study3D.FpsGame
{
    public class ScratchState : BossState
    {
        [Header("할퀴기 참조")]
        [SerializeField] private HitBox scratch;

        public override StateName Name => StateName.ScratchState;
        public override bool IsAttack => true;
        public override string AnimatorTrigger => "Scratch";

        public override void Initialize(BossMonster bossMonster)
        {
            base.Initialize(bossMonster);
            scratch.gameObject.SetActive(false);
        }

        public override void Enter()
        {
            scratch.gameObject.SetActive(true);
        }

        public override void Exit()
        {
            scratch.gameObject.SetActive(false);
        }
    }
}
