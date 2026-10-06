using System.Collections.Generic;
using UnityEngine;

namespace JungleDash
{
    public enum PowerUpType { Magnet, Shield, SpeedBoost, DoubleScore, Fly }

    public class PowerUpManager : MonoBehaviour
    {
        public static PowerUpManager Instance { get; private set; }

        private readonly Dictionary<PowerUpType, float> activeTimers = new Dictionary<PowerUpType, float>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (activeTimers.Count == 0) return;
            float dt = Time.deltaTime;
            var toRemove = new List<PowerUpType>();
            var keys = new List<PowerUpType>(activeTimers.Keys);
            foreach (var k in keys)
            {
                activeTimers[k] -= dt;
                if (activeTimers[k] <= 0f) toRemove.Add(k);
            }
            foreach (var k in toRemove) activeTimers.Remove(k);
        }

        public void Activate(PowerUpType type)
        {
            float duration;
            if (type == PowerUpType.Magnet)         duration = 10f;
            else if (type == PowerUpType.Shield)    duration = 9999f;
            else if (type == PowerUpType.SpeedBoost)duration = 6f;
            else if (type == PowerUpType.DoubleScore)duration = 12f;
            else if (type == PowerUpType.Fly)       duration = 8f;
            else                                    duration = 8f;

            activeTimers[type] = duration;
        }

        public bool IsActive(PowerUpType type) => activeTimers.ContainsKey(type);

        public float GetRemainingTime(PowerUpType type)
        {
            float t;
            return activeTimers.TryGetValue(type, out t) ? t : 0f;
        }

        public void ConsumeShield()
        {
            activeTimers.Remove(PowerUpType.Shield);
        }

        public void ResetAll() => activeTimers.Clear();
    }
}
