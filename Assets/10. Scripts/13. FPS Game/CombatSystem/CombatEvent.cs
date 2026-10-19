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

    public class CombatEvent : InGameEvent
    {
        public int Damage { get; set; }
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
