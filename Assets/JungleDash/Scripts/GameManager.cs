// GameManager.cs – Manages score, coins, lives, game state, UI wiring.
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("HUD References")]
    public TextMeshProUGUI scoreTMP;
    public TextMeshProUGUI coinTMP;

    [Header("Game Over Panel")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverScoreTMP;
    public TextMeshProUGUI gameOverBestTMP;
    public Button restartButton;

    public bool IsRunning { get; private set; }

    int _score;
    int _coins;
    int _lastScore = -1;
    int _lastCoins = -1;
    const string BestKey = "BestScore";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
    }

    void Start()
    {
        IsRunning = true;
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (restartButton) restartButton.onClick.AddListener(RestartGame);
        PushHUD();
    }

    // Called by PlayerController when it earns points
    public void AddScore(int pts)
    {
        if (!IsRunning) return;
        _score += pts;
        PushHUD();
    }

    // Called by Coin pickup
    public void AddCoin(int count = 1)
    {
        if (!IsRunning) return;
        _coins += count;
        _score += count * 10;
        PushHUD();
    }

    public void TriggerGameOver()
    {
        if (!IsRunning) return;
        IsRunning = false;
        int best = PlayerPrefs.GetInt(BestKey, 0);
        if (_score > best) { best = _score; PlayerPrefs.SetInt(BestKey, best); PlayerPrefs.Save(); }
        if (gameOverPanel) gameOverPanel.SetActive(true);
        if (gameOverScoreTMP) gameOverScoreTMP.SetText("Score: {0}", _score);
        if (gameOverBestTMP)  gameOverBestTMP.SetText("Best: {0}", best);
    }

    void RestartGame() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

    void PushHUD()
    {
        if (_score != _lastScore)  { _lastScore = _score;  if (scoreTMP) scoreTMP.SetText("Score: {0:000000}", _score); }
        if (_coins != _lastCoins)  { _lastCoins = _coins;  if (coinTMP)  coinTMP.SetText("x{0}", _coins); }
    }
}
