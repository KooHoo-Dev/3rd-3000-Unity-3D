using UnityEngine;

namespace RageRunGames.EasyPaperPlaneController
{
    public class PaperPlaneEffectHandler : MonoBehaviour
    {
        [SerializeField] private ParticleSystem speedlinesEffect;
        [SerializeField] private float speedlinesEmissionBoostRate = 150f;
        
        private BasePlaneController _planeController;

        private void Awake()
        {
            _planeController = GetComponent<BasePlaneController>();
        }

        private void OnEnable()
        {
            _planeController.OnCrash += HandleCrash;
        }

        private void OnDisable()
        {
            _planeController.OnCrash -= HandleCrash;
        }

        private void HandleCrash()
        {
            var module = speedlinesEffect.emission;
            module.rateOverTime = 0f;
        }

        private void Update()
        {
            var module = speedlinesEffect.emission;
            module.rateOverTime = _planeController.IsBoosting ? Mathf.Lerp(module.rateOverTime.constant, speedlinesEmissionBoostRate, Time.deltaTime * 10f): 
                Mathf.Lerp(module.rateOverTime.constant, 0f, Time.deltaTime * 10f);
        }
    }
}