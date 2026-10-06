// Obstacle.cs
// Marks an object as a fatal obstacle.
// Provides Hide() and TriggerDebris() for shield interaction.
// No per-frame logic needed.
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Visuals")]
    [Tooltip("The visual mesh renderer to hide on shield hit.")]
    public Renderer obstacleRenderer;
    [Tooltip("Debris particle system child to play on shield hit.")]
    public ParticleSystem debrisParticles;

    // ─────────────────────────────────────────────
    //  Internal
    // ─────────────────────────────────────────────
    private Collider _col;

    private void Awake()
    {
        _col = GetComponent<Collider>();
    }

    // ─────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────

    /// <summary>Hide the visual and disable the collider after a shield hit.</summary>
    public void Hide()
    {
        if (obstacleRenderer != null) obstacleRenderer.enabled = false;
        if (_col             != null) _col.enabled             = false;
    }

    /// <summary>Play the debris burst particle effect.</summary>
    public void TriggerDebris()
    {
        if (debrisParticles != null) debrisParticles.Play();
    }

    /// <summary>
    /// Called by TrackSpawner when this obstacle is recycled from pool.
    /// Restores visibility and collider so it can be reused.
    /// </summary>
    public void ResetObstacle()
    {
        if (obstacleRenderer != null) obstacleRenderer.enabled = true;
        if (_col             != null) _col.enabled             = true;
    }
}
