// CameraFollow.cs – Temple Run-style close behind-the-character camera
// Slightly low angle looking up, dramatic perspective, FOV boost on speed.
using UnityEngine;

namespace JungleDash
{
    public class CameraFollow : MonoBehaviour
    {
        // Temple Run camera: low behind, close, tilted slightly up
        public Vector3 offset        = new Vector3(0f, 3.2f, -5.5f);
        public float   lookAhead     = 6f;
        public float   lookUp        = 1.2f;
        public float   smoothTime    = 0.06f;
        public float   normalFOV     = 65f;
        public float   boostFOV      = 78f;
        public float   rollAmount    = 4f;   // slight roll when switching lanes

        private Vector3 vel;
        private float   shakeTimer;
        private float   shakeMag;
        private Camera  cam;
        private float   currentRoll;
        private float   targetRoll;

        public Transform target;

        private void Awake() => cam = GetComponent<Camera>();

        public void SetTarget(Transform t) => target = t;

        public void Shake(float duration, float magnitude)
        {
            shakeTimer = duration;
            shakeMag   = magnitude;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Smooth position follow
            Vector3 desired = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref vel, smoothTime);

            // Look at point slightly ahead and above player
            Vector3 lookAt = target.position + new Vector3(0f, lookUp, lookAhead);
            transform.LookAt(lookAt);

            // Screen shake
            if (shakeTimer > 0f)
            {
                transform.position += Random.insideUnitSphere * shakeMag;
                shakeTimer         -= Time.deltaTime;
            }

            // Lane-switch roll (makes game feel more dynamic)
            if (target.TryGetComponent<PlayerRunner>(out var player))
                targetRoll = (player.lane - 1) * -rollAmount;
            currentRoll = Mathf.Lerp(currentRoll, targetRoll, Time.deltaTime * 8f);
            Vector3 euler = transform.eulerAngles;
            euler.z       = currentRoll;
            transform.eulerAngles = euler;

            // Dynamic FOV
            if (cam != null)
            {
                bool boosting = PowerUpManager.Instance != null
                             && PowerUpManager.Instance.IsActive(PowerUpType.SpeedBoost);
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView,
                    boosting ? boostFOV : normalFOV, Time.deltaTime * 4f);
            }
        }
    }
}
