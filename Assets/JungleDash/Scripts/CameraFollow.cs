using UnityEngine;

namespace JungleDash
{
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 4.4f, -6.8f);
        public float lookAheadDistance = 14f;
        public float lookHeightOffset = 1.3f;
        public float smoothTime = 0.08f;
        public float normalFOV = 58f;
        public float boostFOV = 70f;

        private Vector3 velocity;
        private float shakeTimer;
        private float shakeMagnitude;
        private Camera cam;

        private void Awake() => cam = GetComponent<Camera>();

        public void SetTarget(Transform t) => target = t;

        public void Shake(float duration, float magnitude)
        {
            shakeTimer = duration;
            shakeMagnitude = magnitude;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desired = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);

            Vector3 lookAt = target.position + new Vector3(0f, lookHeightOffset, lookAheadDistance);
            transform.LookAt(lookAt);

            if (shakeTimer > 0f)
            {
                transform.position += Random.insideUnitSphere * shakeMagnitude;
                shakeTimer -= Time.deltaTime;
            }

            if (cam != null)
            {
                bool boosting = PowerUpManager.Instance != null && PowerUpManager.Instance.IsActive(PowerUpType.SpeedBoost);
                float targetFOV = boosting ? boostFOV : normalFOV;
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * 4f);
            }
        }
    }
}
