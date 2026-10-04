using UnityEngine;
using UnityEngine.UI;

namespace AdventureGame
{
    public class AdventureHUD : MonoBehaviour
    {
        [Header("UI References")]
        public Text gemsText;
        public Text scoreText;
        public Text timerText;
        public GameObject victoryPanel;
        public Text victoryTimeText;
        public Button restartButton;

        private void Start()
        {
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(false);
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.onGemsChanged.AddListener(UpdateGemsUI);
                GameManager.Instance.onScoreChanged.AddListener(UpdateScoreUI);
                GameManager.Instance.onGameWon.AddListener(ShowVictoryUI);

                UpdateGemsUI(GameManager.Instance.gemsCollected, GameManager.Instance.totalGemsInLevel);
                UpdateScoreUI(GameManager.Instance.currentScore);
            }
        }

        private void Update()
        {
            if (timerText != null && GameManager.Instance != null && !GameManager.Instance.isGameWon)
            {
                float t = GameManager.Instance.elapsedTime;
                int minutes = Mathf.FloorToInt(t / 60f);
                int seconds = Mathf.FloorToInt(t % 60f);
                timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }
        }

        private void UpdateGemsUI(int collected, int total)
        {
            if (gemsText != null)
            {
                gemsText.text = string.Format("Gems: {0} / {1}", collected, total);
            }
        }

        private void UpdateScoreUI(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = string.Format("Score: {0}", score);
            }
        }

        private void ShowVictoryUI()
        {
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
            }

            if (victoryTimeText != null && GameManager.Instance != null)
            {
                float t = GameManager.Instance.elapsedTime;
                int minutes = Mathf.FloorToInt(t / 60f);
                int seconds = Mathf.FloorToInt(t % 60f);
                victoryTimeText.text = string.Format("Time: {0:00}:{1:00} | Score: {2}", minutes, seconds, GameManager.Instance.currentScore);
            }
        }

        private void OnRestartClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
        }
    }
}
