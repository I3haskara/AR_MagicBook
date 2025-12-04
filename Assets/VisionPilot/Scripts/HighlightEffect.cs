using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple glow/pulse highlight:
/// - Temporarily boosts material emission
/// - Works with built-in / URP Standard-like shaders
/// </summary>
public class HighlightEffect : MonoBehaviour
{
    [Header("Highlight Settings")]
    public Color highlightColor = new Color(0.1f, 0.8f, 1f, 1f);
    public float pulseDuration = 0.35f;
    public int pulseCount = 2;
    public float emissionIntensity = 2.0f;

    private Renderer[] renderers;
    private MaterialPropertyBlock mpb;
    private bool isHighlighting;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        mpb = new MaterialPropertyBlock();
    }

    /// <summary>
    /// Call this when AI says effects.highlight == true
    /// </summary>
    public void PlayHighlight()
    {
        if (!gameObject.activeInHierarchy) return;
        if (isHighlighting) return;

        StartCoroutine(HighlightRoutine());
    }

    private IEnumerator HighlightRoutine()
    {
        isHighlighting = true;

        // We will animate a 0→1→0 curve for each pulse
        for (int p = 0; p < pulseCount; p++)
        {
            float t = 0f;
            while (t < pulseDuration)
            {
                t += Time.deltaTime;
                float normalized = Mathf.Clamp01(t / pulseDuration);

                // 0 → 1 → 0 curve (PingPong style)
                float curve = 1f - Mathf.Abs(2f * normalized - 1f);

                ApplyEmission(curve);

                yield return null;
            }
        }

        // Clear highlight at end
        ApplyEmission(0f);
        isHighlighting = false;
    }

    private void ApplyEmission(float strength)
    {
        // strength: 0..1
        Color emission = highlightColor * (strength * emissionIntensity);

        foreach (var r in renderers)
        {
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_EmissionColor", emission);
            r.SetPropertyBlock(mpb);
        }
    }
}