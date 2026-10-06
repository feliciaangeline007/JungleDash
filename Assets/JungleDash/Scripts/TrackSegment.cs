// TrackSegment.cs
// Describes one reusable segment of the track.
// Root is always at scale (1,1,1). Ground is a scaled CHILD.
// Extends along +Z from its origin.
using UnityEngine;

public class TrackSegment : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Geometry")]
    [Tooltip("World-space length of this segment along +Z.")]
    public float segmentLength = 30f;

    [Tooltip("Reference to the ground child that is scaled.")]
    public Transform groundChild;

    [Tooltip("Ground width (X).")]
    public float groundWidth = 6f;

    [Tooltip("Ground height/thickness (Y).")]
    public float groundThickness = 0.5f;

    // ─────────────────────────────────────────────
    //  Public helpers
    // ─────────────────────────────────────────────

    /// <summary>World-space Z position of this segment's end.</summary>
    public float EndZ => transform.position.z + segmentLength;

    /// <summary>
    /// Scales the ground child so root stays (1,1,1).
    /// Call this from the builder after setting segmentLength / groundWidth.
    /// </summary>
    public void ApplyGroundScale()
    {
        if (groundChild == null)
        {
            Debug.LogWarning("[TrackSegment] groundChild is null; cannot apply ground scale.");
            return;
        }
        // Root is scale 1 — apply dimensions directly to child
        groundChild.localScale = new Vector3(groundWidth, groundThickness, segmentLength);
        // Centre the child so segment origin is the segment START, ground centred in Z
        groundChild.localPosition = new Vector3(0f, -groundThickness * 0.5f, segmentLength * 0.5f);
    }

    /// <summary>Called by TrackSpawner when segment is recycled to a new position.</summary>
    public void ResetTo(Vector3 newOrigin)
    {
        transform.position = newOrigin;
        transform.rotation = Quaternion.identity;
    }
}
