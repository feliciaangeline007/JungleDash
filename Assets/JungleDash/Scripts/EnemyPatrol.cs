// EnemyPatrol.cs – Simple back-and-forth patrol on a platform.
using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    [Tooltip("How far left/right to patrol from spawn point.")]
    public float patrolRange = 1.2f;
    public float speed = 1.8f;

    Vector3 _origin;
    int     _dir = 1;
    SpriteRenderer _sr;

    void OnEnable()
    {
        _origin = transform.position;
        _dir = 1;
        _sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsRunning) return;

        transform.position += Vector3.right * (_dir * speed * Time.deltaTime);
        float dx = transform.position.x - _origin.x;
        if (dx >  patrolRange) _dir = -1;
        if (dx < -patrolRange) _dir =  1;
        if (_sr) _sr.flipX = _dir < 0;
    }
}
