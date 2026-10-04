using UnityEngine;

namespace AdventureGame
{
    public enum KeyType
    {
        Ruby,
        Sapphire,
        Emerald
    }

    public class CollectibleKey : MonoBehaviour
    {
        [Header("Key Properties")]
        public KeyType keyType;
        public string keyName = "Ancient Key";
        public Color keyColor = Color.red;
        public float rotateSpeed = 70f;
        public float bobSpeed = 2f;
        public float bobHeight = 0.2f;

        [Header("Audio")]
        public AudioClip pickupSound;

        private Vector3 m_StartPos;
        private bool m_Collected = false;

        private void Start()
        {
            m_StartPos = transform.position;
        }

        private void Update()
        {
            if (m_Collected) return;

            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
            float newY = m_StartPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (m_Collected) return;

            if (other.CompareTag("Player") || other.GetComponentInParent<PlayerHealth>() != null)
            {
                m_Collected = true;

                if (pickupSound != null)
                {
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position, 1f);
                }

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.CollectKey(keyType);
                }

                Destroy(gameObject);
            }
        }
    }
}
