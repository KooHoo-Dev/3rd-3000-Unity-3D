using UnityEngine;

namespace Study3D.Mecanim
{
    public class FootIK : MonoBehaviour
    {
        [Header("지면 확인")]
        [SerializeField] private Transform footIKRayPoint;
        [SerializeField] private LayerMask layerMask;

        [Header("다리")]
        [SerializeField] private Transform leftKnee;
        [SerializeField] private Transform rightKnee;
        [SerializeField] private Transform leftFoot;
        [SerializeField] private Transform rightFoot;
        [SerializeField] private Transform hint;

        [Header("보정")]
        [SerializeField] private Vector3 footOffset;
        [SerializeField] private Vector3 bodyOffset;

        private Animator animator;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        private void Update()
        {
            Ray ray = new Ray(transform.position + Vector3.up, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, layerMask))
            {
                Vector3 point = transform.position;
                point.y = hit.point.y;
                transform.position = point;
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            UpdateFootIK(AvatarIKGoal.LeftFoot, leftFoot);
            UpdateFootIK(AvatarIKGoal.RightFoot, rightFoot);

            animator.SetIKHintPosition(AvatarIKHint.RightKnee, hint.position);
            animator.SetIKHintPositionWeight(AvatarIKHint.RightKnee, 1.0f);
        }

        private void UpdateFootIK(AvatarIKGoal goal, Transform footTransform)
        {
            Ray footRay = new Ray(footTransform.position + Vector3.up, Vector3.down);
            if (!Physics.Raycast(footRay, out RaycastHit hit, 100.0f, layerMask)) return;

            Vector3 footPosition = animator.GetIKPosition(goal);
            footPosition.y = hit.point.y;

            Vector3 dir = Vector3.ProjectOnPlane(transform.forward, hit.normal);
            Quaternion targetRotation = Quaternion.LookRotation(dir, hit.normal);
            animator.SetIKRotation(goal, targetRotation);
            animator.SetIKRotationWeight(goal, 1.0f);

            Vector3 convertedPoint = hit.normal * footOffset.magnitude;
            animator.SetIKPosition(goal, footPosition + convertedPoint);
            animator.SetIKPositionWeight(goal, 1.0f);
        }
    }
}
