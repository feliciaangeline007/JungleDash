using UnityEngine;

namespace JungleDash
{
    public enum CollectibleType { Coin, Gem, PowerUp }

    public class Collectible : MonoBehaviour
    {
        public CollectibleType Type;
        public PowerUpType PowerUpVariant;
        public bool IsCollected { get; private set; }

        private float bobPhase;

        private void Update()
        {
            if (IsCollected) return;
            transform.Rotate(0f, 160f * Time.deltaTime, 0f, Space.World);
            bobPhase += Time.deltaTime * 3.5f;
            Vector3 p = transform.localPosition;
            p.y = 0.75f + Mathf.Sin(bobPhase) * 0.18f;
            transform.localPosition = p;
        }

        public void PullTowards(Vector3 target, float speed)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        }

        public void Collect()
        {
            IsCollected = true;
            gameObject.SetActive(false);
        }
    }
}
