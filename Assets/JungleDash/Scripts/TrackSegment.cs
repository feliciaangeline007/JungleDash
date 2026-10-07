// TrackSegment.cs – One reusable piece of the endless track.
using UnityEngine;

public class TrackSegment : MonoBehaviour
{
    [Header("Geometry")]
    public float segmentLength   = 30f;
    public float groundWidth     = 6f;
    public float groundThickness = 0.5f;
    public Transform groundChild;   // The scaled child cube

    public float EndZ => transform.position.z + segmentLength;

    public void ApplyGroundScale()
    {
        if (groundChild == null) return;
        groundChild.localScale    = new Vector3(groundWidth, groundThickness, segmentLength);
        groundChild.localPosition = new Vector3(0f, -groundThickness * 0.5f, segmentLength * 0.5f);
    }

    public void ResetTo(Vector3 pos)
    {
        transform.position = pos;
        transform.rotation = Quaternion.identity;
    }
}
