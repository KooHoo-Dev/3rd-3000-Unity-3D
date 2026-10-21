using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Study3D.FpsGame
{
    public class Grenade : MonoBehaviour, IObjectPoolItem
    {
        [Header("참조")]
        [SerializeField] private Rigidbody rigidBody;
        [SerializeField] private ParticleSystem explosion;
        [FormerlySerializedAs("collider")]
        [SerializeField] private Collider bodyCollider;

        [Header("폭발")]
        [SerializeField] private float explosionRadius = 3.0f;
        [Tooltip("아무것도 맞지 않았을 때 이 시간(초)이 지나면 사라집니다")]
        [SerializeField] private float maxLifetime = 5.0f;

        private readonly List<IDamageable> damagedMonsters = new List<IDamageable>();
        private bool isExploded;
        private float lifetime;

        public int Damage { get; set; }
        public string Key { get; set; }
        public GameObject GameObject => gameObject;

        private void OnEnable()
        {
            isExploded = false;
            lifetime = 0.0f;
            damagedMonsters.Clear();

            bodyCollider.enabled = true;
            rigidBody.isKinematic = false;
            rigidBody.linearVelocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;
            explosion.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (isExploded && explosion.isPlaying == false)
            {
                ReturnToPool();
                return;
            }

            lifetime += Time.deltaTime;
            if (isExploded == false && lifetime > maxLifetime)
            {
                ReturnToPool();
            }
        }

        public void ReturnToPool()
        {
            if (string.IsNullOrEmpty(Key))
            {
                Destroy(gameObject);
                return;
            }

            ObjectPoolManager.Instance.ReturnToPool(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isExploded) return;

            if (CombatSystem.Instance.GetMonsterOrNull(other) != null)
            {
                Explode(other);
                return;
            }

            if (other.isTrigger) return;
            if (other.transform.IsChildOf(Player.Player.LocalPlayer.transform)) return;

            Explode(other);
        }

        public void LaunchGrenade(float force)
        {
            gameObject.SetActive(true);
            rigidBody.AddForce(-transform.up * (force), ForceMode.VelocityChange);
        }

        private void Explode(Collider directHit)
        {
            isExploded = true;
            bodyCollider.enabled = false;
            rigidBody.isKinematic = true;
            explosion.gameObject.SetActive(true);

            TryDamage(directHit);

            Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius, CombatSystem.Instance.EnemyMask);
            for (int i = 0; i < colliders.Length; i++)
            {
                TryDamage(colliders[i]);
            }
        }

        private void TryDamage(Collider target)
        {
            var monster = CombatSystem.Instance.GetMonsterOrNull(target);
            if (monster == null || damagedMonsters.Contains(monster)) return;

            damagedMonsters.Add(monster);

            CombatEvent combatEvent = new CombatEvent();
            combatEvent.Sender = Player.Player.LocalPlayer;
            combatEvent.Receiver = monster;
            combatEvent.Damage = Damage;
            combatEvent.HitPosition = target.ClosestPoint(transform.position);
            combatEvent.Collider = target;

            CombatSystem.Instance.AddInGameEvent(combatEvent);
        }
    }
}
