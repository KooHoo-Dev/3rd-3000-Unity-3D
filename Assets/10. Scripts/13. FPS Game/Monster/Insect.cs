using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Study3D.FpsGame
{
    public class Insect : MonoBehaviour, IDamageable
    {
        private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
        private static readonly int DeathTriggerHash = Animator.StringToHash("Death");
        private static readonly int MoveStateHash = Animator.StringToHash("Move");
        private static readonly int AttackStateHash = Animator.StringToHash("Attack");

        [Serializable]
        public class MonsterStat
        {
            [SerializeField] private int hp;
            [SerializeField] private int maxHp;
            [SerializeField] private float range;
            [SerializeField] private float speed;

            public int Hp { get => hp; set => hp = value; }
            public int MaxHp => maxHp;
            public float Range => range;
            public float Speed => speed;
        }

        [Serializable]
        public class AttackAnimEvent
        {
            [SerializeField] [Range(0.0f, 1.0f)] private float timing;
            [SerializeField] private bool isAttacked;

            public float Timing => timing;
            public bool IsAttacked { get => isAttacked; set => isAttacked = value; }
        }

        [Header("능력치")]
        [SerializeField] private MonsterStat monsterStat;
        [SerializeField] private AttackAnimEvent fireAnimEvent;
        [Tooltip("죽은 뒤 이 시간(초)이 지나면 오브젝트를 지웁니다")]
        [SerializeField] private float destroyDelay = 3.0f;

        [Header("참조")]
        [FormerlySerializedAs("collider")]
        [SerializeField] private Collider bodyCollider;
        [SerializeField] private Animator animator;
        [Tooltip("탄환을 꺼낼 풀 이름. 씬의 ObjectPoolManager 목록에 적힌 Key와 글자까지 같아야 합니다")]
        [SerializeField] private string bulletPoolKey = "InsectBullet";
        [SerializeField] private Transform firePoint;

        private Transform localPlayerTransform;
        private AnimatorStateInfo prevInfo;
        private bool isDead;

        public Collider MainCollider => bodyCollider;
        public GameObject GameObject => gameObject;

        private void Start()
        {
            CombatSystem.Instance.RegisterMonster(this);
            localPlayerTransform = Player.Player.LocalPlayer.transform;
        }

        private void OnEnable()
        {
            monsterStat.Hp = monsterStat.MaxHp;
        }

        private void OnDestroy()
        {
            if (CombatSystem.Instance != null) CombatSystem.Instance.UnregisterMonster(bodyCollider);
        }

        private void Update()
        {
            if (isDead)
            {
                transform.Translate(Physics.gravity * Time.deltaTime, Space.World);
                return;
            }

            float distance = Vector3.Distance(localPlayerTransform.position, transform.position);
            Vector3 direction = (localPlayerTransform.position - transform.position).normalized;
            transform.forward = direction;

            AnimatorStateInfo currentInfo = animator.GetCurrentAnimatorStateInfo(0);

            if (currentInfo.shortNameHash == MoveStateHash)
            {
                if (monsterStat.Range >= distance)
                {
                    animator.ResetTrigger(AttackTriggerHash);
                    animator.SetTrigger(AttackTriggerHash);
                }
                else
                {
                    transform.Translate(direction * (Time.deltaTime * monsterStat.Speed), Space.World);
                }
            }
            else if (currentInfo.shortNameHash == AttackStateHash)
            {
                if (prevInfo.shortNameHash != currentInfo.shortNameHash)
                {
                    fireAnimEvent.IsAttacked = false;
                }

                if (fireAnimEvent.IsAttacked == false && fireAnimEvent.Timing < currentInfo.normalizedTime)
                {
                    fireAnimEvent.IsAttacked = true;

                    var bullet = ObjectPoolManager.Instance.GetObjectOrNull(bulletPoolKey) as InsectBullet;
                    if (bullet != null)
                    {
                        bullet.transform.position = firePoint.position;
                        bullet.Owner = this;
                        bullet.gameObject.SetActive(true);
                    }
                }
            }
            else
            {
                transform.Translate(Physics.gravity * Time.deltaTime, Space.World);
            }

            prevInfo = currentInfo;
        }

        public void TakeDamage(CombatEvent combatEvent)
        {
            if (isDead) return;

            monsterStat.Hp -= combatEvent.Damage;
            if (monsterStat.Hp <= 0)
            {
                Die();
            }
        }

        public void TakeHeal(HealEvent healEvent)
        {
        }

        private void Die()
        {
            isDead = true;
            animator.SetTrigger(DeathTriggerHash);

            CombatSystem.Instance.UnregisterMonster(bodyCollider);
            bodyCollider.enabled = false;

            Destroy(gameObject, destroyDelay);
        }
    }
}
