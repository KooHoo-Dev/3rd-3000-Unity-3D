using UnityEngine;
using UnityEngine.Serialization;

namespace Study3D.Mecanim
{
    public class Equipment : MonoBehaviour
    {
        [Header("IK 목표")]
        [SerializeField] private Transform rightHandTarget;
        [SerializeField] private Transform leftHandTarget;

        [FormerlySerializedAs("iKWeight")]
        [Range(0.0f, 1.0f)]
        [SerializeField] private float ikWeight;

        public Transform RightHandTarget => rightHandTarget;
        public Transform LeftHandTarget => leftHandTarget;
        public float IkWeight => ikWeight;
    }
}
