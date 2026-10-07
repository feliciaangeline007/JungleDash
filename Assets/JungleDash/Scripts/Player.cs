// Player.cs - The explorer: lanes, jump, slide, flying, animation with premium game-feel polish.
using UnityEngine;

namespace JungleDash
{
    public class Player : MonoBehaviour
    {
        const float Gravity = 38f;
        const float JumpV = 14.2f;
        const float DoubleJumpV = 11f;
        const float SlideTime = 0.85f;
        const float CoyoteTime = 0.12f;
        const float JumpBufferTime = 0.15f;
        const float LaneSwitchSpeed = 18f;

        public int lane = 1;                // 0,1,2
        public float x, y, vy;
        public bool grounded = true, sliding, alive = true, running, flying, speedBoost, doubleScore;

        public float Height { get { return sliding ? 0.8f : (flying ? 2.6f : 1.75f); } }
        public float Z { get { return transform.position.z; } }

        Transform model;
        Animator anim;
        GameObject shieldGO, flyGO, speedGO, doubleGO;
        float slideT, deadT, tiltYaw, tiltRoll, animSpeed = 1f;
        public System.Action OnLand;
        public System.Action OnDoubleJump;

        // Enhanced game feel
        float coyoteTimer;
        float jumpBufferTimer;
        bool hasDoubleJumped;
        float landingImpact;
        float runVibration;
        float breathCycle;
        
        // Smooth lane switching
        float laneSwitchVelocity;
        int previousLane = 1;
        float laneSwitchTimer;

        // Trail effect
        GameObject trailGO;
        float trailPulse;

