using System;
using UnityEngine;

namespace JungleDash
{
    public class PlayerRunner : MonoBehaviour
    {
        public static readonly float[] LaneX = { -2.6f, 0f, 2.6f };

        public float laneChangeSpeed = 14f;
        public float jumpPower       = 12.5f;
        public float gravity         = -28f;
        public float flyHeight       = 2.4f;

        public int  currentLane = 1;
        public bool isGrounded  = true;
        public bool isRunning   = false;

        private float targetX;
        private float currentX;
        private float verticalVelocity;
        private float currentY;

        private Animator  animator;
        private GameObject characterVisual;
        private GameObject shieldVisual;

        public event Action             OnCrash;
        public event Action<Collectible> OnCollected;

        // ── Bootstrap ─────────────────────────────────────────────────────────
        private void Awake()
        {
            BuildVisual();
            BuildShield();
            EnsureCollider();
        }

        private static Shader SafeShader()
        {
            string[] names = {
                "Universal Render Pipeline/Lit",
                "Universal Render Pipeline/Simple Lit",
                "Universal Render Pipeline/Unlit",
                "Unlit/Color",
                "Standard"
            };
            foreach (var n in names) { var s = Shader.Find(n); if (s != null) return s; }
            return null;
        }

        private static Material ColorMat(Color c)
        {
            var sh = SafeShader();
            var m  = sh != null ? new Material(sh) : new Material(Shader.Find("Diffuse") ?? Shader.Find("Standard"));
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            return m;
        }

        private void BuildVisual()
        {
            // Try Ethan FBX from Resources
            var fbx = Resources.Load<GameObject>("Ethan");
            if (fbx != null)
            {
                characterVisual = Instantiate(fbx, transform);
                characterVisual.transform.localPosition = Vector3.zero;
                characterVisual.transform.localRotation = Quaternion.identity;
                characterVisual.transform.localScale    = Vector3.one;

                var rb = characterVisual.GetComponent<Rigidbody>();
                if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }

                // Disable all colliders on the visual – we use our own on the parent
                foreach (var col in characterVisual.GetComponentsInChildren<Collider>())
                    col.enabled = false;

                animator = characterVisual.GetComponentInChildren<Animator>();
                return;
            }

            // Procedural character – looks like an adventurer in orange gear
            characterVisual = new GameObject("RunnerVisual");
            characterVisual.transform.SetParent(transform, false);

            var bodyMat   = ColorMat(new Color(0.88f, 0.40f, 0.15f)); // orange vest
            var pantsMat  = ColorMat(new Color(0.25f, 0.22f, 0.18f)); // dark pants
            var skinMat   = ColorMat(new Color(0.82f, 0.62f, 0.44f)); // skin
            var shoesMat  = ColorMat(new Color(0.18f, 0.14f, 0.10f)); // dark shoes
            var hairMat   = ColorMat(new Color(0.22f, 0.14f, 0.08f)); // hair
            var hatMat    = ColorMat(new Color(0.50f, 0.32f, 0.10f)); // hat

            // Torso (upper body)
            AddPart(PrimitiveType.Capsule, new Vector3(0f, 1.08f, 0f), new Vector3(0.52f, 0.46f, 0.42f), bodyMat);
            // Head
            AddPart(PrimitiveType.Sphere,  new Vector3(0f, 1.72f, 0f), Vector3.one * 0.46f, skinMat);
            // Hat brim
            AddPart(PrimitiveType.Cylinder, new Vector3(0f, 1.98f, 0f), new Vector3(0.68f, 0.06f, 0.68f), hatMat);
            // Hat top
            AddPart(PrimitiveType.Cylinder, new Vector3(0f, 2.14f, 0f), new Vector3(0.46f, 0.18f, 0.46f), hatMat);
            // Pelvis
            AddPart(PrimitiveType.Cube, new Vector3(0f, 0.72f, 0f), new Vector3(0.48f, 0.22f, 0.38f), pantsMat);
            // Left leg
            AddPart(PrimitiveType.Capsule, new Vector3(-0.16f, 0.30f, 0f), new Vector3(0.22f, 0.36f, 0.22f), pantsMat);
            // Right leg
            AddPart(PrimitiveType.Capsule, new Vector3( 0.16f, 0.30f, 0f), new Vector3(0.22f, 0.36f, 0.22f), pantsMat);
            // Left shoe
            AddPart(PrimitiveType.Cube, new Vector3(-0.16f, 0.06f,  0.04f), new Vector3(0.24f, 0.12f, 0.32f), shoesMat);
            // Right shoe
            AddPart(PrimitiveType.Cube, new Vector3( 0.16f, 0.06f,  0.04f), new Vector3(0.24f, 0.12f, 0.32f), shoesMat);
            // Left arm
            AddPart(PrimitiveType.Capsule, new Vector3(-0.42f, 1.05f, 0f), new Vector3(0.20f, 0.32f, 0.20f), skinMat);
            // Right arm
            AddPart(PrimitiveType.Capsule, new Vector3( 0.42f, 1.05f, 0f), new Vector3(0.20f, 0.32f, 0.20f), skinMat);
            // Backpack
            AddPart(PrimitiveType.Cube, new Vector3(0f, 1.1f, -0.28f), new Vector3(0.36f, 0.44f, 0.22f), hatMat);
        }

