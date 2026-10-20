using UnityEngine;
using UnityEngine.AI;

namespace Study3D.Pathfinding
{
    public class FollowAgent : MonoBehaviour
    {
        [Header("따라갈 대상")]
        [SerializeField] private NavMeshAgent targetAgent;

        private NavMeshAgent agent;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            if (targetAgent == null) return;

            if (targetAgent.isStopped == false)
            {
                agent.SetDestination(targetAgent.transform.position);
            }
        }
    }
}
