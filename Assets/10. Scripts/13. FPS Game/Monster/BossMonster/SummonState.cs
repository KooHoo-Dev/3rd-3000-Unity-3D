using System.Collections;
using UnityEngine;

namespace Study3D.FpsGame
{
    public class SummonState : BossState
    {
        [Header("소환")]
        [SerializeField] private Insect minionPrefab;
        [Tooltip("플레이어를 중심으로 벌레가 떨어질 원의 반지름(m)")]
        [SerializeField] private float spawnRadius = 3.0f;
        [Tooltip("소환 동작의 이 진행률(normalizedTime)에 벌레를 만듭니다")]
        [SerializeField] [Range(0.0f, 1.0f)] private float spawnTiming;

        [Header("소환 수와 높이")]
        [SerializeField] private int minMinionCount = 2;
        [SerializeField] private int maxMinionCount = 3;
        [Tooltip("플레이어 발밑에서 이 높이(m) 사이의 무작위 위치에서 떨어뜨립니다")]
        [SerializeField] private float minDropHeight = 1.0f;
        [SerializeField] private float maxDropHeight = 4.0f;

        public override StateName Name => StateName.SummonState;
        public override bool IsAttack => true;
        public override string AnimatorTrigger => "Summon";

        public override void Enter()
        {
            StartCoroutine(SpawnCoroutine());
        }

        private IEnumerator SpawnCoroutine()
        {
            while (animator.GetCurrentAnimatorStateInfo(0).IsName(animatorStateName) == false)
            {
                yield return null;
            }

            while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < spawnTiming)
            {
                yield return null;
            }

            int minionCount = Random.Range(minMinionCount, maxMinionCount + 1);
            for (int i = 0; i < minionCount; i++)
            {
                Vector3 spawnPosition = Player.Player.LocalPlayer.transform.position;
                Vector2 circlePoint = Random.insideUnitCircle;

                spawnPosition.x += spawnRadius * circlePoint.x;
                spawnPosition.y += Random.Range(minDropHeight, maxDropHeight);
                spawnPosition.z += spawnRadius * circlePoint.y;

                Instantiate(minionPrefab, spawnPosition, Quaternion.identity);
            }
        }
    }
}
