using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MiniGolf.KSG
{
    public class LaserBar : MonoBehaviour
    {
        [SerializeField] private GameObject[] lasers;
        [SerializeField] private float rotateSpeed;
        [SerializeField] private float appearInterval;
        
        private void Start()
        {
            StartLaserLoopAsync().Forget();
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
        }

        private async UniTaskVoid StartLaserLoopAsync()
        {
            var token = this.GetCancellationTokenOnDestroy();

            while (true)
            {
                for (int i = 0; i < lasers.Length; i++)
                {
                    lasers[i].SetActive(true);
                }
                await UniTask.Delay(TimeSpan.FromSeconds(appearInterval), cancellationToken: token);

                for (int i = 0; i < lasers.Length; i++)
                {
                    lasers[i].SetActive(false);
                }
                await UniTask.Delay(TimeSpan.FromSeconds(appearInterval), cancellationToken: token);
            }
        }
    }
}