        public void Build(GameAssets ga)
        {
            GameObject m = null;
            if (ga != null && ga.ethan != null) m = Instantiate(ga.ethan);
            if (m == null)
            {
                GameObject ep = Resources.Load<GameObject>("Ethan");
                if (ep != null) m = Instantiate(ep);
            }

            if (m != null)
            {
                m.name = "Explorer";
                model = m.transform;
                model.SetParent(transform, false);
                anim = m.GetComponent<Animator>();
                if (anim == null) anim = m.AddComponent<Animator>();
                if (ga != null && ga.animator != null) anim.runtimeAnimatorController = ga.animator;
                else anim.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("EthanController");
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                int savedSkin = PlayerPrefs.GetInt("jd_selected_skin", 0);
                ApplySkin(m, savedSkin);
            }
            else
            {
                GameObject cap = new GameObject("Explorer");
                cap.transform.SetParent(transform, false);
                model = cap.transform;
                int savedSkin = PlayerPrefs.GetInt("jd_selected_skin", 0);
                Color capCol = savedSkin == 1 ? new Color(0.12f, 0.14f, 0.17f)
                            : savedSkin == 2 ? new Color(1f,    0.82f, 0.18f)
                            : savedSkin == 3 ? new Color(0.08f, 0.72f, 0.92f)
                            :                  new Color(0.72f, 0.58f, 0.30f);  // khaki
                // Body capsule
                Gfx.Prim(PrimitiveType.Capsule, model, new Vector3(0, 0.90f, 0), new Vector3(0.58f, 0.88f, 0.58f),
                         Mats.Flat(capCol, 0.22f));
                // Head sphere — slightly lighter
                Gfx.Prim(PrimitiveType.Sphere, model, new Vector3(0, 1.95f, 0), new Vector3(0.45f, 0.45f, 0.45f),
                         Mats.Flat(new Color(capCol.r + 0.08f, capCol.g + 0.06f, capCol.b), 0.18f));
                // Teal explorer hat brim
                Gfx.Prim(PrimitiveType.Cylinder, model, new Vector3(0, 2.22f, 0), new Vector3(0.55f, 0.06f, 0.55f),
                         Mats.Flat(new Color(0.05f, 0.55f, 0.55f), 0.30f));
                // Backpack
                Gfx.Prim(PrimitiveType.Cube, model, new Vector3(0, 1.05f, -0.28f), new Vector3(0.32f, 0.45f, 0.22f),
                         Mats.Flat(new Color(0.28f, 0.22f, 0.12f), 0.12f));
            }

            // ── POWER-UP VISUALS — Bioluminescent Temple theme ────────────────
            // Shield: electric blue glass bubble with teal emissive rim
            Material glass = Mats.Get(MatKind.Glass, new Color(0.10f, 0.55f, 1.00f, 0.22f), null, 1, 1, 0.95f);
            shieldGO = Gfx.Prim(PrimitiveType.Sphere, transform, new Vector3(0, 0.95f, 0), new Vector3(2.3f, 2.3f, 2.3f),
                                glass, default(Vector3), false, "Shield");
            shieldGO.SetActive(false);

            // Fly aura: vivid violet-purple wings with teal hover ring
            Material mFly = Mats.Get(MatKind.Emissive, new Color(0.78f, 0.25f, 1.00f), null, 1, 1, 0.85f, 2.5f);
            Material mFlyRing = Mats.Get(MatKind.Emissive, new Color(0.05f, 0.95f, 0.85f), null, 1, 1, 0.90f, 2.0f);
            flyGO = new GameObject("FlyAura");
            flyGO.transform.SetParent(transform, false);
            Gfx.Prim(PrimitiveType.Cylinder, flyGO.transform, new Vector3(-0.70f, 1.18f, -0.22f), new Vector3(0.14f, 0.82f, 0.48f), mFly, new Vector3(22, 0, -38), false, "WingL");
            Gfx.Prim(PrimitiveType.Cylinder, flyGO.transform, new Vector3( 0.70f, 1.18f, -0.22f), new Vector3(0.14f, 0.82f, 0.48f), mFly, new Vector3(22, 0,  38), false, "WingR");
            Gfx.Prim(PrimitiveType.Cylinder, flyGO.transform, new Vector3(0, 0.05f, 0), new Vector3(1.25f, 0.10f, 1.25f), mFlyRing, default(Vector3), false, "HoverRing");
            flyGO.SetActive(false);

            // Speed aura: deep orange elongated trail capsule
            Material mSpd = Mats.Get(MatKind.Emissive, new Color(1f, 0.48f, 0.00f), null, 1, 1, 0.92f, 2.2f);
            speedGO = new GameObject("SpeedAura");
            speedGO.transform.SetParent(transform, false);
            Gfx.Prim(PrimitiveType.Capsule, speedGO.transform, new Vector3(0, 0.90f, -0.45f), new Vector3(0.60f, 1.20f, 1.40f), mSpd, new Vector3(65, 0, 0), false, "Trail");
            speedGO.SetActive(false);

            // Double score aura: blazing gold-yellow rotating star
            Material mDbl = Mats.Get(MatKind.Emissive, new Color(1f, 0.95f, 0.05f), null, 1, 1, 0.92f, 2.5f);
            doubleGO = new GameObject("DoubleAura");
            doubleGO.transform.SetParent(transform, false);
            Gfx.Prim(PrimitiveType.Cube,   doubleGO.transform, new Vector3(0, 2.35f, 0), new Vector3(0.38f, 0.38f, 0.38f), mDbl, new Vector3(45, 45, 0), false, "StarA");
            Gfx.Prim(PrimitiveType.Sphere, doubleGO.transform, new Vector3(0, 2.35f, 0), new Vector3(0.22f, 0.22f, 0.22f), mDbl, default(Vector3), false, "StarCore");
            doubleGO.SetActive(false);

            // Speed trail: teal-cyan ground streak
            Material mTrail = Mats.Get(MatKind.Emissive, new Color(0.05f, 0.90f, 0.80f, 0.45f), null, 1, 1, 0.55f, 1.2f);
            trailGO = new GameObject("RunTrail");
            trailGO.transform.SetParent(transform, false);
            Gfx.Prim(PrimitiveType.Cube, trailGO.transform, new Vector3(0, 0.04f, -0.9f), new Vector3(0.55f, 0.05f, 2.2f), mTrail, default(Vector3), false, "TrailStrip");
            trailGO.SetActive(false);
        }

        public int currentSkin = 0;

        public void SetSkin(int skinId)
        {
            currentSkin = skinId;
            if (model != null) ApplySkin(model.gameObject, currentSkin);
        }

