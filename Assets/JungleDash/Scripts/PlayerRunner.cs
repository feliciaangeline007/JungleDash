// PlayerRunner.cs – Temple Run-style player controller
// Uses Ethan FBX if available, otherwise procedural adventurer mesh.
// Supports lane-switch, jump, and slide.
using System;
using UnityEngine;

namespace JungleDash
{
    public class PlayerRunner : MonoBehaviour
    {
        // Lane positions synced with TrackSpawner
        public static float[] LaneX => TrackSpawner.LaneX;

        // ── Tuning ────────────────────────────────────────────────────────────
        public float laneSpeed    = 16f;
        public float jumpPower    = 11.5f;
        public float gravity      = -30f;
        public float flyHeight    = 2.8f;
        public float slideTime    = 0.55f;

        // ── State ─────────────────────────────────────────────────────────────
        public int  lane       = 1;
        public bool grounded   = true;
        public bool running    = false;
        public bool sliding    = false;

        private float targetX;
        private float currentX;
        private float velY;
        private float currentY;
        private float slideTimer;
        private float runTime;    // for procedural leg animation

        private GameObject visual;
        private GameObject shieldFx;

        // Leg parts for procedural run animation
        private Transform legL, legR, armL, armR;

        public event Action             OnCrash;
        public event Action<Collectible> OnCollected;

        // ── Bootstrap ─────────────────────────────────────────────────────────
        private void Awake()
        {
            BuildVisual();
            BuildShieldFx();
            EnsureCollider();
        }

        private void BuildVisual()
        {
            // Attempt Ethan from Resources
            var ethan = Resources.Load<GameObject>("Ethan");
            if (ethan != null)
            {
                visual = Instantiate(ethan, transform);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale    = Vector3.one;
                // Disable Ethan's own physics / colliders
                var rb = visual.GetComponent<Rigidbody>();
                if (rb) { rb.isKinematic = true; rb.useGravity = false; }
                foreach (var col in visual.GetComponentsInChildren<Collider>()) col.enabled = false;
                return;
            }

            // Procedural adventurer character
            visual = new GameObject("RunnerMesh");
            visual.transform.SetParent(transform, false);

            var body  = MatFactory.Opaque(new Color(.85f, .38f, .12f), 0f, .25f);  // orange jacket
            var pants = MatFactory.Opaque(new Color(.20f, .18f, .15f), 0f, .20f);  // dark trousers
            var skin  = MatFactory.Opaque(new Color(.80f, .60f, .42f), 0f, .15f);  // skin tone
            var boot  = MatFactory.Opaque(new Color(.14f, .10f, .07f), 0f, .30f);  // leather boots
            var hat   = MatFactory.Opaque(new Color(.44f, .28f, .08f), 0f, .20f);  // explorer hat
            var pack  = MatFactory.Opaque(new Color(.30f, .22f, .10f), 0f, .20f);  // backpack

            // Torso
            AddMeshPart(PrimitiveType.Capsule, new Vector3(0f, 1.05f, 0f), new Vector3(.52f, .48f, .42f), body);
            // Head
            AddMeshPart(PrimitiveType.Sphere,  new Vector3(0f, 1.72f, 0f), Vector3.one * .44f, skin);
            // Hat brim
            AddMeshPart(PrimitiveType.Cylinder,new Vector3(0f, 1.95f, 0f), new Vector3(.70f, .055f, .70f), hat);
            // Hat crown
            AddMeshPart(PrimitiveType.Cylinder,new Vector3(0f, 2.12f, 0f), new Vector3(.46f, .16f, .46f), hat);
            // Belt / hips
            AddMeshPart(PrimitiveType.Cube,    new Vector3(0f, .70f, 0f), new Vector3(.50f, .20f, .40f), pants);
            // Backpack
            AddMeshPart(PrimitiveType.Cube,    new Vector3(0f, 1.1f, -.26f), new Vector3(.36f, .44f, .20f), pack);

            // Legs (stored for animation)
            legL = AddMeshPartTf(PrimitiveType.Capsule, new Vector3(-.15f, .28f, 0f), new Vector3(.22f, .36f, .22f), pants);
            legR = AddMeshPartTf(PrimitiveType.Capsule, new Vector3( .15f, .28f, 0f), new Vector3(.22f, .36f, .22f), pants);
            AddMeshPart(PrimitiveType.Cube, new Vector3(-.15f, .06f, .04f), new Vector3(.24f, .12f, .32f), boot);
            AddMeshPart(PrimitiveType.Cube, new Vector3( .15f, .06f, .04f), new Vector3(.24f, .12f, .32f), boot);

            // Arms (stored for animation)
            armL = AddMeshPartTf(PrimitiveType.Capsule, new Vector3(-.42f, 1.05f, 0f), new Vector3(.20f, .30f, .20f), skin);
            armR = AddMeshPartTf(PrimitiveType.Capsule, new Vector3( .42f, 1.05f, 0f), new Vector3(.20f, .30f, .20f), skin);
        }

        private void AddMeshPart(PrimitiveType t, Vector3 p, Vector3 s, Material m)
        {
            var go = GameObject.CreatePrimitive(t);
            go.transform.SetParent(visual.transform, false);
            go.transform.localPosition = p;
            go.transform.localScale    = s;
            go.GetComponent<Renderer>().sharedMaterial = m;
            Destroy(go.GetComponent<Collider>());
        }

