// PowerUpHUD.cs
// Displays one icon + fill bar per power-up.
// Shown only when the ramp > 0. Alpha and fill follow ramp.
// No per-frame allocations (no new, no string concat).
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PowerUpHUD : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Per-entry data (serializable for Inspector)
    // ─────────────────────────────────────────────
    [System.Serializable]
    public class HUDEntry
    {
        [Tooltip("Root GameObject for this icon group (icon + bar).")]
        public GameObject root;
        [Tooltip("Icon Image component.")]
        public Image icon;
        [Tooltip("Filled Image for the shrinking bar (Image Type = Filled).")]
        public Image fillBar;
        [Tooltip("Text label (FLY, SPD, MAG, SHD, x2).")]
        public TextMeshProUGUI label;
    }

    [Header("HUD Entries (one per power-up, order matches PowerUpType enum)")]
    public HUDEntry[] entries = new HUDEntry[5];

    // ─────────────────────────────────────────────
    //  Internal: cache ramp values to avoid redraw
    // ─────────────────────────────────────────────
    private float[] _lastRamp = new float[5];

    // ─────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────
    private void Start()
    {
        // Hide all entries at start
        for (int i = 0; i < entries.Length; i++)
        {
            _lastRamp[i] = -1f; // force first update
            if (entries[i] != null && entries[i].root != null)
                entries[i].root.SetActive(false);
        }
    }

    private void Update()
    {
        if (PowerUpManager.Instance == null) return;

        UpdateEntry(0, PowerUpManager.Instance.MagnetRamp);
        UpdateEntry(1, PowerUpManager.Instance.ShieldRamp);
        UpdateEntry(2, PowerUpManager.Instance.SpeedBoostRamp);
        UpdateEntry(3, PowerUpManager.Instance.FlyRamp);
        UpdateEntry(4, PowerUpManager.Instance.DoubleScoreRamp);
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────
    private void UpdateEntry(int idx, float ramp)
    {
        if (idx >= entries.Length) return;
        HUDEntry e = entries[idx];
        if (e == null) return;

        // Only redraw when ramp changes meaningfully (avoids per-frame UI dirty)
        if (Mathf.Abs(ramp - _lastRamp[idx]) < 0.005f) return;
        _lastRamp[idx] = ramp;

        bool visible = ramp > 0.01f;
        if (e.root != null && e.root.activeSelf != visible)
            e.root.SetActive(visible);

        if (!visible) return;

        // Fill bar
        if (e.fillBar != null) e.fillBar.fillAmount = ramp;

        // Icon alpha
        if (e.icon != null)
        {
            Color c = e.icon.color;
            c.a     = Mathf.Clamp01(ramp);
            e.icon.color = c;
        }

        // Label alpha
        if (e.label != null)
        {
            Color c = e.label.color;
            c.a     = Mathf.Clamp01(ramp);
            e.label.color = c;
        }
    }
}
