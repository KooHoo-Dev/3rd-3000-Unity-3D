using Study3D.FpsGame;
using UnityEngine;
using UnityEngine.Serialization;

namespace Study3D.Mecanim
{
    public class BossAlfa : MonoBehaviour
    {
        private static readonly int BreathHash = Animator.StringToHash("Breath");
        private static readonly int AttackHash = Animator.StringToHash("Attack");

        [Header("이펙트")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private ParticleSystem breathEffect;

        [Header("공격 판정")]
        [FormerlySerializedAs("ScratchCollider")]
        [SerializeField] private Collider scratchCollider;
        [FormerlySerializedAs("BreathCollider")]
        [SerializeField] private Collider breathCollider;
        [FormerlySerializedAs("animEventReceiver")]
        [FormerlySerializedAs("AnimEventReciver")]
        [SerializeField] private AnimStateEventListener animEventListener;

        private Animator Animator { get; set; }

        private void Awake()
        {
            Animator = GetComponent<Animator>();

            scratchCollider.enabled = false;
            breathCollider.enabled = false;
        }
    }
}
