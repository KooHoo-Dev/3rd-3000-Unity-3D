using UnityEngine;

namespace Study3D.FpsGame
{
    public class IdleState : BossState
    {
        public override StateName Name => StateName.IdleState;

        protected override void OnAnimationEnd()
        {
            var attacks = bossMonster.AttackStates;
            if (attacks.Count == 0) return;

            var next = attacks[Random.Range(0, attacks.Count)];
            bossMonster.ChangeState(next.Name);
        }
    }
}