        public static void ApplySkin(GameObject m, int skinId)
        {
            Color bodyCol, accentCol;
            float smooth = 0.25f;
            switch (skinId)
            {
                case 1: // Shadow Hunter
                    bodyCol = new Color(0.12f, 0.14f, 0.17f);
                    accentCol = new Color(0.85f, 0.15f, 0.15f);
                    smooth = 0.5f;
                    break;
                case 2: // Golden Champion
                    bodyCol = new Color(1f, 0.82f, 0.18f);
                    accentCol = new Color(1f, 0.95f, 0.6f);
                    smooth = 0.8f;
                    break;
                case 3: // Cyber Nomad
                    bodyCol = new Color(0.08f, 0.72f, 0.92f);
                    accentCol = new Color(0.85f, 0.18f, 0.95f);
                    smooth = 0.6f;
                    break;
                default: // Classic Explorer — teal-accented khaki field gear
                    bodyCol = new Color(0.72f, 0.58f, 0.30f);   // warm khaki
                    accentCol = new Color(0.05f, 0.62f, 0.62f); // teal accent
                    smooth = 0.20f;
                    break;
            }

            Material body = Mats.Flat(bodyCol, smooth);
            Material accent = Mats.Flat(accentCol, 0.8f);

            foreach (Renderer r in m.GetComponentsInChildren<Renderer>())
            {
                if (r.name.Contains("Shield") || r.name.Contains("Wing") || r.name.Contains("Ring") || r.name.Contains("Trail") || r.name.Contains("Star"))
                    continue;

                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = r.name.ToLower().Contains("glass") ? accent : body;
                r.sharedMaterials = mats;
            }
        }

        public void ResetPlayer()
        {
            lane = 1; x = 0; y = 0; vy = 0; grounded = true; sliding = false; alive = true; running = false;
            flying = false; speedBoost = false; doubleScore = false;
            deadT = 0; slideT = 0;
            coyoteTimer = 0; jumpBufferTimer = 0; hasDoubleJumped = false;
            landingImpact = 0; runVibration = 0; breathCycle = 0;
            previousLane = 1; laneSwitchTimer = 0; laneSwitchVelocity = 0;
            transform.position = Vector3.zero;
            if (model != null) { model.localPosition = Vector3.zero; model.localRotation = Quaternion.identity; model.localScale = Vector3.one; }
            SetShield(false);
            SetFlying(false);
            SetSpeed(false);
            SetDouble(false);
            if (trailGO != null) trailGO.SetActive(false);
            if (anim != null) { anim.Rebind(); anim.Update(0f); }
        }

        public void SetShield(bool on) { if (shieldGO != null) shieldGO.SetActive(on); }
        public void SetFlying(bool on) { flying = on; if (flyGO != null) flyGO.SetActive(on); }
        public void SetSpeed(bool on) { speedBoost = on; if (speedGO != null) speedGO.SetActive(on); }
        public void SetDouble(bool on) { doubleScore = on; if (doubleGO != null) doubleGO.SetActive(on); }

        Vector3 currentScale = Vector3.one;
        Vector3 targetScale = Vector3.one;
        Vector3 scaleVelocity = Vector3.zero;

        public bool ChangeLane(int dir)
        {
            int n = Mathf.Clamp(lane + dir, 0, 2);
            if (n == lane) return false;
            previousLane = lane;
            lane = n;
            laneSwitchTimer = 0.2f;
            laneSwitchVelocity = dir * 12f;
            targetScale = new Vector3(0.88f, 1.06f, 0.92f);
            return true;
        }

        public bool Jump()
        {
            // Jump buffer - store intent
            jumpBufferTimer = JumpBufferTime;
            
            if (flying || !alive) return false;
            
            // Coyote time allows jump slightly after leaving ground
            bool canJump = grounded || coyoteTimer > 0f;
            
            if (canJump)
            {
                grounded = false;
                sliding = false;
                vy = JumpV;
                coyoteTimer = 0f;
                jumpBufferTimer = 0f;
                hasDoubleJumped = false;
                targetScale = new Vector3(0.78f, 1.32f, 0.82f);
                return true;
            }
            
            // Double jump (small boost)
            if (!grounded && !hasDoubleJumped)
            {
                hasDoubleJumped = true;
                vy = DoubleJumpV;
                jumpBufferTimer = 0f;
                targetScale = new Vector3(0.85f, 1.2f, 0.85f);
                if (VFX.Instance != null)
                {
                    VFX.Instance.PlayDoubleJumpBurst(transform.position + Vector3.up * 0.3f);
                }
                if (OnDoubleJump != null) OnDoubleJump();
                return true;
            }
            
            return false;
        }

