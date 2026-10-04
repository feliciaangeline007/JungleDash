using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace AdventureGame
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Player & Checkpoints")]
        public Transform player;
        public Vector3 initialSpawnPoint = new Vector3(0, 1.5f, 0);
        private Vector3 m_CurrentCheckpoint;
        private Quaternion m_CurrentCheckpointRotation;

        [Header("Keys & Quests")]
        public bool hasRubyKey = false;
        public bool hasSapphireKey = false;
        public bool hasEmeraldKey = false;
        public ShrinePortal shrinePortal;

        [Header("Collectibles & Score")]
        public int totalGemsInLevel = 10;
        public int gemsCollected = 0;
        public int currentScore = 0;

        [Header("Game State")]
        public bool isGameOver = false;
        public bool isGameWon = false;
        public bool isPaused = false;
        public float elapsedTime = 0f;

        [Header("Events")]
        public UnityEvent<int, int> onGemsChanged;
        public UnityEvent<int> onScoreChanged;
        public UnityEvent<bool, bool, bool> onKeysChanged;
        public UnityEvent<string> onNotificationMessage;
        public UnityEvent onGameWon;
        public UnityEvent onGameOver;
        public UnityEvent onPlayerRespawned;

        private Rigidbody m_PlayerRigidbody;
        private CharacterController m_PlayerCharController;
        private PlayerHealth m_PlayerHealth;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            m_CurrentCheckpoint = initialSpawnPoint;
            m_CurrentCheckpointRotation = Quaternion.identity;
            Time.timeScale = 1.0f;
        }

        private void Start()
        {
            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
            }

            if (player != null)
            {
                m_PlayerRigidbody = player.GetComponent<Rigidbody>();
                m_PlayerCharController = player.GetComponent<CharacterController>();
                m_PlayerHealth = player.GetComponent<PlayerHealth>();
                m_CurrentCheckpoint = player.position;
                m_CurrentCheckpointRotation = player.rotation;
            }

            var gems = Object.FindObjectsByType<CollectibleGem>(FindObjectsSortMode.None);
            if (gems != null && gems.Length > 0)
            {
                totalGemsInLevel = gems.Length;
            }

            if (shrinePortal == null)
            {
                shrinePortal = Object.FindAnyObjectByType<ShrinePortal>();
            }

            onGemsChanged?.Invoke(gemsCollected, totalGemsInLevel);
            onScoreChanged?.Invoke(currentScore);
            onKeysChanged?.Invoke(hasRubyKey, hasSapphireKey, hasEmeraldKey);
        }

        private void Update()
        {
            if (!isGameOver && !isGameWon && !isPaused)
            {
                elapsedTime += Time.deltaTime;
            }

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                TogglePause();
            }
        }

        public void AddGem(int points)
        {
            gemsCollected++;
            currentScore += points;

            onGemsChanged?.Invoke(gemsCollected, totalGemsInLevel);
            onScoreChanged?.Invoke(currentScore);
        }

        public void AddScore(int points)
        {
            currentScore += points;
            onScoreChanged?.Invoke(currentScore);
        }

        public void CollectKey(KeyType type)
        {
            string keyName = "";
            switch (type)
            {
                case KeyType.Ruby:
                    hasRubyKey = true;
                    keyName = "Ruby Key";
                    break;
                case KeyType.Sapphire:
                    hasSapphireKey = true;
                    keyName = "Sapphire Key";
                    break;
                case KeyType.Emerald:
                    hasEmeraldKey = true;
                    keyName = "Emerald Key";
                    break;
            }

            ShowMessage($"Acquired {keyName}!");
            onKeysChanged?.Invoke(hasRubyKey, hasSapphireKey, hasEmeraldKey);

            if (shrinePortal != null)
            {
                shrinePortal.CheckAndUnlock(hasRubyKey, hasSapphireKey, hasEmeraldKey);
            }
        }

        public void ShowMessage(string msg)
        {
            onNotificationMessage?.Invoke(msg);
        }

        public void SetCheckpoint(Vector3 position, Quaternion rotation)
        {
            m_CurrentCheckpoint = position;
            m_CurrentCheckpointRotation = rotation;
            ShowMessage("Checkpoint Saved!");
        }

        public void RespawnPlayer()
        {
            if (player == null) return;

            if (m_PlayerRigidbody != null)
            {
                m_PlayerRigidbody.linearVelocity = Vector3.zero;
                m_PlayerRigidbody.angularVelocity = Vector3.zero;
            }

            if (m_PlayerCharController != null)
            {
                m_PlayerCharController.enabled = false;
            }

            player.position = m_CurrentCheckpoint + Vector3.up * 0.5f;
            player.rotation = m_CurrentCheckpointRotation;

            if (m_PlayerCharController != null)
            {
                m_PlayerCharController.enabled = true;
            }

            if (m_PlayerHealth != null)
            {
                m_PlayerHealth.TakeDamage(1); // Minor penalty on falling
            }

            onPlayerRespawned?.Invoke();
        }

        public void OnPlayerDied()
        {
            isGameOver = true;
            onGameOver?.Invoke();
        }

        public void TriggerVictory()
        {
            if (isGameWon) return;
            isGameWon = true;

            // Calculate stars (1 to 3)
            int stars = 1;
            if (gemsCollected >= (totalGemsInLevel * 0.7f)) stars = 2;
            if (gemsCollected >= totalGemsInLevel && elapsedTime <= 180f) stars = 3;

            // Save high score and stars in PlayerPrefs
            int bestScore = PlayerPrefs.GetInt("BestScore", 0);
            if (currentScore > bestScore)
            {
                PlayerPrefs.SetInt("BestScore", currentScore);
            }

            int currentStars = PlayerPrefs.GetInt("IslandStars", 0);
            if (stars > currentStars)
            {
                PlayerPrefs.SetInt("IslandStars", stars);
            }
            PlayerPrefs.Save();

            onGameWon?.Invoke();
        }

        public void TogglePause()
        {
            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0f : 1.0f;
        }

        public void RestartGame()
        {
            Time.timeScale = 1.0f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void LoadMainMenu()
        {
            Time.timeScale = 1.0f;
            SceneManager.LoadScene(0); // Scene 0 is MainMenu
        }
    }
}
