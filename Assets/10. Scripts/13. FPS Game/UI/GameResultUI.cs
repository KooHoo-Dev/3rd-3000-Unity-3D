using TMPro;
using UnityEngine;
using Study3D.FpsGame.Player;

namespace Study3D.FpsGame
{
    public class GameResultUI : MonoBehaviour
    {
        [Header("결과 글자 (한글이므로 Noto Sans KR 폰트 에셋을 씀)")]
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private string victoryMessage = "승리";
        [SerializeField] private string defeatMessage = "패배";
        [SerializeField] private Color victoryColor = Color.yellow;
        [SerializeField] private Color defeatColor = Color.red;

        [Header("결과가 나오면 멈출 조작")]
        [SerializeField] private PlayerController playerController;

        private LocalPlayer localPlayer;
        private BossMonster boss;
        private bool isGameOver;

        private void Start()
        {
            resultText.enabled = false;

            localPlayer = Player.Player.LocalPlayer;
            boss = BossMonster.CurrentSceneBossMonster;
            if (localPlayer != null) localPlayer.OnDied += ShowDefeat;
            if (boss != null) boss.OnDied += ShowVictory;
        }

        private void OnDestroy()
        {
            if (localPlayer != null) localPlayer.OnDied -= ShowDefeat;
            if (boss != null) boss.OnDied -= ShowVictory;
        }

        private void ShowVictory()
        {
            ShowResult(victoryMessage, victoryColor);
        }

        private void ShowDefeat()
        {
            ShowResult(defeatMessage, defeatColor);
        }

        private void ShowResult(string message, Color color)
        {
            if (isGameOver) return;
            isGameOver = true;

            resultText.text = message;
            resultText.color = color;
            resultText.enabled = true;

            if (playerController != null) playerController.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
