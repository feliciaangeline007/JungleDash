using UnityEngine;

namespace AdventureGame
{
    public class WaterHazard : MonoBehaviour
    {
        [Header("Hazard Settings")]
        public float respawnDelay = 0.5f;

        private void OnTriggerEnter(Collider other)
        {
            var character = other.GetComponentInParent<UnityStandardAssets.Characters.ThirdPerson.ThirdPersonCharacter>();
            if (character != null || other.CompareTag("Player"))
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.RespawnPlayer();
                }
            }
        }
    }
}
