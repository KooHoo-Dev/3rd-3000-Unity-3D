using System;
using System.Security;
using UnityEngine;

namespace Study3D.FpsGame
{
    public class ParticleEffect : MonoBehaviour, IObjectPoolItem
    {
        [SerializeField] private ParticleSystem particle;

        public string Key { get; set; }
        public GameObject GameObject => gameObject;
        
        private void Update()
        {
            // 파티클이 집(ObjectPool)으로 돌아가는 시기는
            // 재생이 끝났을때 입니다.
            if(particle.isPlaying == false) ReturnToPool();
        }

        // 인터페이스에 ReturnToPool이 있는 이유는
        // 솔직히 까먹지말라고 정의해 놓은겁니다.
        public void ReturnToPool()
        {
            ObjectPoolManager.Instance.ReturnToPool(this);
        }
        
        // particle을 제어하는 함수들.
        private void StudyParticle()
        {
            // ParticleSystem이 부착되어있는 GameObject가 활성화 되면
            // 바로 재생을 하는지 여부 입니다.
            bool playOnAwake = particle.main.playOnAwake;
            
            // 재생이 종료되면, 다시 시작할지 여부 입니다.
            // 켜져있으면 반복 재생 됩니다.
            bool isLoop = particle.main.loop;
            
            particle.Play(); // 파티클을 재생시키는 함수입니다.
            
            // 파티클이 현재 재생중인지 알 수 있는 프로퍼티 입니다.
            // 보통 isPlaying이 false가 되면 뭔가를 처리하는게 많습니다.
            bool isPlaying = particle.isPlaying;
           
            particle.Pause(); // 파티클 재생을 일시 정지할때 사용합니다.
            
            // 파티클이 현재 일시정지중인지 알 수 있는 프로퍼티 입니다.
            bool isPaused = particle.isPaused;
            
            particle.Stop(); // 파티클 재생을 멈추게 하는 함수 입니다.
        }

    }
}