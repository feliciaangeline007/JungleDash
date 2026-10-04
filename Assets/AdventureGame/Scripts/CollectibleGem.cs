using System.Collections;
using UnityEngine;

namespace AdventureGame
{
    public class CollectibleGem : MonoBehaviour
    {
        [Header("Movement")]
        public float rotateSpeed = 90f;
        public float bobSpeed = 2f;
        public float bobHeight = 0.25f;

        [Header("Score & Value")]
        public int pointValue = 100;
        public Color gemColor = new Color(1f, 0.85f, 0.2f); // Gold

        [Header("Effects")]
        public AudioClip pickupSound;
        public GameObject pickupEffectPrefab;

        private Vector3 m_StartPos;
        private bool m_Collected = false;

        private void Start()
        {
            m_StartPos = transform.position;
            // Slightly offset start time so not all gems bob in exact sync
            m_StartPos.y += Random.Range(-0.05f, 0.05f);
        }

        private void Update()
        {
            if (m_Collected) return;

            // Rotate
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

            // Bob up and down
            float newY = m_StartPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (m_Collected) return;

            // Check if player touched the gem
            if (other.CompareTag("Player") || other.GetComponentInParent<UnityStandardAssets.Characters.ThirdPerson.ThirdPersonCharacter>() != null)
            {
                m_Collected = true;

                // Play sound at point
                if (pickupSound != null)
                {
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position, 1.0f);
                }

                // Spawn particle burst if any
                if (pickupEffectPrefab != null)
                {
                    Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
                }

                // Notify GameManager
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddGem(pointValue);
                }

                Destroy(gameObject);
            }
        }
    }
}
