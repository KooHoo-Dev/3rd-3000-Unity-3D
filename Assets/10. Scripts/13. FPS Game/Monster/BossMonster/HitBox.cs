using UnityEngine;

namespace Study3D.FpsGame
{
    public class HitBox : MonoBehaviour
    {
        [Header("공격")]
        [Tooltip("켜질 때마다 플레이어를 한 번만 맞힐 때의 데미지")]
        [SerializeField] private int damage = 10;

        private BossMonster owner;
        private bool hasHit;
        private Transform followPoint;
        private Transform followDirection;

        private void Awake()
        {
            owner = GetComponentInParent<BossMonster>();
        }

        private void OnEnable()
        {
            hasHit = false;
        }

        public void Follow(Transform point, Transform direction)
        {
            followPoint = point;
            followDirection = direction;
        }

        private void Update()
        {
            if (followPoint == null) return;

            transform.position = followPoint.position;
            transform.forward = followDirection.forward;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasHit) return;

            var player = Player.Player.LocalPlayer;
            if (player == null || other != player.MainCollider) return;

            hasHit = true;

            CombatSystem.Instance.AddInGameEvent(new CombatEvent
            {
                Sender = owner,
                Receiver = player,
                Damage = damage,
                HitPosition = other.ClosestPoint(transform.position),
                Collider = other,
            });
        }
    }
}
