using UnityEngine;

namespace JungleDash
{
    public enum ObstacleType { Rock, FallenLog, AncientPillar, LowBarrier }

    public class Obstacle : MonoBehaviour
    {
        public ObstacleType Type;
        public float Height;
        public bool CanJumpOver => Height < 1.35f;

        public void Initialize(ObstacleType type, float height)
        {
            Type = type;
            Height = height;
        }

        public void BreakObstacle() => gameObject.SetActive(false);
    }
}
