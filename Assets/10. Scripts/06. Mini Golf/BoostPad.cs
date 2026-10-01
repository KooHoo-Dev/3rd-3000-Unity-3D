using System;
using Study3D.MiniGolf;
using UnityEngine;

public enum PadType
{
    Boost,
    Jump
}

namespace MiniGolf.KSG
{
    public class BoostPad : MonoBehaviour
    {
        [SerializeField] private PadType padType;
        [SerializeField] private float boostSpeed;

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<GolfBall>(out var ball))
            {
                switch (padType)
                {
                    case PadType.Boost:
                        ball.rBody.linearVelocity = Vector3.zero;
                        ball.rBody.angularVelocity = Vector3.zero;
                        ball.rBody.AddForce(Vector3.forward * boostSpeed, ForceMode.VelocityChange);
                        break;
                    case PadType.Jump:
                        ball.rBody.AddForce(Vector3.up * boostSpeed, ForceMode.Impulse);
                        break;
                }
            }
        }
    }
}