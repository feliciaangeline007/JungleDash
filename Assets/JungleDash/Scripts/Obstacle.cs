// Obstacle.cs – Tags and optionally kills the player on contact.
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public Renderer obstacleRenderer;
    public ParticleSystem debrisParticles;

    Collider _col;
    void Awake() { _col = GetComponent<Collider>(); gameObject.tag = "Obstacle"; }

    public void Hide()
    {
        if (obstacleRenderer) obstacleRenderer.enabled = false;
        if (_col) _col.enabled = false;
    }

    public void Burst() { if (debrisParticles) debrisParticles.Play(); }

    public void ResetObstacle()
    {
        if (obstacleRenderer) obstacleRenderer.enabled = true;
        if (_col) _col.enabled = true;
    }

    void OnEnable() => ResetObstacle();
}
