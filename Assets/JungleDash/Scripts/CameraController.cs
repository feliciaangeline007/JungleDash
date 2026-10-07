// CameraController.cs - Cinematic chase camera with dynamic FOV, parallax, screen shake, and smooth transitions.
using UnityEngine;

namespace JungleDash
{
    public class CameraController : MonoBehaviour
    {
        public Camera Cam { get; private set; }

        Vector3 currentPos;
        Vector3 posVelocity;
        float shakeIntensity;
        float targetFOV = 60f;
        float currentFOV = 60f;
        float fovVelocity;
        
        // Cinematic
        float breathOffset;
        float speedDistortion;
        float hitFlashTimer;
        float transitionTimer;
        float menuOrbitAngle;
        
        // Dynamic tilt
        float currentTiltX;
        float currentTiltZ;

        public void Setup(Camera cameraComponent)
        {
            Cam = cameraComponent;
            if (Cam == null)
            {
                Cam = Camera.main;
                if (Cam == null)
                {
                    GameObject c = new GameObject("Main Camera");
                    c.tag = "MainCamera";
                    Cam = c.AddComponent<Camera>();
                }
            }

            if (Cam.GetComponent<AudioListener>() == null)
            {
                Cam.gameObject.AddComponent<AudioListener>();
            }

            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.nearClipPlane = 0.3f;
            Cam.farClipPlane = 180f;
            Cam.allowHDR = true;
            currentPos = Cam.transform.position;
            currentFOV = Cam.fieldOfView;
        }

        public void SetBackgroundColor(Color col)
        {
            if (Cam != null) Cam.backgroundColor = col;
        }

        public void AddShake(float amount)
        {
            shakeIntensity = Mathf.Clamp(shakeIntensity + amount, 0f, 1.5f);
        }

        public float HitFlashAlpha { get { return Mathf.Clamp01(hitFlashTimer / 0.15f); } }

        public void TriggerHitFlash()
        {
            hitFlashTimer = 0.15f;
        }

        public void Tick(float dt, Vector3 playerPos, bool isRunning, bool isFlying, bool isSpeedBoost, bool isDead)
        {
            if (Cam == null) return;

            breathOffset += dt * (isRunning ? 2.8f : 1.2f);
            float breathX = Mathf.Sin(breathOffset * 0.7f) * 0.04f;
            float breathY = Mathf.Sin(breathOffset) * 0.06f;

            // Target camera position
            Vector3 targetPos;
            if (isRunning)
            {
                float xOffset = playerPos.x * 0.48f;
                float yOffset = isFlying ? 5.2f : (playerPos.y * 0.42f + 3.4f);
                float zOffset = playerPos.z - (isSpeedBoost ? 6.8f : 5.6f);

                // Subtle parallax breathing
                xOffset += breathX;
                yOffset += breathY * 0.5f;

                targetPos = new Vector3(xOffset, yOffset, zOffset);
                targetFOV = isSpeedBoost ? 70f : (isFlying ? 65f : 60f);
                
                // Speed-based FOV push
                speedDistortion = Mathf.Lerp(speedDistortion, isSpeedBoost ? 4f : 0f, 1f - Mathf.Exp(-3f * dt));
                targetFOV += speedDistortion;
            }
            else if (isDead)
            {
                // Dramatic pull-back with slight orbit
                float deadOrbit = Mathf.Sin(Time.time * 0.5f) * 1.5f;
                targetPos = new Vector3(playerPos.x * 0.3f + deadOrbit, playerPos.y + 4.5f, playerPos.z - 7.5f);
                targetFOV = 54f;
            }
            else
            {
                // Menu stance - gentle orbit around character
                menuOrbitAngle += dt * 8f;
                float orbitX = Mathf.Sin(menuOrbitAngle * Mathf.Deg2Rad) * 0.3f;
                float orbitY = Mathf.Sin(menuOrbitAngle * 0.3f * Mathf.Deg2Rad) * 0.15f;
                targetPos = new Vector3(0.8f + orbitX, 1.8f + orbitY + breathY, -3.2f);
                targetFOV = 54f;
            }

            // Smooth position with critically-damped spring
            float followSpeed = isRunning ? 16f : 7f;
            currentPos = Vector3.Lerp(currentPos, targetPos, 1f - Mathf.Exp(-followSpeed * dt));

            // Apply screen shake with perlin noise for organic feel
            Vector3 shakeOffset = Vector3.zero;
            if (shakeIntensity > 0.001f)
            {
                float s = shakeIntensity;
                float t = Time.time;
                shakeOffset = new Vector3(
                    (Mathf.PerlinNoise(t * 38f, 0f) - 0.5f) * s * 0.9f,
                    (Mathf.PerlinNoise(0f, t * 38f) - 0.5f) * s * 0.9f,
                    (Mathf.PerlinNoise(t * 25f, t * 25f) - 0.5f) * s * 0.3f
                );
                shakeIntensity = Mathf.Lerp(shakeIntensity, 0f, 1f - Mathf.Exp(-9f * dt));
            }

            Cam.transform.position = currentPos + shakeOffset;

            // Look rotation with dynamic tilt
            float targetTiltZ = 0f;
            float targetTiltX = 0f;
            
            if (isRunning || isDead)
            {
                Vector3 lookTarget = playerPos + new Vector3(0f, isFlying ? 1.6f : 1.2f, isDead ? 0f : 4.5f);
                Quaternion wantRot = Quaternion.LookRotation(lookTarget - Cam.transform.position);
                
                // Add subtle tilt based on lateral movement
                targetTiltZ = -playerPos.x * 1.2f;
                targetTiltX = isFlying ? -2f : 0f;
                
                currentTiltZ = Mathf.Lerp(currentTiltZ, targetTiltZ, 1f - Mathf.Exp(-8f * dt));
                currentTiltX = Mathf.Lerp(currentTiltX, targetTiltX, 1f - Mathf.Exp(-6f * dt));
                
                Quaternion tilt = Quaternion.Euler(currentTiltX, 0f, currentTiltZ);
                Cam.transform.rotation = Quaternion.Slerp(Cam.transform.rotation, wantRot * tilt, 1f - Mathf.Exp(-18f * dt));
            }
            else
            {
                // Menu view look at hero
                Vector3 lookTarget = new Vector3(0f, 0.9f, 0f);
                Quaternion wantRot = Quaternion.LookRotation(lookTarget - Cam.transform.position);
                Cam.transform.rotation = Quaternion.Slerp(Cam.transform.rotation, wantRot, 1f - Mathf.Exp(-8f * dt));
            }

            // Smooth FOV with spring feel
            float fovDiff = targetFOV - currentFOV;
            fovVelocity += fovDiff * 80f * dt;
            fovVelocity *= Mathf.Exp(-8f * dt);
            currentFOV += fovVelocity * dt;
            Cam.fieldOfView = currentFOV;

            // Hit flash (chromatic-like subtle red tint via background)
            if (hitFlashTimer > 0f)
            {
                hitFlashTimer -= dt;
            }
        }

        public void SnapTo(Vector3 pos, Quaternion rot)
        {
            if (Cam == null) return;
            currentPos = pos;
            Cam.transform.position = pos;
            Cam.transform.rotation = rot;
        }
    }
}