        private Transform AddMeshPartTf(PrimitiveType t, Vector3 p, Vector3 s, Material m)
        {
            AddMeshPart(t, p, s, m);
            return visual.transform.GetChild(visual.transform.childCount - 1);
        }

        private void BuildShieldFx()
        {
            shieldFx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shieldFx.name = "ShieldFx";
            shieldFx.transform.SetParent(transform, false);
            shieldFx.transform.localPosition = new Vector3(0f, 1f, 0f);
            shieldFx.transform.localScale    = Vector3.one * 2.5f;
            Destroy(shieldFx.GetComponent<Collider>());
            shieldFx.GetComponent<Renderer>().sharedMaterial =
                MatFactory.Glow(new Color(.2f, .85f, 1f), 1.5f);
            shieldFx.SetActive(false);
        }

        private void EnsureCollider()
        {
            var col = GetComponent<CapsuleCollider>() ?? gameObject.AddComponent<CapsuleCollider>();
            col.center    = new Vector3(0f, .85f, 0f);
            col.radius    = 0.38f;
            col.height    = 1.7f;
            col.isTrigger = true;
        }

        // ── Control ───────────────────────────────────────────────────────────
        public void StartRun()
        {
            running   = true;
            sliding   = false;
            lane      = 1;
            targetX   = currentX = LaneX[1];
            currentY  = velY = 0f;
            grounded  = true;
            runTime   = 0f;
            transform.position = new Vector3(currentX, 0f, 0f);
            ResizeCollider(false);
        }

        public void StopRun()
        {
            running = false;
        }

        public void MoveLeft()
        {
            if (!running || lane <= 0) return;
            lane--;
            targetX = LaneX[lane];
        }

        public void MoveRight()
        {
            if (!running || lane >= 2) return;
            lane++;
            targetX = LaneX[lane];
        }

        public void Jump()
        {
            if (!running || !grounded) return;
            velY     = jumpPower;
            grounded = false;
            if (sliding) { sliding = false; ResizeCollider(false); }
            SoundManager.Instance?.PlayJump();
        }

        public void Slide()
        {
            if (!running || !grounded) return;
            sliding    = true;
            slideTimer = slideTime;
            ResizeCollider(true);
            SoundManager.Instance?.PlayJump(); // reuse whoosh
        }

        private void ResizeCollider(bool slim)
        {
            var col = GetComponent<CapsuleCollider>();
            if (col == null) return;
            col.center = slim ? new Vector3(0f, .45f, 0f) : new Vector3(0f, .85f, 0f);
            col.height = slim ? .90f : 1.70f;
        }

        // ── Update ────────────────────────────────────────────────────────────
        private void Update()
        {
            float dt = Time.deltaTime;
            currentX = Mathf.Lerp(currentX, targetX, dt * laneSpeed);

            // Slide countdown
            if (sliding)
            {
                slideTimer -= dt;
                if (slideTimer <= 0f) { sliding = false; ResizeCollider(false); }
            }

            bool flying = PowerUpManager.Instance != null && PowerUpManager.Instance.IsActive(PowerUpType.Fly);
            if (flying)
            {
                currentY = Mathf.Lerp(currentY, flyHeight, dt * 6f);
                velY     = 0f;
                grounded = false;
            }
            else
            {
                if (!grounded)
                {
                    velY     += gravity * dt;
                    currentY += velY * dt;
                    if (currentY <= 0f) { currentY = velY = 0f; grounded = true; }
                }
            }

            transform.position = new Vector3(currentX, currentY, transform.position.z);

            // Shield fx
            bool hasShield = PowerUpManager.Instance != null && PowerUpManager.Instance.IsActive(PowerUpType.Shield);
            if (shieldFx != null && shieldFx.activeSelf != hasShield) shieldFx.SetActive(hasShield);
            if (hasShield && shieldFx != null)
                shieldFx.transform.localScale = Vector3.one * (2.4f + Mathf.Sin(Time.time * 7f) * .15f);

            // Procedural run animation (only when no animator)
            if (running && legL != null)
            {
                runTime += dt * 8f;
                float swing = Mathf.Sin(runTime) * 22f;
                legL.localRotation  = Quaternion.Euler( swing, 0f, 0f);
                legR.localRotation  = Quaternion.Euler(-swing, 0f, 0f);
                if (armL != null) armL.localRotation = Quaternion.Euler(-swing * .6f, 0f, 12f);
                if (armR != null) armR.localRotation = Quaternion.Euler( swing * .6f, 0f, -12f);
            }

            // Crouch visual for slide
            if (visual != null)
            {
                float targetSY = sliding ? .6f : 1f;
                Vector3 sc     = visual.transform.localScale;
                sc.y = Mathf.Lerp(sc.y, targetSY, dt * 12f);
                visual.transform.localScale = sc;
            }

            // Magnet sweep
            if (running && PowerUpManager.Instance != null && PowerUpManager.Instance.IsActive(PowerUpType.Magnet))
            {
                foreach (var hit in Physics.OverlapSphere(transform.position, 10f))
                {
                    var c = hit.GetComponent<Collectible>();
                    if (c != null && !c.IsCollected && c.Type != CollectibleType.PowerUp)
                        c.PullTowards(transform.position + Vector3.up * .8f, 20f);
                }
            }
        }

        // ── Collision ─────────────────────────────────────────────────────────
        private void OnTriggerEnter(Collider other)
        {
            if (!running) return;

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

            running = false;
            SoundManager.Instance?.PlayCrash();
            OnCrash?.Invoke();
        }
    }
}
