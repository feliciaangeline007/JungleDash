// CameraFollow.cs – Smooth 3D follow camera with FOV boost support.
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Offset & Smoothing")]
    public Vector3 offset      = new Vector3(0f, 2.8f, -5.5f);
    public float smoothTime    = 0.12f;
    public float baseFOV       = 65f;
    public float farClip       = 130f;

    Camera _cam;
    Vector3 _vel;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        _cam.fieldOfView  = baseFOV;
        _cam.farClipPlane = farClip;
    }

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 desired = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref _vel, smoothTime);
        Vector3 lookAt = target.position + Vector3.up * 1f + Vector3.forward * 2f;
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(lookAt - transform.position), 8f * Time.deltaTime);
    }
}
