using UnityEngine;

namespace AdventureGame
{
    public class MovingPlatform : MonoBehaviour
    {
        [Header("Movement Settings")]
        public Vector3 moveOffset = new Vector3(0, 0, 8f);
        public float speed = 2.5f;
        public float waitTimeAtEnds = 1.0f;

        private Vector3 m_StartPos;
        private Vector3 m_EndPos;
        private float m_Progress = 0f;
        private bool m_MovingToEnd = true;
        private float m_WaitTimer = 0f;

        private void Start()
        {
            m_StartPos = transform.position;
            m_EndPos = m_StartPos + moveOffset;
        }

        private void Update()
        {
            if (m_WaitTimer > 0)
            {
                m_WaitTimer -= Time.deltaTime;
                return;
            }

            float step = (speed / Vector3.Distance(m_StartPos, m_EndPos)) * Time.deltaTime;

            if (m_MovingToEnd)
            {
                m_Progress += step;
                if (m_Progress >= 1.0f)
                {
                    m_Progress = 1.0f;
                    m_MovingToEnd = false;
                    m_WaitTimer = waitTimeAtEnds;
                }
            }
            else
            {
                m_Progress -= step;
                if (m_Progress <= 0.0f)
                {
                    m_Progress = 0.0f;
                    m_MovingToEnd = true;
                    m_WaitTimer = waitTimeAtEnds;
                }
            }

            // Smooth cosine interpolation
            float smoothT = Mathf.SmoothStep(0f, 1f, m_Progress);
            transform.position = Vector3.Lerp(m_StartPos, m_EndPos, smoothT);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponent<UnityStandardAssets.Characters.ThirdPerson.ThirdPersonCharacter>() != null)
            {
                other.transform.SetParent(transform);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponent<UnityStandardAssets.Characters.ThirdPerson.ThirdPersonCharacter>() != null)
            {
                other.transform.SetParent(null);
            }
        }
    }
}
