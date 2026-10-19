using UnityEngine;

namespace Study3D.FpsGame
{
    public interface IDamageable
    {
        Collider MainCollider { get; }
        GameObject GameObject { get; }

        void TakeDamage(CombatEvent combatEvent);
        void TakeHeal(HealEvent healEvent);
    }
}