        public bool Slide()
        {
            if (flying || !alive) return false;
            if (!grounded)
            {
                vy = Mathf.Min(vy, -20f); // Fast fall - snappier
                targetScale = new Vector3(0.85f, 1.2f, 0.85f);
                return false;
            }
            if (sliding) { slideT = SlideTime; return false; }
            sliding = true; slideT = SlideTime;
            targetScale = new Vector3(1.18f, 0.62f, 1.15f);
            return true;
        }

        public void Die()
        {
            alive = false; running = false; sliding = false;
            SetShield(false); SetFlying(false); SetSpeed(false); SetDouble(false);
            if (trailGO != null) trailGO.SetActive(false);
            if (VFX.Instance != null)
            {
                VFX.Instance.PlayObstacleHit(transform.position + Vector3.up * 0.8f);
                VFX.Instance.PlayDeathExplosion(transform.position + Vector3.up * 0.5f);
            }
        }

        public void Tick(float dt, float speed)
        {
            if (dt <= 0f) return;

            // Squash & stretch with spring physics (smoother, more organic)
            Vector3 scaleDiff = targetScale - currentScale;
            scaleVelocity += scaleDiff * 320f * dt;
            scaleVelocity *= Mathf.Exp(-12f * dt);
            currentScale += scaleVelocity * dt;
            targetScale = Vector3.Lerp(targetScale, Vector3.one, 1f - Mathf.Exp(-6f * dt));
            if (model != null) model.localScale = currentScale;

            // Coyote time countdown
            if (grounded) coyoteTimer = CoyoteTime;
            else if (coyoteTimer > 0f) coyoteTimer -= dt;
            
            // Jump buffer - auto-jump when landing
            if (jumpBufferTimer > 0f)
            {
                jumpBufferTimer -= dt;
                if (grounded && jumpBufferTimer > 0f && alive)
                {
                    Jump();
                }
            }

            if (!alive)
            {
                deadT += dt;
                float k = Mathf.Clamp01(deadT * 2.5f);
                float bounce = Mathf.Sin(deadT * 12f) * Mathf.Exp(-deadT * 6f) * 8f;
                if (model != null)
                {
                    model.localRotation = Quaternion.Euler(Mathf.Lerp(0f, -85f, k) + bounce, bounce * 0.5f, bounce * 0.3f);
                    model.localPosition = new Vector3(0f, 0.2f * k, -0.3f * k);
                }
                if (anim != null) anim.speed = Mathf.Lerp(anim.speed, 0f, 1f - Mathf.Exp(-8f * dt));
                return;
            }

            // Breathing idle animation
            breathCycle += dt * (running ? 4f : 1.5f);

            float targetX = (lane - 1) * World.LaneW;
            float prev = x;
            x = Mathf.Lerp(x, targetX, 1f - Mathf.Exp(-LaneSwitchSpeed * dt));
            float vx = (x - prev) / dt;

            // Trail visibility
            if (trailGO != null)
            {
                bool showTrail = running && (speedBoost || speed > 18f);
                trailGO.SetActive(showTrail);
                if (showTrail)
                {
                    trailPulse += dt * 8f;
                    float pulse = 0.8f + Mathf.Sin(trailPulse) * 0.2f;
                    trailGO.transform.localScale = new Vector3(pulse, 1f, 1f + (speed - 12f) * 0.08f);
                }
            }

            if (flying)
            {
                grounded = false;
                sliding = false;
                vy = 0f;
                y = Mathf.Lerp(y, 3.5f, 1f - Mathf.Exp(-6f * dt));
                if (flyGO != null)
                {
                    flyGO.transform.localRotation = Quaternion.Euler(0f, Time.time * 180f, 0f);
                    // Wing bob
                    float wingBob = Mathf.Sin(Time.time * 6f) * 0.08f;
                    flyGO.transform.localPosition = new Vector3(0f, wingBob, 0f);
                }
            }
            else
            {
                if (!grounded)
                {
                    vy -= Gravity * dt;
                    y += vy * dt;
                    if (y <= 0f)
                    {
                        y = 0f;
                        landingImpact = Mathf.Clamp01(Mathf.Abs(vy) / 20f);
                        vy = 0f; grounded = true;
                        hasDoubleJumped = false;
                        
                        // Scale based on landing impact
                        float impactStrength = 0.15f + landingImpact * 0.2f;
                        targetScale = new Vector3(1f + impactStrength, 1f - impactStrength, 1f + impactStrength);
                        
                        if (VFX.Instance != null)
                        {
                            VFX.Instance.PlayLandingDust(transform.position);
                        }
                        if (OnLand != null) OnLand();
                    }
                }
            }

            if (doubleGO != null && doubleScore)
            {
                doubleGO.transform.localRotation = Quaternion.Euler(Time.time * 90f, Time.time * 140f, 0f);
                float bob = Mathf.Sin(Time.time * 4f) * 0.1f;
                doubleGO.transform.localPosition = new Vector3(0f, 2.3f + bob, 0f);
            }

            // Shield pulse animation
            if (shieldGO != null && shieldGO.activeSelf)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 3f) * 0.06f;
                shieldGO.transform.localScale = new Vector3(2.3f * pulse, 2.3f * pulse, 2.3f * pulse);
            }

