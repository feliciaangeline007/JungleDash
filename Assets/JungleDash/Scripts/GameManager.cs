// GameManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("HUD")]
    public TextMeshProUGUI scoreTMP;
    public TextMeshProUGUI coinTMP;

    [Header("Game Over")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverScoreTMP;
    public TextMeshProUGUI gameOverBestTMP;
    public Button restartButton;

    [Header("Player Reference")]
    public Transform PlayerTransform;

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
        if (restartButton) restartButton.onClick.AddListener(
            () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
        PushHUD();
    }

    public void AddScore(int pts)
    {
        if (!IsRunning) return;
        _score += pts;
        PushHUD();
    }

    public void AddCoin(int n = 1)
    {
        if (!IsRunning) return;
        _coins += n;
        _score += n * 10;
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

    void PushHUD()
    {
        if (_score != _lastScore) { _lastScore = _score; if (scoreTMP) scoreTMP.SetText("Score: {0:000000}", _score); }
        if (_coins != _lastCoins) { _lastCoins = _coins; if (coinTMP)  coinTMP.SetText("x{0}", _coins); }
    }
}
