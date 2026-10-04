using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace AdventureGame
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Buttons")]
        public Button playButton;
        public Button howToPlayButton;
        public Button settingsButton;
        public Button quitButton;

        [Header("Modals")]
        public GameObject howToPlayPanel;
        public GameObject settingsPanel;
        public Button closeHowToPlayBtn;
        public Button closeSettingsBtn;

        [Header("Stats Display")]
        public Text statsText;

        [Header("Settings UI")]
        public Slider volumeSlider;
        public Toggle highFpsToggle;

        private void Start()
        {
            if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            if (playButton != null) playButton.onClick.AddListener(StartGame);
            if (howToPlayButton != null) howToPlayButton.onClick.AddListener(() => howToPlayPanel.SetActive(true));
            if (settingsButton != null) settingsButton.onClick.AddListener(() => settingsPanel.SetActive(true));
            if (quitButton != null) quitButton.onClick.AddListener(QuitGame);

            if (closeHowToPlayBtn != null) closeHowToPlayBtn.onClick.AddListener(() => howToPlayPanel.SetActive(false));
            if (closeSettingsBtn != null) closeSettingsBtn.onClick.AddListener(() => settingsPanel.SetActive(false));

            // Load high score & stars
            int bestScore = PlayerPrefs.GetInt("BestScore", 0);
            int stars = PlayerPrefs.GetInt("IslandStars", 0);
            if (statsText != null)
            {
                string starDisplay = stars > 0 ? new string('⭐', stars) : "None yet";
                statsText.text = $"Best Score: {bestScore} | Mastery: {starDisplay}";
            }

            // Settings
            if (volumeSlider != null)
            {
                float vol = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
                volumeSlider.value = vol;
                AudioListener.volume = vol;
                volumeSlider.onValueChanged.AddListener((v) =>
                {
                    AudioListener.volume = v;
                    PlayerPrefs.SetFloat("MasterVolume", v);
                });
            }

            if (highFpsToggle != null)
            {
                int targetFps = PlayerPrefs.GetInt("TargetFPS", 60);
                Application.targetFrameRate = targetFps;
                highFpsToggle.isOn = (targetFps == 60);
                highFpsToggle.onValueChanged.AddListener((isOn) =>
                {
                    int fps = isOn ? 60 : 30;
                    Application.targetFrameRate = fps;
                    PlayerPrefs.SetInt("TargetFPS", fps);
                });
            }
        }

        public void StartGame()
        {
            SceneManager.LoadScene("AdventureIsland");
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
