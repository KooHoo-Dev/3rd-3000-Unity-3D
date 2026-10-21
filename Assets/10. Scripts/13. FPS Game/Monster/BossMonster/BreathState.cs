using UnityEngine;

namespace Study3D.FpsGame
{
    public class BreathState : BossState
    {
        [Header("브레스 참조")]
        [SerializeField] private HitBox breath;
        [SerializeField] private Transform breathPoint;

        public override StateName Name => StateName.BreathState;
        public override bool IsAttack => true;
        public override string AnimatorTrigger => "Breath";

        public override void Initialize(BossMonster bossMonster)
        {
            base.Initialize(bossMonster);
            breath.Follow(breathPoint, breathPoint);
            breath.gameObject.SetActive(false);
        }

        public override void Enter()
        {
            breath.transform.localRotation = Quaternion.Euler(Vector3.up * 90.0f);
            breath.gameObject.SetActive(true);
        }

        public override void Exit()
        {
            breath.gameObject.SetActive(false);
        }
    }
}
