using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace AdventureGame
{
    public class CompleteGameHUD : MonoBehaviour
    {
        [Header("Player Status UI")]
        public Text healthText;
        public Slider staminaSlider;
        public Text gemsText;
        public Text scoreText;
        public Text timerText;
        public Text notificationText;

        [Header("Key Icons")]
        public Image rubyKeyIcon;
        public Image sapphireKeyIcon;
        public Image emeraldKeyIcon;

        [Header("Modals")]
        public GameObject pausePanel;
        public GameObject gameOverPanel;
        public GameObject victoryPanel;

        [Header("Victory UI Details")]
        public Text victoryTimeText;
        public Text victoryScoreText;
        public Text victoryStarsText;

        [Header("Buttons")]
        public Button pauseButton;
        public Button resumeButton;
        public Button restartButton;
        public Button gameOverRetryButton;
        public Button victoryPlayAgainButton;
        public Button[] mainMenuButtons;

        [Header("Mobile Sprint")]
        public Button sprintButton;

        private Coroutine m_NotificationCoroutine;

        private void Start()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (victoryPanel != null) victoryPanel.SetActive(false);

            // Hook up buttons
            if (pauseButton != null) pauseButton.onClick.AddListener(OnPauseClicked);
            if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);
            if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
            if (gameOverRetryButton != null) gameOverRetryButton.onClick.AddListener(OnRetryClicked);
            if (victoryPlayAgainButton != null) victoryPlayAgainButton.onClick.AddListener(OnRestartClicked);

            if (mainMenuButtons != null)
            {
                foreach (var b in mainMenuButtons)
                {
                    if (b != null) b.onClick.AddListener(OnMainMenuClicked);
                }
            }

            // Hook up mobile sprint
            if (sprintButton != null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null)
                {
                    var stamina = p.GetComponent<PlayerStamina>();
                    if (stamina != null)
                    {
                        // Toggle sprint state
                        sprintButton.onClick.AddListener(() =>
                        {
                            stamina.SetMobileSprint(!stamina.isSprinting);
                        });
                    }
                }
            }

            // Hook up GameManager events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.onGemsChanged.AddListener(UpdateGemsUI);
                GameManager.Instance.onScoreChanged.AddListener(UpdateScoreUI);
                GameManager.Instance.onKeysChanged.AddListener(UpdateKeysUI);
                GameManager.Instance.onNotificationMessage.AddListener(ShowNotification);
                GameManager.Instance.onGameWon.AddListener(ShowVictoryUI);
                GameManager.Instance.onGameOver.AddListener(ShowGameOverUI);

                UpdateGemsUI(GameManager.Instance.gemsCollected, GameManager.Instance.totalGemsInLevel);
                UpdateScoreUI(GameManager.Instance.currentScore);
                UpdateKeysUI(GameManager.Instance.hasRubyKey, GameManager.Instance.hasSapphireKey, GameManager.Instance.hasEmeraldKey);
            }

            // Hook up PlayerHealth events
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var health = player.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.onHealthChanged.AddListener(UpdateHealthUI);
                    UpdateHealthUI(health.currentHealth, health.maxHealth);
                }

                var stamina = player.GetComponent<PlayerStamina>();
                if (stamina != null)
                {
                    stamina.onStaminaChanged.AddListener(UpdateStaminaUI);
                    UpdateStaminaUI(stamina.currentStamina, stamina.maxStamina);
                }
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

            if (pausePanel != null && GameManager.Instance != null)
            {
                if (pausePanel.activeSelf != GameManager.Instance.isPaused)
                {
                    pausePanel.SetActive(GameManager.Instance.isPaused);
                }
            }
        }

        private void UpdateHealthUI(int current, int max)
        {
            if (healthText != null)
            {
                string hearts = "";
                for (int i = 0; i < max; i++)
                {
                    hearts += (i < current) ? "❤️ " : "🖤 ";
                }
                healthText.text = hearts.Trim();
            }
        }

        private void UpdateStaminaUI(float current, float max)
        {
            if (staminaSlider != null)
            {
                staminaSlider.value = max > 0 ? (current / max) : 0f;
            }
        }

        private void UpdateGemsUI(int collected, int total)
        {
            if (gemsText != null)
            {
                gemsText.text = string.Format("💎 {0} / {1}", collected, total);
            }
        }

        private void UpdateScoreUI(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = string.Format("Score: {0}", score);
            }
        }

        private void UpdateKeysUI(bool ruby, bool sapphire, bool emerald)
        {
            if (rubyKeyIcon != null) rubyKeyIcon.color = ruby ? Color.red : new Color(0.3f, 0.3f, 0.3f, 0.4f);
            if (sapphireKeyIcon != null) sapphireKeyIcon.color = sapphire ? Color.cyan : new Color(0.3f, 0.3f, 0.3f, 0.4f);
            if (emeraldKeyIcon != null) emeraldKeyIcon.color = emerald ? Color.green : new Color(0.3f, 0.3f, 0.3f, 0.4f);
        }

        public void ShowNotification(string message)
        {
            if (notificationText == null) return;

            if (m_NotificationCoroutine != null) StopCoroutine(m_NotificationCoroutine);
            m_NotificationCoroutine = StartCoroutine(NotificationRoutine(message));
        }

        private IEnumerator NotificationRoutine(string message)
        {
            notificationText.text = message;
            notificationText.gameObject.SetActive(true);
            yield return new WaitForSeconds(3.0f);
            notificationText.gameObject.SetActive(false);
        }

        private void ShowVictoryUI()
        {
            if (victoryPanel != null) victoryPanel.SetActive(true);

            if (GameManager.Instance != null)
            {
                float t = GameManager.Instance.elapsedTime;
                int minutes = Mathf.FloorToInt(t / 60f);
                int seconds = Mathf.FloorToInt(t % 60f);

                if (victoryTimeText != null) victoryTimeText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);
                if (victoryScoreText != null) victoryScoreText.text = string.Format("Score: {0}", GameManager.Instance.currentScore);

                int stars = PlayerPrefs.GetInt("IslandStars", 1);
                string starIcons = stars == 3 ? "⭐⭐⭐" : (stars == 2 ? "⭐⭐" : "⭐");
                if (victoryStarsText != null) victoryStarsText.text = starIcons;
            }
        }

        private void ShowGameOverUI()
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
        }

        private void OnPauseClicked()
        {
            if (GameManager.Instance != null) GameManager.Instance.TogglePause();
        }

        private void OnResumeClicked()
        {
            if (GameManager.Instance != null) GameManager.Instance.TogglePause();
        }

        private void OnRestartClicked()
        {
            if (GameManager.Instance != null) GameManager.Instance.RestartGame();
        }

        private void OnRetryClicked()
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.isGameOver = false;
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null)
                {
                    var health = p.GetComponent<PlayerHealth>();
                    if (health != null) health.ResetHealth();
                }
                GameManager.Instance.RespawnPlayer();
            }
        }

        private void OnMainMenuClicked()
        {
            if (GameManager.Instance != null) GameManager.Instance.LoadMainMenu();
        }
    }
}
