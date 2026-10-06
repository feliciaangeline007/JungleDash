using UnityEngine;

namespace JungleDash
{
    public enum CollectibleType { Coin, Gem, PowerUp }

    public class Collectible : MonoBehaviour
    {
        public CollectibleType Type;
        public PowerUpType PowerUpVariant;
        public bool IsCollected { get; private set; }

        // BUG FIX: store the spawn Y in world space, not local space.
        // Using localPosition.y caused coins to drift underground when the
        // segment parent moved, or snap when the game restarted.
        private float baseWorldY;
        private float bobPhase;
        private bool  initialized;

        private void Start()
        {
            baseWorldY  = transform.position.y;
            bobPhase    = Random.Range(0f, Mathf.PI * 2f); // stagger so they don't all bob together
            initialized = true;
        }

        private void Update()
        {
            if (IsCollected || !initialized) return;

            // Spin around world-up axis
            transform.Rotate(0f, 180f * Time.deltaTime, 0f, Space.World);

            // Bob in world space (not local)
            bobPhase += Time.deltaTime * 3.5f;
            Vector3 p = transform.position;
            p.y = baseWorldY + Mathf.Sin(bobPhase) * 0.18f;
            transform.position = p;
        }

        public void PullTowards(Vector3 worldTarget, float speed)
        {
            transform.position = Vector3.MoveTowards(transform.position, worldTarget, speed * Time.deltaTime);
        }

        public void Collect()
        {
            IsCollected = true;
            gameObject.SetActive(false);
        }
    }
}
