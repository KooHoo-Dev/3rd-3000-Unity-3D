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
            
            Color damageTextColor = Color.orange;
            
            var part = BossMonster.CurrentSceneBossMonster.GetPart(combatEvent.Collider);
            if (part != BossMonster.Parts.Unknown) damageTextColor = GetBossPartColor(part);
            else damageTextColor = GetDamageColor(combatEvent.DamageType);
            
            dmgText.Set(
                combatEvent.HitPosition, 
                $"{combatEvent.Damage}",
                damageTextDuration,
                damageTextColor);
            
            dmgText.gameObject.SetActive(true);
        }

        // 피해 종류별로 데미지 텍스트 색을 다르게 보여줍니다.
        private static Color GetDamageColor(DamageType damageType)
        {
            switch (damageType)
            {
                case DamageType.Pierce:   return new Color(0.4f, 0.8f, 1.0f);  // 하늘색
                case DamageType.Ricochet: return new Color(1.0f, 0.85f, 0.2f); // 노란색
                case DamageType.Electric: return new Color(0.7f, 0.5f, 1.0f);  // 보라색
                default:                  return Color.red;
            }
        }

        private static Color GetBossPartColor(BossMonster.Parts part)
        {
            return part switch
            {
                BossMonster.Parts.Unknown => Color.orange,
                BossMonster.Parts.Spine => Color.yellow,
                BossMonster.Parts.Head => Color.green,
                BossMonster.Parts.LeftArm => Color.blue,
                BossMonster.Parts.RightArm => Color.blue,
                _ => Color.orange
            };
        }
    }
}