using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ARSegmentObjectGroup : MonoBehaviour
{
    [Header("Segment ID (must match Python segment_id)")]
    public string segmentId;

    [Header("Fade settings")]
    public float fadeDuration = 0.5f;

    private Renderer[] _renderers;

    private void Awake()
    {
        // Grab all child renderers (even if disabled)
        _renderers = GetComponentsInChildren<Renderer>(true);
    }

    /// <summary>
    /// Instant toggle (if you still need it somewhere)
    /// </summary>
    public void SetActive(bool enabled)
    {
        if (_renderers == null) _renderers = GetComponentsInChildren<Renderer>(true);

        foreach (var r in _renderers)
        {
            if (r == null) continue;
            r.enabled = enabled;
        }

        gameObject.SetActive(enabled);
    }

    /// <summary>
    /// Smooth fade-in / fade-out of all child renderers.
    /// </summary>
    public void SetActiveSmooth(bool enabled)
    {
        if (_renderers == null) _renderers = GetComponentsInChildren<Renderer>(true);

        // Make sure group is active so we can fade in
        if (enabled && !gameObject.activeSelf)
            gameObject.SetActive(true);

        foreach (var r in _renderers)
        {
            if (r == null) continue;
            StartCoroutine(FadeRoutine(r, enabled));
        }
    }

    private IEnumerator FadeRoutine(Renderer r, bool enabled)
    {
        if (r == null) yield break;

        float start = enabled ? 0f : 1f;
        float end   = enabled ? 1f : 0f;

        // This makes an instance of the material per renderer (fine for demo)
        Material m = r.material;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alphaT = (fadeDuration > 0f) ? (t / fadeDuration) : 1f;
            float a = Mathf.Lerp(start, end, alphaT);

            Color c = m.color;
            c.a = a;
            m.color = c;

            yield return null;
        }

        // Snap to final
        Color finalC = m.color;
        finalC.a = end;
        m.color = finalC;

        // Disable renderer at the end of fade-out to avoid ghost interaction
        r.enabled = enabled;

        // Optionally: fully disable GameObject when faded out
        if (!enabled)
        {
            // Wait one frame or so if needed, but usually fine:
            gameObject.SetActive(false);
        }
    }
}