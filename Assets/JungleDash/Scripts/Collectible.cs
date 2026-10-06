// Collectible.cs
// Shared script for coins and gems.
// Registers itself with PowerUpManager for magnet pull.
// Spins in place. No per-frame allocations.
using UnityEngine;

public class Collectible : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Value")]
    [Tooltip("Score points awarded when collected.")]
    public int points    = 10;
    [Tooltip("Number of coins this counts as (gems = 5).")]
    public int coinValue = 1;

    [Header("Visuals")]
    [Tooltip("Rotation speed (degrees/sec).")]
    public float spinSpeed = 120f;
    [Tooltip("Bob amplitude in world units.")]
    public float bobAmp    = 0.12f;
    [Tooltip("Bob frequency in Hz.")]
    public float bobFreq   = 1.5f;

    // ─────────────────────────────────────────────
    //  Internal
    // ─────────────────────────────────────────────
    private float _spawnY;
    private float _timeOffset;
    private bool  _collected;

    // ─────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────
    private void OnEnable()
    {
        _collected  = false;
        _spawnY     = transform.position.y;
        _timeOffset = Random.value * 6.28f; // desync bob
        if (PowerUpManager.Instance != null)
            PowerUpManager.Instance.RegisterCollectible(this);
    }

    private void OnDisable()
    {
        if (PowerUpManager.Instance != null)
            PowerUpManager.Instance.UnregisterCollectible(this);
    }

    private void Update()
    {
        if (_collected) return;
        // Spin
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        // Bob
        float y = _spawnY + Mathf.Sin(Time.time * bobFreq * Mathf.PI * 2f + _timeOffset) * bobAmp;
        Vector3 p = transform.position;
        p.y = y;
        transform.position = p;
    }

    // ─────────────────────────────────────────────
    //  Collection
    // ─────────────────────────────────────────────

    /// <summary>Called by PlayerRunner on trigger or by Magnet when close enough.</summary>
    public void Collect()
    {
        if (_collected) return;
        _collected = true;
        if (GameManager.Instance != null)
            GameManager.Instance.AddPickup(points, coinValue);
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) Collect();
    }
}
