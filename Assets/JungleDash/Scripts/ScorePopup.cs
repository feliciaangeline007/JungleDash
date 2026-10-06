// ScorePopup.cs – Floating "+10" text that rises and fades.
using UnityEngine;
using TMPro;

public class ScorePopup : MonoBehaviour
{
    public float riseSpeed = 2f;
    public float lifetime  = 0.8f;

    TextMeshPro _tmp;
    float _timer;
    Color _startColor;

    void Awake() { _tmp = GetComponent<TextMeshPro>(); }

    public void Show(int pts, Vector3 worldPos)
    {
        transform.position = worldPos;
        _timer = lifetime;
        if (_tmp)
        {
            _tmp.SetText("+{0}", pts);
            _startColor = _tmp.color;
            _startColor.a = 1f;
            _tmp.color = _startColor;
        }
        gameObject.SetActive(true);
    }

    void Update()
    {
        _timer -= Time.deltaTime;
        transform.position += Vector3.up * riseSpeed * Time.deltaTime;
        if (_tmp)
        {
            Color c = _tmp.color;
            c.a = Mathf.Max(0f, _timer / lifetime);
            _tmp.color = c;
        }
        if (_timer <= 0f) gameObject.SetActive(false);
    }
}
