using UnityEngine;

namespace AdventureGame
{
    public class PlayerAudioEffects : MonoBehaviour
    {
        [Header("Audio Clips")]
        public AudioClip[] footstepClips;
        public AudioClip jumpClip;
        public AudioClip landClip;

        [Header("Step Timing")]
        public float stepInterval = 0.45f;
        public float sprintStepInterval = 0.28f;

        private AudioSource m_AudioSource;
        private Rigidbody m_Rigidbody;
        private PlayerStamina m_Stamina;
        private float m_StepTimer = 0f;
        private bool m_WasGrounded = true;
        private int m_LastFootstepIndex = -1;

        private void Awake()
        {
            m_AudioSource = GetComponent<AudioSource>();
            if (m_AudioSource == null)
            {
                m_AudioSource = gameObject.AddComponent<AudioSource>();
            }
            m_AudioSource.spatialBlend = 0.5f;
            m_AudioSource.playOnAwake = false;

            m_Rigidbody = GetComponent<Rigidbody>();
            m_Stamina = GetComponent<PlayerStamina>();
        }

        private void Update()
        {
            if (m_Rigidbody == null) return;

            bool isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.3f);
            float horizontalSpeed = new Vector2(m_Rigidbody.linearVelocity.x, m_Rigidbody.linearVelocity.z).magnitude;

            // Landing sound
            if (!m_WasGrounded && isGrounded && landClip != null)
            {
                m_AudioSource.PlayOneShot(landClip, 0.6f);
            }
            m_WasGrounded = isGrounded;

            // Footsteps
            if (isGrounded && horizontalSpeed > 0.8f)
            {
                float currentInterval = (m_Stamina != null && m_Stamina.isSprinting) ? sprintStepInterval : stepInterval;
                m_StepTimer += Time.deltaTime;

                if (m_StepTimer >= currentInterval)
                {
                    m_StepTimer = 0f;
                    PlayFootstep();
                }
            }
            else
            {
                m_StepTimer = 0f;
            }
        }

        public void PlayJump()
        {
            if (jumpClip != null && m_AudioSource != null)
            {
                m_AudioSource.PlayOneShot(jumpClip, 0.7f);
            }
        }

        private void PlayFootstep()
        {
            if (footstepClips == null || footstepClips.Length == 0 || m_AudioSource == null) return;

            int index = Random.Range(0, footstepClips.Length);
            if (footstepClips.Length > 1 && index == m_LastFootstepIndex)
            {
                index = (index + 1) % footstepClips.Length;
            }
            m_LastFootstepIndex = index;

            if (footstepClips[index] != null)
            {
                m_AudioSource.pitch = Random.Range(0.9f, 1.1f);
                m_AudioSource.PlayOneShot(footstepClips[index], 0.35f);
            }
        }
    }
}
