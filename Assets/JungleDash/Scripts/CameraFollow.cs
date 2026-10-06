// CameraFollow.cs – Smooth 2D camera that follows the player horizontally only.
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Offset & Smoothing")]
    public Vector3 offset = new Vector3(3f, 1f, -10f);
    [Range(0.01f, 1f)] public float smoothTime = 0.15f;

    Vector3 _vel;

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 desired = target.position + offset;
        // Only follow X (horizontal), keep Y and Z fixed by offset
        desired.y = offset.y;
        desired.z = offset.z;
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref _vel, smoothTime);
    }
}
