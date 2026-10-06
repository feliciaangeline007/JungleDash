// GameManager.cs
// Central game state: score, coins, distance, game-over, UI wiring.
// No per-frame allocations.
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Singleton
    // ─────────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    // ─────────────────────────────────────────────
    //  Inspector references
    // ─────────────────────────────────────────────
    [Header("HUD References")]
    [Tooltip("TextMeshProUGUI showing the running score.")]
    public TextMeshProUGUI scoreTMP;
    [Tooltip("TextMeshProUGUI showing the coin count.")]
    public TextMeshProUGUI coinTMP;

    [Header("Game Over Panel")]
    [Tooltip("Root GameObject of the Game Over panel.")]
    public GameObject gameOverPanel;
    [Tooltip("TextMeshProUGUI showing the final score.")]
    public TextMeshProUGUI gameOverScoreTMP;
    [Tooltip("TextMeshProUGUI showing the best score.")]
    public TextMeshProUGUI gameOverBestTMP;
    [Tooltip("Restart button.")]
    public Button restartButton;

    [Header("Player")]
    [Tooltip("Transform of the player (used by Magnet etc.).")]
    public Transform PlayerTransform;

    // ─────────────────────────────────────────────
    //  Runtime state
    // ─────────────────────────────────────────────
    public bool IsRunning { get; private set; }

    private float _score;
    private int   _displayScore;   // last integer score pushed to TMP
    private int   _coins;
    private int   _displayCoins;

    private const string BestScoreKey = "BestScore";

    // ─────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount  = 0;
    }

    private void Start()
    {
        IsRunning = true;
        _score    = 0f;
        _coins    = 0;
        _displayScore = -1; // force first update
        _displayCoins = -1;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);

        RefreshScoreHUD();
        RefreshCoinHUD();
    }

    // ─────────────────────────────────────────────
    //  Public API called by PlayerRunner / Collectible
    // ─────────────────────────────────────────────

    /// <summary>Add distance-based score (called every frame by PlayerRunner).</summary>
    public void AddDistance(float metres)
    {
        if (!IsRunning) return;
        float mult = PowerUpManager.Instance != null
            ? PowerUpManager.Instance.ScoreMultiplier : 1f;
        _score += metres * mult;
        RefreshScoreHUD();
    }

    /// <summary>Add pickup points and coins.</summary>
    public void AddPickup(int points, int coinCount)
    {
        if (!IsRunning) return;
        float mult = PowerUpManager.Instance != null
            ? PowerUpManager.Instance.ScoreMultiplier : 1f;
        _score += points * mult;
        _coins += coinCount;
        RefreshScoreHUD();
        RefreshCoinHUD();
    }

    /// <summary>Trigger game over.</summary>
    public void TriggerGameOver()
    {
        if (!IsRunning) return;
        IsRunning = false;

        int finalScore = Mathf.RoundToInt(_score);
        int best       = PlayerPrefs.GetInt(BestScoreKey, 0);
        if (finalScore > best)
        {
            best = finalScore;
            PlayerPrefs.SetInt(BestScoreKey, best);
            PlayerPrefs.Save();
        }

        if (gameOverPanel    != null) gameOverPanel.SetActive(true);
        if (gameOverScoreTMP != null) gameOverScoreTMP.SetText("{0}", finalScore);
        if (gameOverBestTMP  != null) gameOverBestTMP.SetText("{0}", best);
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────
    private void RefreshScoreHUD()
    {
        int s = Mathf.RoundToInt(_score);
        if (s == _displayScore) return;
        _displayScore = s;
        if (scoreTMP != null) scoreTMP.SetText("{0}", s);
    }

    private void RefreshCoinHUD()
    {
        if (_coins == _displayCoins) return;
        _displayCoins = _coins;
        if (coinTMP != null) coinTMP.SetText("{0}", _coins);
    }

    private void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
