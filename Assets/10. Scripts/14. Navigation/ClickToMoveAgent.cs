using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace Study3D.Pathfinding
{
    public class ClickToMoveAgent : MonoBehaviour
    {
        [Header("목표")]
        [SerializeField] private Transform pointerGuide;
        [SerializeField] private Transform[] goal;

        private NavMeshAgent agent;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            HandleGoalKeys();
            HandleClick();
        }

        private void HandleGoalKeys()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.digit1Key.wasPressedThisFrame) MoveToGoal(0);
            if (keyboard.digit2Key.wasPressedThisFrame) MoveToGoal(1);
            if (keyboard.digit3Key.wasPressedThisFrame) MoveToGoal(2);
            if (keyboard.digit4Key.wasPressedThisFrame) MoveToGoal(3);
        }

        private void HandleClick()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            if (mouse.leftButton.wasPressedThisFrame == false) return;

            Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hitInfo, 1000f))
            {
                pointerGuide.position = hitInfo.point;
                agent.SetDestination(hitInfo.point);
            }
        }

        private void MoveToGoal(int index)
        {
            if (index < goal.Length && goal[index] != null)
                agent.SetDestination(goal[index].position);
        }
    }
}
