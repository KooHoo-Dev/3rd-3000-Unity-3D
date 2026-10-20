using System;
using Study3D.FpsGame.UI;
using UnityEngine;

namespace Study3D.FpsGame.Feedback
{
    // Hit이라고 하는 Effect의 모든 피드백들을 재생시키는 하나의 컨트롤러 입니다.
    // - 특정 코드에서 이것저것 참조하여 재생시키면 종속성이 늘어나서 새로 추가할때 힘듭니다
    // - 이런 느낌의 곁다리 코드들은 한곳에 집약적으로 모아두는게 좋습니다.
    
    public class HitEffectController : MonoBehaviour
    {
        [Header("Settings")] 
        [SerializeField] private string bloodPoolKey = "Blood";
        [SerializeField] private string damageTextPoolKey = "DamageText";
        [SerializeField] private float damageTextDuration = 0.5f;
        
        private void Start()
        {
            CombatSystem.Instance.OnCombatEvent += OnHit;
        }

        private void OnDestroy()
        {
            if(CombatSystem.Instance != null) 
                CombatSystem.Instance.OnCombatEvent -= OnHit;
        }

        private void OnHit(CombatEvent combatEvent)
        {
            var blood = ObjectPoolManager.Instance.GetObjectOrNull(bloodPoolKey);
            blood.GameObject.transform.position = combatEvent.HitPosition;
            blood.GameObject.SetActive(true);
            
            FloatingText dmgText = 
                ObjectPoolManager.Instance.GetObjectOrNull(damageTextPoolKey) as FloatingText;
            
            dmgText.Set(
                combatEvent.HitPosition, 
                $"{combatEvent.Damage}",
                damageTextDuration,
                Color.red);
            
            dmgText.gameObject.SetActive(true);
        }
    }
}