using UnityEngine;

namespace Study3D.FpsGame
{
    public class Health
    {
        public int HP { get; private set; }
        public int MaxHP { get; private set; }
        public bool IsDead => HP <= 0;

        public Health(int maxHp)
        {
            MaxHP = maxHp;
            HP = maxHp;
        }

        public void TakeDamage(int damage)
        {
            HP = Mathf.Max(0, HP - damage);
        }

        public void Heal(int amount)
        {
            HP = Mathf.Min(MaxHP, HP + amount);
        }
    }
}
