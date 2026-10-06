// CoinBob.cs – Makes a coin bob up and down and spin.
using UnityEngine;

public class CoinBob : MonoBehaviour
{
    public float bobAmp  = 0.18f;
    public float bobFreq = 2.0f;
    public float spinSpeed = 180f;

    float _baseY;
    float _timeOffset;

    void OnEnable()
    {
        _baseY = transform.position.y;
        _timeOffset = Random.value * Mathf.PI * 2f;
    }

    void Update()
    {
        Vector3 p = transform.position;
        p.y = _baseY + Mathf.Sin(Time.time * bobFreq * Mathf.PI * 2f + _timeOffset) * bobAmp;
        transform.position = p;
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f);
    }
}
