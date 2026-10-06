using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Study3D.AirPlaneGame
{
    public partial class AirPlaneGameManager : MonoBehaviour
    {
        // 비행기의 주행기록을 남겨서 특정한 시점으로 복원하는 기능을 만들어 봅시다.
        private struct FlyLog
        {
            public Vector3 WorldPosition;
            public Quaternion WorldRotation;
        }
        
        private Queue<FlyLog> flyLogQueue = new Queue<FlyLog>();
        
        private void InitFlyLog()
        {
            Debug.Log("InitFlyLog");
            LoopRecord().Forget();
        }

        private async UniTaskVoid LoopRecord()
        {
            while (gameObject.activeInHierarchy)
            {
                await UniTask.WaitForSeconds(0.1f);          // 0.1초 기다렸다가
                RecordFlyLog();                                     // 기록해준다
                if (flyLogQueue.Count > 30) flyLogQueue.Dequeue();  // 30개를 초과하면 빼준다 
            }
        }

        private void RecordFlyLog()
        {
            FlyLog flyLog = new()
            {
                WorldPosition = AirPlaneController.LocalAirPlane.transform.position,
                WorldRotation = AirPlaneController.LocalAirPlane.transform.rotation
            };
            
            flyLogQueue.Enqueue(flyLog);
        }
    }
}