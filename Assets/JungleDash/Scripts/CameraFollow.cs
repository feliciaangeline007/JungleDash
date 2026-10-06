// CameraFollow.cs
// Smooth third-person camera that follows the player.
// Adjusts FOV based on SpeedBoost ramp. No per-frame allocations.
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Target")]
    [Tooltip("The player transform to follow.")]
    public Transform target;

    [Header("Offset")]
    [Tooltip("Offset from the target in local camera space.")]
    public Vector3 offset = new Vector3(0f, 2.8f, -5.5f);

    [Header("Smoothing")]
    [Tooltip("Position damping time (lower = tighter).")]
    public float positionDamping = 0.12f;
    [Tooltip("Rotation damping time.")]
    public float rotationDamping = 0.08f;

    [Header("FOV")]
    [Tooltip("Base field of view (degrees).")]
    public float baseFOV   = 65f;
    [Tooltip("Far clip plane.")]
    public float farClip   = 130f;

    // ─────────────────────────────────────────────
    //  Internal
    // ─────────────────────────────────────────────
    private Camera _cam;
    private Vector3  _velRef   = Vector3.zero; // SmoothDamp ref
    private float    _fovRef   = 0f;

    private void Awake()
    {
        _cam          = GetComponent<Camera>();
        _cam.farClipPlane = farClip;
        _cam.fieldOfView  = baseFOV;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Desired world position: target pos + world-space offset
        // We only follow Z and Y of the target (X is fixed at 0 — player lanes vary)
        Vector3 desiredPos = target.position + offset;

        // Smooth position
        transform.position = Vector3.SmoothDamp(
            transform.position, desiredPos, ref _velRef, positionDamping);

        // Always look slightly ahead of the player
        Vector3 lookTarget = target.position + Vector3.forward * 2f + Vector3.up * 1.2f;
        Quaternion desiredRot = Quaternion.LookRotation(lookTarget - transform.position);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, desiredRot, rotationDamping / Time.deltaTime * 0.01f);

        // FOV from speed boost
        float fovBonus = PowerUpManager.Instance != null
            ? PowerUpManager.Instance.FOVBonus : 0f;
        _cam.fieldOfView = Mathf.SmoothDamp(
            _cam.fieldOfView, baseFOV + fovBonus, ref _fovRef, 0.3f);
    }
}
