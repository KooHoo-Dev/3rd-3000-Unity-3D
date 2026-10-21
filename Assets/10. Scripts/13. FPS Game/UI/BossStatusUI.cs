using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Study3D.FpsGame
{
    public class BossStatusUI : MonoBehaviour
    {
        [Header("채울 이미지 (Image Type: Filled)")]
        [SerializeField] private Image fillImage;

        [Header("보스 이름")]
        [SerializeField] private TMP_Text nameText;

        private BossMonster boss;

        private void Start()
        {
            boss = BossMonster.CurrentSceneBossMonster;
            nameText.text = boss.name;
        }

        private void Update()
        {
            fillImage.fillAmount = (float)boss.Health.HP / boss.Health.MaxHP;
        }
    }
}
