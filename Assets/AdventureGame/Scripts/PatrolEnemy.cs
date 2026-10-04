using UnityEngine;

namespace AdventureGame
{
    public class PatrolEnemy : MonoBehaviour
    {
        [Header("Patrol & Movement")]
        public Transform[] waypoints;
        public float patrolSpeed = 2.0f;
        public float chaseSpeed = 3.5f;
        public float waypointTolerance = 0.5f;

        [Header("Aggro & Combat")]
        public float detectionRadius = 8.0f;
        public float attackRadius = 1.3f;
        public int attackDamage = 1;
        public float attackCooldown = 1.5f;

        [Header("Defeat & Drops")]
        public GameObject defeatEffectPrefab;
        public int pointsOnDefeat = 250;

        private int m_CurrentWaypointIndex = 0;
        private Transform m_Player;
        private PlayerHealth m_PlayerHealth;
        private float m_LastAttackTime = -99f;
        private bool m_IsChasing = false;
        private bool m_IsDefeated = false;
        private UnityStandardAssets.Characters.ThirdPerson.ThirdPersonCharacter m_Character;

        private void Start()
        {
            m_Character = GetComponent<UnityStandardAssets.Characters.ThirdPerson.ThirdPersonCharacter>();
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                m_Player = p.transform;
                m_PlayerHealth = p.GetComponent<PlayerHealth>();
            }
        }

        private void Update()
        {
            if (m_IsDefeated) return;

            if (m_Player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null)
                {
                    m_Player = p.transform;
                    m_PlayerHealth = p.GetComponent<PlayerHealth>();
                }
                return;
            }

            float distToPlayer = Vector3.Distance(transform.position, m_Player.position);

            if (distToPlayer <= detectionRadius)
            {
                m_IsChasing = true;
                ChasePlayer(distToPlayer);
            }
            else
            {
                m_IsChasing = false;
                Patrol();
            }
        }

        private void Patrol()
        {
            if (waypoints == null || waypoints.Length == 0) return;

            Transform targetWp = waypoints[m_CurrentWaypointIndex];
            if (targetWp == null) return;

            Vector3 direction = (targetWp.position - transform.position);
            direction.y = 0;

            if (direction.magnitude <= waypointTolerance)
            {
                m_CurrentWaypointIndex = (m_CurrentWaypointIndex + 1) % waypoints.Length;
                if (m_Character != null) m_Character.Move(Vector3.zero, false, false);
            }
            else
            {
                if (m_Character != null)
                {
                    m_Character.Move(direction.normalized * (patrolSpeed / 4f), false, false);
                }
                else
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
                    transform.position += transform.forward * patrolSpeed * Time.deltaTime;
                }
            }
        }

        private void ChasePlayer(float distance)
        {
            Vector3 direction = (m_Player.position - transform.position);
            direction.y = 0;

            if (direction.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 6f);
            }

            if (distance > attackRadius)
            {
                if (m_Character != null)
                {
                    m_Character.Move(direction.normalized * (chaseSpeed / 4f), false, false);
                }
                else
                {
                    transform.position += transform.forward * chaseSpeed * Time.deltaTime;
                }
            }
            else
            {
                if (m_Character != null) m_Character.Move(Vector3.zero, false, false);

                // Attack player
                if (Time.time - m_LastAttackTime >= attackCooldown)
                {
                    m_LastAttackTime = Time.time;
                    if (m_PlayerHealth != null)
                    {
                        m_PlayerHealth.TakeDamage(attackDamage);
                    }

                    // Knockback player
                    var rb = m_Player.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        Vector3 knockDir = (m_Player.position - transform.position).normalized + Vector3.up * 0.4f;
                        rb.AddForce(knockDir * 6f, ForceMode.Impulse);
                    }
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (m_IsDefeated) return;

            // Check if player jumped on top of the enemy
            if (collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponentInParent<PlayerHealth>() != null)
            {
                foreach (ContactPoint contact in collision.contacts)
                {
                    // If contact normal points downwards, player landed on top!
                    if (contact.normal.y < -0.5f)
                    {
                        DefeatEnemy(collision.gameObject);
                        return;
                    }
                }
            }
        }

        private void DefeatEnemy(GameObject playerObj)
        {
            m_IsDefeated = true;

            // Bounce player up
            var rb = playerObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 7.5f, rb.linearVelocity.z);
            }

            // Spawn effect
            if (defeatEffectPrefab != null)
            {
                Instantiate(defeatEffectPrefab, transform.position, Quaternion.identity);
            }

            // Award points
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(pointsOnDefeat);
            }

            Destroy(gameObject, 0.1f);
        }
    }
}
