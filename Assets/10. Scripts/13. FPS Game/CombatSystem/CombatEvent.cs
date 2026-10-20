using UnityEngine;

namespace Study3D.FpsGame
{
    public abstract class InGameEvent
    {
        public enum EventType
        {
            Unknown,
            Combat,
            Heal,
        }

        public IDamageable Sender { get; set; }
        public IDamageable Receiver { get; set; }
        public abstract EventType Type { get; }
    }

    // 피해의 종류. 피드백(이펙트, 데미지 텍스트 색상)을 다르게 보여줄 때 사용합니다.
    public enum DamageType
    {
        Normal,
        Pierce,    // 관통 (Railgun)
        Ricochet,  // 도탄 (Ricochet Rifle)
        Electric,  // 전격 (Tesla Coil)
    }

    public class CombatEvent : InGameEvent
    {
        public int Damage { get; set; }
        public DamageType DamageType { get; set; } = DamageType.Normal;
        public Vector3 HitPosition { get; set; }
        public Collider Collider { get; set; }
        public override EventType Type => EventType.Combat;
    }

    public class HealEvent : InGameEvent
    {
        public int Heal { get; set; }
        public Vector3 Position { get; set; }
        public override EventType Type => EventType.Heal;
    }
}
