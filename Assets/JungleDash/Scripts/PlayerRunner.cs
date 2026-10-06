using System;
using UnityEngine;

namespace JungleDash
{
    public class PlayerRunner : MonoBehaviour
    {
        public static readonly float[] LaneX = { -2.6f, 0f, 2.6f };

        public float laneChangeSpeed = 14f;
        public float jumpPower = 12.5f;
        public float gravity = -28f;
        public float flyHeight = 2.4f;

        public int currentLane = 1;
        public bool isGrounded = true;
        public bool isRunning = false;

        private float targetX = 0f;
        private float currentX = 0f;
        private float verticalVelocity = 0f;
        private float currentY = 0f;

        private Animator animator;
        private GameObject characterVisual;
        private GameObject shieldObject;

        public event Action OnCrash;
        public event Action<Collectible> OnCollected;

        private void Awake()
        {
            BuildCharacterVisual();
            BuildShieldVisual();
            EnsureCollider();
        }

        private void BuildCharacterVisual()
        {
            // Try Ethan FBX from Resources
            GameObject fbx = Resources.Load<GameObject>("Ethan");
            if (fbx != null)
            {
                characterVisual = Instantiate(fbx, transform);
                characterVisual.transform.localPosition = Vector3.zero;
                characterVisual.transform.localRotation = Quaternion.identity;
                var rb = characterVisual.GetComponent<Rigidbody>();
                if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }
                var cc = characterVisual.GetComponent<Collider>();
                if (cc != null) cc.enabled = false;
                animator = characterVisual.GetComponent<Animator>();
            }

            // Fallback: procedural capsule character
            if (characterVisual == null)
            {
                characterVisual = new GameObject("RunnerVisual");
                characterVisual.transform.SetParent(transform, false);

                Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                Material bodyMat = new Material(sh);
                Color orange = new Color(0.92f, 0.44f, 0.2f);
                bodyMat.color = orange;
                if (bodyMat.HasProperty("_BaseColor")) bodyMat.SetColor("_BaseColor", orange);

                Material skinMat = new Material(sh);
                Color skin = new Color(0.8f, 0.55f, 0.38f);
                skinMat.color = skin;
                if (skinMat.HasProperty("_BaseColor")) skinMat.SetColor("_BaseColor", skin);

                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.transform.SetParent(characterVisual.transform, false);
                body.transform.localPosition = new Vector3(0f, 0.95f, 0f);
                body.transform.localScale = new Vector3(0.6f, 0.85f, 0.5f);
                body.GetComponent<Renderer>().sharedMaterial = bodyMat;
                Destroy(body.GetComponent<Collider>());

                GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                head.transform.SetParent(characterVisual.transform, false);
                head.transform.localPosition = new Vector3(0f, 1.65f, 0f);
                head.transform.localScale = Vector3.one * 0.48f;
                head.GetComponent<Renderer>().sharedMaterial = skinMat;
                Destroy(head.GetComponent<Collider>());
            }
        }

        private void BuildShieldVisual()
        {
            shieldObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shieldObject.name = "ShieldAura";
            shieldObject.transform.SetParent(transform, false);
            shieldObject.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            shieldObject.transform.localScale = Vector3.one * 2.3f;
            Destroy(shieldObject.GetComponent<Collider>());

            Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material m = new Material(sh);
            Color c = new Color(0.2f, 0.85f, 1f, 0.38f);
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            shieldObject.GetComponent<Renderer>().sharedMaterial = m;
            shieldObject.SetActive(false);
        }

        private void EnsureCollider()
        {
            var col = GetComponent<CapsuleCollider>();
            if (col == null) col = gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.9f, 0f);
            col.radius = 0.4f;
            col.height = 1.8f;
            col.isTrigger = true;
        }

        public void StartRun()
        {
            isRunning = true;
            currentLane = 1;
            targetX = currentX = LaneX[1];
            currentY = verticalVelocity = 0f;
            isGrounded = true;
            transform.position = new Vector3(currentX, 0f, 0f);
            SetAnimRun(true);
        }

        public void StopRun()
        {
            isRunning = false;
            SetAnimRun(false);
        }

        public void MoveLeft()
        {
            if (!isRunning || currentLane <= 0) return;
            currentLane--;
            targetX = LaneX[currentLane];
        }

        public void MoveRight()
        {
            if (!isRunning || currentLane >= 2) return;
            currentLane++;
            targetX = LaneX[currentLane];
        }

        public void Jump()
        {
            if (!isRunning || !isGrounded) return;
            verticalVelocity = jumpPower;
            isGrounded = false;
            if (animator != null) { animator.SetBool("OnGround", false); animator.SetFloat("Jump", 1f); }
            SoundManager.Instance?.PlayJump();
        }

        private void SetAnimRun(bool running)
        {
            if (animator == null) return;
            animator.SetFloat("Forward", running ? 1f : 0f);
            animator.SetBool("OnGround", true);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            currentX = Mathf.Lerp(currentX, targetX, dt * laneChangeSpeed);

            bool flying = PowerUpManager.Instance != null && PowerUpManager.Instance.IsActive(PowerUpType.Fly);
            if (flying)
            {
                currentY = Mathf.Lerp(currentY, flyHeight, dt * 6f);
                verticalVelocity = 0f;
                isGrounded = false;
            }
            else
            {
                if (!isGrounded)
                {
                    verticalVelocity += gravity * dt;
                    currentY += verticalVelocity * dt;
                    if (currentY <= 0f)
                    {
                        currentY = verticalVelocity = 0f;
                        isGrounded = true;
                        if (animator != null) { animator.SetBool("OnGround", true); animator.SetFloat("Jump", 0f); }
                    }
                }
            }

            transform.position = new Vector3(currentX, currentY, transform.position.z);

            bool hasShield = PowerUpManager.Instance != null && PowerUpManager.Instance.IsActive(PowerUpType.Shield);
            if (shieldObject != null && shieldObject.activeSelf != hasShield)
                shieldObject.SetActive(hasShield);
            if (hasShield && shieldObject != null)
                shieldObject.transform.localScale = Vector3.one * (2.2f + Mathf.Sin(Time.time * 6f) * 0.12f);

            // Magnet pull
            if (isRunning && PowerUpManager.Instance != null && PowerUpManager.Instance.IsActive(PowerUpType.Magnet))
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, 11f);
                foreach (var h in hits)
                {
                    var col = h.GetComponent<Collectible>();
                    if (col != null && !col.IsCollected && col.Type != CollectibleType.PowerUp)
                        col.PullTowards(transform.position + Vector3.up * 0.8f, 22f);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isRunning) return;

            var collectible = other.GetComponent<Collectible>();
            if (collectible != null && !collectible.IsCollected)
            {
                collectible.Collect();
                OnCollected?.Invoke(collectible);
                return;
            }

            var obstacle = other.GetComponent<Obstacle>();
            if (obstacle != null)
            {
                if (PowerUpManager.Instance != null && PowerUpManager.Instance.IsActive(PowerUpType.SpeedBoost))
                {
                    obstacle.BreakObstacle();
                    return;
                }
                if (PowerUpManager.Instance != null && PowerUpManager.Instance.IsActive(PowerUpType.Shield))
                {
                    PowerUpManager.Instance.ConsumeShield();
                    obstacle.BreakObstacle();
                    return;
                }
                if (PowerUpManager.Instance != null && PowerUpManager.Instance.IsActive(PowerUpType.Fly))
                    return;

                isRunning = false;
                SoundManager.Instance?.PlayCrash();
                OnCrash?.Invoke();
            }
        }
    }
}
