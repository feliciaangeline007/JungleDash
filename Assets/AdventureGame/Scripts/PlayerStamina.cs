using UnityEngine;
using UnityEngine.Events;

namespace AdventureGame
{
    public class PlayerStamina : MonoBehaviour
    {
        [Header("Stamina Settings")]
        public float maxStamina = 100f;
        public float currentStamina = 100f;
        public float drainRate = 25f; // Per second while sprinting
        public float rechargeRate = 18f; // Per second while resting
        public float sprintMultiplier = 1.6f;

        [Header("Events")]
        public UnityEvent<float, float> onStaminaChanged;

        public bool isSprinting { get; private set; }
        private bool m_MobileSprintActive = false;
        private UnityStandardAssets.Characters.ThirdPerson.ThirdPersonCharacter m_Character;

        private void Awake()
        {
            m_Character = GetComponent<UnityStandardAssets.Characters.ThirdPerson.ThirdPersonCharacter>();
            currentStamina = maxStamina;
        }

        private void Start()
        {
            onStaminaChanged?.Invoke(currentStamina, maxStamina);
        }

        private void Update()
        {
            // Check sprint input (Shift on keyboard or mobile button toggle)
            bool wantsToSprint = Input.GetKey(KeyCode.LeftShift) || m_MobileSprintActive;

            // Check if player is actually moving
            var rb = GetComponent<Rigidbody>();
            bool isMoving = rb != null && rb.linearVelocity.sqrMagnitude > 0.5f;

            if (wantsToSprint && isMoving && currentStamina > 5f)
            {
                isSprinting = true;
                currentStamina = Mathf.Max(0f, currentStamina - drainRate * Time.deltaTime);
            }
            else
            {
                isSprinting = false;
                if (!wantsToSprint)
                {
                    currentStamina = Mathf.Min(maxStamina, currentStamina + rechargeRate * Time.deltaTime);
                }
            }

            onStaminaChanged?.Invoke(currentStamina, maxStamina);
        }

        public void SetMobileSprint(bool active)
        {
            m_MobileSprintActive = active;
        }
    }
}
