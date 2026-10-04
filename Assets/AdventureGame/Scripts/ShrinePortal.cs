using UnityEngine;

namespace AdventureGame
{
    public class ShrinePortal : MonoBehaviour
    {
        [Header("Portal Visuals")]
        public GameObject portalVFX;
        public Light portalBeaconLight;
        public Renderer[] keyPedestals;

        [Header("Audio")]
        public AudioClip unlockFanfare;

        private bool m_IsUnlocked = false;

        private void Start()
        {
            if (portalVFX != null) portalVFX.SetActive(false);
            if (portalBeaconLight != null) portalBeaconLight.enabled = false;
        }

        public void CheckAndUnlock(bool hasRuby, bool hasSapphire, bool hasEmerald)
        {
            if (m_IsUnlocked) return;

            if (hasRuby && hasSapphire && hasEmerald)
            {
                UnlockPortal();
            }
        }

        private void UnlockPortal()
        {
            m_IsUnlocked = true;

            if (portalVFX != null) portalVFX.SetActive(true);
            if (portalBeaconLight != null) portalBeaconLight.enabled = true;

            if (unlockFanfare != null)
            {
                AudioSource.PlayClipAtPoint(unlockFanfare, transform.position, 1.0f);
            }

            Debug.Log("<color=cyan><b>[ShrinePortal] The Ancient Portal is now active! Step inside to complete your quest!</b></color>");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponentInParent<PlayerHealth>() != null)
            {
                if (m_IsUnlocked)
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.TriggerVictory();
                    }
                }
                else
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.ShowMessage("Gather all 3 Ancient Keys to activate the Shrine Portal!");
                    }
                }
            }
        }
    }
}
