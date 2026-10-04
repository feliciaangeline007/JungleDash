using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace AdventureGame
{
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        public int maxHealth = 3;
        public int currentHealth = 3;
        public float invulnerabilityDuration = 1.5f;

        [Header("Audio")]
        public AudioClip hurtSound;
        public AudioClip defeatSound;

        [Header("Events")]
        public UnityEvent<int, int> onHealthChanged;
        public UnityEvent onPlayerDefeated;

        private bool m_IsInvulnerable = false;
        private Renderer[] m_Renderers;

        private void Awake()
        {
            currentHealth = maxHealth;
            m_Renderers = GetComponentsInChildren<Renderer>();
        }

        private void Start()
        {
            onHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void TakeDamage(int damage)
        {
            if (m_IsInvulnerable || currentHealth <= 0) return;

            currentHealth = Mathf.Max(0, currentHealth - damage);
            onHealthChanged?.Invoke(currentHealth, maxHealth);

            if (hurtSound != null)
            {
                AudioSource.PlayClipAtPoint(hurtSound, transform.position, 1f);
            }

            if (currentHealth <= 0)
            {
                Defeat();
            }
            else
            {
                StartCoroutine(InvulnerabilityRoutine());
            }
        }

        public void Heal(int amount)
        {
            if (currentHealth <= 0) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            onHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void ResetHealth()
        {
            currentHealth = maxHealth;
            m_IsInvulnerable = false;
            onHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void Defeat()
        {
            if (defeatSound != null)
            {
                AudioSource.PlayClipAtPoint(defeatSound, transform.position, 1f);
            }

            onPlayerDefeated?.Invoke();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerDied();
            }
        }

        private IEnumerator InvulnerabilityRoutine()
        {
            m_IsInvulnerable = true;
            float elapsed = 0f;
            float flashInterval = 0.15f;

            while (elapsed < invulnerabilityDuration)
            {
                SetRenderersVisible(false);
                yield return new WaitForSeconds(flashInterval);
                SetRenderersVisible(true);
                yield return new WaitForSeconds(flashInterval);
                elapsed += flashInterval * 2f;
            }

            SetRenderersVisible(true);
            m_IsInvulnerable = false;
        }

        private void SetRenderersVisible(bool visible)
        {
            if (m_Renderers == null) return;
            foreach (var r in m_Renderers)
            {
                if (r != null) r.enabled = visible;
            }
        }
    }
}
