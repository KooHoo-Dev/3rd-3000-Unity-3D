using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Study3D.FpsGame
{
    public class BossMonster : MonoBehaviour, IDamageable
    {
        public enum Parts
        {
            Unknown,
            Spine,
            Head,
            LeftArm,
            RightArm,
        }

        [Serializable]
        public class BossPartsGroup
        {
            [SerializeField] private Collider spineCollider;
            [SerializeField] private Collider headCollider;
            [SerializeField] private Collider[] leftArmCollider;
            [SerializeField] private Collider[] rightArmCollider;

            private readonly Dictionary<Collider, Parts> partsDic = new Dictionary<Collider, Parts>();

            public Collider SpineCollider => spineCollider;
            public IEnumerable<Collider> Colliders => partsDic.Keys;

            public void Initialize()
            {
                partsDic[spineCollider] = Parts.Spine;
                partsDic[headCollider] = Parts.Head;
                foreach (var collider in rightArmCollider) partsDic[collider] = Parts.RightArm;
                foreach (var collider in leftArmCollider) partsDic[collider] = Parts.LeftArm;
            }

            public Parts GetBossPart(Collider collider)
            {
                return partsDic.TryGetValue(collider, out var part) ? part : Parts.Unknown;
            }
        }

        public static BossMonster CurrentSceneBossMonster { get; private set; }

        [Header("능력치")]
        [SerializeField] private int maxHp = 5000;

        [Header("부위별 충돌체")]
        [FormerlySerializedAs("BossPartsGroup")]
        [SerializeField] private BossPartsGroup bossPartsGroup;

        [Header("애니메이터")]
        [SerializeField] private Animator animator;

        private readonly Dictionary<BossState.StateName, BossState> statesDic = new Dictionary<BossState.StateName, BossState>();
        private readonly List<BossState> attackStates = new List<BossState>();

        public Collider MainCollider => bossPartsGroup.SpineCollider;
        public GameObject GameObject => gameObject;
        public Animator Animator => animator;
        public Health Health { get; private set; }
        public BossState CurrentState { get; private set; }
        public bool IsDead { get; private set; }

        public event Action OnDied;
        public IReadOnlyList<BossState> AttackStates => attackStates;

        private void Awake()
        {
            CurrentSceneBossMonster = this;
            Health = new Health(maxHp);
        }

        private void OnDestroy()
        {
            if (CurrentSceneBossMonster == this) CurrentSceneBossMonster = null;
        }

        private void Start()
        {
            bossPartsGroup.Initialize();
            foreach (var collider in bossPartsGroup.Colliders)
                CombatSystem.Instance.RegisterMonster(collider, this);

            BossState[] states = GetComponentsInChildren<BossState>();
            for (int i = 0; i < states.Length; i++)
            {
                statesDic.Add(states[i].Name, states[i]);
                if (states[i].IsAttack) attackStates.Add(states[i]);
                states[i].Initialize(this);
                states[i].gameObject.SetActive(false);
            }

            ChangeState(BossState.StateName.IdleState);
        }

        public Parts GetPart(Collider collider)
        {
            return bossPartsGroup.GetBossPart(collider);
        }

        public void ChangeState(BossState.StateName newState)
        {
            if (CurrentState != null)
            {
                var prev = CurrentState;
                prev.Exit();
                prev.gameObject.SetActive(false);
            }

            var target = statesDic[newState];
            CurrentState = target;

            if (target.AnimatorTrigger != null) animator.SetTrigger(target.AnimatorTrigger);

            target.gameObject.SetActive(true);
            target.Enter();
        }

        public void TakeDamage(CombatEvent combatEvent)
        {
            if (IsDead) return;

            Health.TakeDamage(combatEvent.Damage);

            if (Health.IsDead) Die();
        }

        private void Die()
        {
            IsDead = true;

            foreach (var collider in bossPartsGroup.Colliders)
                CombatSystem.Instance.UnregisterMonster(collider);

            ChangeState(BossState.StateName.DeadState);
            OnDied?.Invoke();
        }

        public void TakeHeal(HealEvent healEvent)
        {
            if (IsDead) return;
            Health.Heal(healEvent.Heal);
        }
    }
}