        private void AddPart(PrimitiveType type, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.transform.SetParent(characterVisual.transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale    = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            Destroy(go.GetComponent<Collider>());
        }

        private void BuildShield()
        {
            shieldVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shieldVisual.name = "ShieldAura";
            shieldVisual.transform.SetParent(transform, false);
            shieldVisual.transform.localPosition = new Vector3(0f, 1f, 0f);
            shieldVisual.transform.localScale    = Vector3.one * 2.4f;
            Destroy(shieldVisual.GetComponent<Collider>());

            var sh = SafeShader() ?? Shader.Find("Standard");
            var m  = new Material(sh);
            var c  = new Color(0.2f, 0.85f, 1f, 0.32f);
            m.color = c;
            if (m.HasProperty("_BaseColor"))  m.SetColor("_BaseColor", c);
            if (m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", new Color(0f, 0.6f, 1f) * 1.8f);
            }
            shieldVisual.GetComponent<Renderer>().sharedMaterial = m;
            shieldVisual.SetActive(false);
        }

        private void EnsureCollider()
        {
            var col = GetComponent<CapsuleCollider>();
            if (col == null) col = gameObject.AddComponent<CapsuleCollider>();
            col.center  = new Vector3(0f, 0.9f, 0f);
            col.radius  = 0.42f;
            col.height  = 1.8f;
            col.isTrigger = true;
        }

        // ── Control ───────────────────────────────────────────────────────────
        public void StartRun()
        {
            isRunning  = true;
            currentLane = 1;
            targetX = currentX = LaneX[1];
            currentY = verticalVelocity = 0f;
            isGrounded = true;
            transform.position = new Vector3(currentX, 0f, 0f);
            SetAnim(true);
        }

        public void StopRun()
        {
            isRunning = false;
            SetAnim(false);
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

        private void SetAnim(bool running)
        {
            if (animator == null) return;
            animator.SetFloat("Forward", running ? 1f : 0f);
            animator.SetBool("OnGround", true);
        }

        // ── Update ────────────────────────────────────────────────────────────
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
                    currentY         += verticalVelocity * dt;
                    if (currentY <= 0f)
                    {
                        currentY = verticalVelocity = 0f;
                        isGrounded = true;
                        if (animator != null) { animator.SetBool("OnGround", true); animator.SetFloat("Jump", 0f); }
                    }
                }
            }

            transform.position = new Vector3(currentX, currentY, transform.position.z);

            // Shield visual
            bool shield = PowerUpManager.Instance != null && PowerUpManager.Instance.IsActive(PowerUpType.Shield);
            if (shieldVisual != null && shieldVisual.activeSelf != shield) shieldVisual.SetActive(shield);
            if (shield && shieldVisual != null)
                shieldVisual.transform.localScale = Vector3.one * (2.3f + Mathf.Sin(Time.time * 6f) * 0.14f);

            // Magnet sweep
            if (isRunning && PowerUpManager.Instance != null && PowerUpManager.Instance.IsActive(PowerUpType.Magnet))
            {
                foreach (var hit in Physics.OverlapSphere(transform.position, 11f))
                {
                    var col = hit.GetComponent<Collectible>();
                    if (col != null && !col.IsCollected && col.Type != CollectibleType.PowerUp)
                        col.PullTowards(transform.position + Vector3.up * 0.9f, 22f);
                }
            }
        }

        // ── Collision ─────────────────────────────────────────────────────────
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
            if (obstacle == null) return;

            var pm = PowerUpManager.Instance;
            if (pm != null && pm.IsActive(PowerUpType.SpeedBoost)) { obstacle.BreakObstacle(); return; }
            if (pm != null && pm.IsActive(PowerUpType.Shield))     { pm.ConsumeShield(); obstacle.BreakObstacle(); return; }
            if (pm != null && pm.IsActive(PowerUpType.Fly))        return;

            isRunning = false;
            SoundManager.Instance?.PlayCrash();
            OnCrash?.Invoke();
        }
    }
}