            if (sliding)
            {
                slideT -= dt;
                if (slideT <= 0f) sliding = false;
            }

            // Running vibration (subtle head bob)
            if (running && grounded && !sliding)
            {
                runVibration += dt * (speed * 1.2f);
            }

            float z = transform.position.z + (running ? speed * dt : 0f);
            transform.position = new Vector3(x, y, z);

            // Lane switch timer
            if (laneSwitchTimer > 0f)
            {
                laneSwitchTimer -= dt;
                laneSwitchVelocity = Mathf.Lerp(laneSwitchVelocity, 0f, 1f - Mathf.Exp(-14f * dt));
            }

            // Body language
            float yawT = Mathf.Clamp(vx * 2.5f, -30f, 30f);
            float rollT = Mathf.Clamp(-vx * 1.1f, -14f, 14f);
            tiltYaw = Mathf.Lerp(tiltYaw, yawT, 1f - Mathf.Exp(-16f * dt));
            tiltRoll = Mathf.Lerp(tiltRoll, rollT, 1f - Mathf.Exp(-16f * dt));
            
            float pitch;
            if (flying) pitch = 12f + Mathf.Sin(Time.time * 2f) * 3f;
            else if (sliding) pitch = 62f;
            else if (grounded)
            {
                float basePitch = speedBoost ? 16f : 4f;
                float headBob = running ? Mathf.Sin(runVibration) * 1.5f : Mathf.Sin(breathCycle) * 1f;
                pitch = basePitch + headBob;
            }
            else
            {
                pitch = Mathf.Clamp(-vy * 0.9f + 6f, -14f, 16f);
            }

            if (model != null)
            {
                Quaternion want = Quaternion.Euler(pitch, tiltYaw, tiltRoll);
                model.localRotation = Quaternion.Slerp(model.localRotation, want, 1f - Mathf.Exp(-20f * dt));
                Vector3 lp = model.localPosition;
                float wantY = sliding ? -0.12f : 0f;
                lp.y = Mathf.Lerp(lp.y, wantY, 1f - Mathf.Exp(-20f * dt));
                lp.z = Mathf.Lerp(lp.z, sliding ? 0.25f : 0f, 1f - Mathf.Exp(-20f * dt));
                lp.x = 0f;
                model.localPosition = lp;
            }

            if (anim != null && anim.runtimeAnimatorController != null)
            {
                anim.SetFloat("Forward", running ? 1f : 0f, 0.08f, dt);
                anim.SetFloat("Turn", Mathf.Clamp(vx * 0.05f, -1f, 1f), 0.08f, dt);
                anim.SetBool("OnGround", grounded);
                anim.SetBool("Crouch", false);
                if (!grounded && !flying) anim.SetFloat("Jump", vy);
                float target = grounded ? 1.15f + (speed - 10f) * 0.048f : 1f;
                if (flying) target = 0.85f;
                if (sliding) target = 0.6f;
                animSpeed = Mathf.Lerp(animSpeed, running ? target : 1f, 1f - Mathf.Exp(-12f * dt));
                anim.speed = animSpeed;
            }
        }
    }
}
