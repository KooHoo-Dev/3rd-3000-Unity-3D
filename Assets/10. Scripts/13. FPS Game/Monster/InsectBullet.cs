using System;
using UnityEngine;

namespace Study3D.FpsGame
{
    public class InsectBullet : MonoBehaviour, IObjectPoolItem
    {
        [Serializable]
        public class BulletData
        {
            [SerializeField] private float speed;
            [SerializeField] private int damage;
            [SerializeField] private float deathTime;

            public float Speed => speed;
            public int Damage => damage;
            public float DeathTime => deathTime;
        }

        [Header("탄환")]
        [SerializeField] private BulletData bulletData;
        [Tooltip("플레이어 발밑(위치)에서 이 높이(m)를 노립니다. 기본값은 사람 눈높이")]
        [SerializeField] private float targetHeight = 1.7f;

        private float currentTime;

        public Insect Owner { get; set; }
        public string Key { get; set; }
        public GameObject GameObject => gameObject;

        private void OnEnable()
        {
            currentTime = 0;

            var targetPosition = Player.Player.LocalPlayer.transform.position + Vector3.up * targetHeight;
            var direction = (targetPosition - transform.position).normalized;
            transform.forward = direction;
        }

        private void Update()
        {
            transform.Translate(transform.forward * (Time.deltaTime * bulletData.Speed), Space.World);
            currentTime += Time.deltaTime;
            if (currentTime >= bulletData.DeathTime)
            {
                ReturnToPool();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other != Player.Player.LocalPlayer.MainCollider) return;

            CombatSystem.Instance.AddInGameEvent(new CombatEvent
            {
                Sender = Owner,
                Receiver = Player.Player.LocalPlayer,
                Damage = bulletData.Damage,
                HitPosition = other.ClosestPoint(transform.position),
            });

            ReturnToPool();
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
    }
}
