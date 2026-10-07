// Collectible.cs – Coin or gem that spins and bobs.
using UnityEngine;

public class Collectible : MonoBehaviour
{
    public float spinSpeed = 120f;
    public float bobAmp    = 0.12f;
    public float bobFreq   = 1.5f;

    float _baseY, _off;

    void OnEnable()  { _baseY = transform.position.y; _off = Random.value * 6.28f; }

    void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        Vector3 p = transform.position;
        p.y = _baseY + Mathf.Sin(Time.time * bobFreq * Mathf.PI * 2f + _off) * bobAmp;
        transform.position = p;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        bool isGem = gameObject.CompareTag("Gem");
        GameManager.Instance?.AddCoin(isGem ? 5 : 1);
        gameObject.SetActive(false);
    }
}
