// BackgroundScroller.cs – Parallax scrolling background layers.
// Attach to each background layer GameObject.
using UnityEngine;

public class BackgroundScroller : MonoBehaviour
{
    [Tooltip("Fraction of player speed this layer moves (0=static, 1=same as player).")]
    public float parallaxFactor = 0.3f;

    [Tooltip("Width of a single tile of this layer.")]
    public float tileWidth = 20f;

    Transform _cam;
    float _startX;

    void Start()
    {
        _cam = Camera.main != null ? Camera.main.transform : null;
        _startX = transform.position.x;
    }

    void LateUpdate()
    {
        if (_cam == null) return;
        float dist = _cam.position.x * parallaxFactor;
        float x = _startX + dist;

        // Tile seamlessly
        float mod = Mathf.Repeat(x, tileWidth) - tileWidth * 0.5f;
        Vector3 p = transform.position;
        p.x = mod;
        transform.position = p;
    }
}
