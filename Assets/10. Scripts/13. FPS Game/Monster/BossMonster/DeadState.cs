namespace Study3D.FpsGame
{
    public class DeadState : BossState
    {
        public override StateName Name => StateName.DeadState;
        public override string AnimatorTrigger => "Dead";

        protected override void OnAnimationEnd()
        {
        }
    }
}
