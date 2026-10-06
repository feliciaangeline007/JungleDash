// PowerUpPickup.cs
// A floating, bobbing, spinning pickup that activates a power-up on collection.
// No per-frame allocations.
using UnityEngine;
using TMPro;

public class PowerUpPickup : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Type")]
    [Tooltip("Which power-up this pickup grants.")]
    public PowerUpType powerUpType = PowerUpType.Magnet;

    [Header("Visuals")]
    [Tooltip("Spin speed in degrees/sec.")]
    public float spinSpeed = 90f;
    [Tooltip("Bob amplitude in world units.")]
    public float bobAmp    = 0.25f;
    [Tooltip("Bob frequency in Hz.")]
    public float bobFreq   = 1.2f;

    [Tooltip("The visual child to spin (leave null to spin the root).")]
    public Transform visualRoot;

    [Tooltip("Optional '×2' TMP label – should NOT be a child of the spinning visual.")]
    public TextMeshPro label;

    [Tooltip("Halo sphere renderer (translucent, no collider).")]
    public Renderer haloRenderer;

    // ─────────────────────────────────────────────
    //  Internal
    // ─────────────────────────────────────────────
    private float _spawnY;
    private float _timeOffset;
    private bool  _collected;
    private Transform _spinTarget;

    // ─────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────
    private void OnEnable()
    {
        _collected  = false;
        _spawnY     = transform.position.y;
        _timeOffset = Random.value * 6.28f;
        _spinTarget = visualRoot != null ? visualRoot : transform;
    }

    private void Update()
    {
        if (_collected) return;

        // Spin the visual
        _spinTarget.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);

        // Bob the root
        float y = _spawnY + Mathf.Sin(Time.time * bobFreq * Mathf.PI * 2f + _timeOffset) * bobAmp;
        Vector3 p = transform.position;
        p.y = y;
        transform.position = p;

        // Keep label upright (billboard) if it exists
        if (label != null && Camera.main != null)
            label.transform.LookAt(Camera.main.transform);
    }

    // ─────────────────────────────────────────────
    //  Collection
    // ─────────────────────────────────────────────

    /// <summary>Called by PlayerRunner on trigger enter.</summary>
    public void Collect()
    {
        if (_collected) return;
        _collected = true;
        if (PowerUpManager.Instance != null)
            PowerUpManager.Instance.Activate(powerUpType);
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) Collect();
    }
}
