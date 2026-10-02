using System;
using Study3D.MiniGolf;
using UnityEngine;

namespace MiniGolf.KSG
{
    public class Bomb : MonoBehaviour
    {
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Ball"))
            {
                Boom(collision.gameObject);
            }
        }

        private void Boom(GameObject ball)
        {
            if (ball.TryGetComponent<GolfBall>(out var golfBall))
            {
                
            }
        }
    }
}