using System;
using System.Collections;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

public class SelectionPoller : MonoBehaviour
{
    [Header("FastAPI")]
    [Tooltip("GET endpoint that returns {\"x\": float, \"y\": float} in 0–1 range.")]
    public string serverUrl = "http://127.0.0.1:8000/selection";
    [Tooltip("Seconds between polls.")]
    public float pollInterval = 0.1f;

    [Header("Selection")]
    [Tooltip("ObjectSelector on the camera that should perform the highlight.")]
    public ObjectSelector objectSelector;

    private HttpClient _client;
    private bool _running;

    private float _lastX = -1f;
    private float _lastY = -1f;

    private float _nextErrorLogTime = 0f;
    private const float ERROR_LOG_COOLDOWN = 5f; // seconds

    [Serializable]
    private class SelectionData
    {
        public float x;
        public float y;
        // Extra fields (source, ts, etc.) from the server are ignored by JsonUtility
    }

    private void Awake()
    {
        _client = new HttpClient();
    }

    private void OnEnable()
    {
        _running = true;
        StartCoroutine(MainLoop());
    }

    private void OnDisable()
    {
        _running = false;
    }

    private void OnDestroy()
    {
        _client?.Dispose();
    }

    private IEnumerator MainLoop()
    {
        // 1) Health check
        yield return HealthCheckCoroutine();

        // 2) Poll loop
        yield return PollLoopCoroutine();
    }

    private IEnumerator HealthCheckCoroutine()
    {
        string healthUrl = serverUrl.Replace("/selection", "/health");

        Task<HttpResponseMessage> t = _client.GetAsync(healthUrl);
        while (!t.IsCompleted)
            yield return null;

        if (t.Result.IsSuccessStatusCode)
        {
            Debug.Log("[SelectionPoller] Selection server healthy.");
        }
        else
        {
            Debug.LogWarning("[SelectionPoller] Selection server NOT healthy: " + t.Result.StatusCode);
        }
    }

    private IEnumerator PollLoopCoroutine()
    {
        while (_running)
        {
            Task pollTask = PollOnceAsync();
            while (!pollTask.IsCompleted)
                yield return null;

            yield return new WaitForSeconds(pollInterval);
        }
    }

    private async Task PollOnceAsync()
    {
        try
        {
            var resp = await _client.GetAsync(serverUrl);
            if (!resp.IsSuccessStatusCode)
                return;

            var json = await resp.Content.ReadAsStringAsync();
            var data = JsonUtility.FromJson<SelectionData>(json);
            if (data == null)
                return;

            // Clamp to [0,1] just in case
            float nx = Mathf.Clamp01(data.x);
            float ny = Mathf.Clamp01(data.y);

            // Skip if nothing changed
            if (Mathf.Approximately(nx, _lastX) &&
                Mathf.Approximately(ny, _lastY))
                return;

            _lastX = nx;
            _lastY = ny;

            Debug.Log($"[SelectionPoller] Got selection x={nx:F3}, y={ny:F3}");

            if (objectSelector != null)
            {
                objectSelector.SelectAtNormalized(nx, ny);
            }
        }
        catch (Exception e)
        {
            if (Time.time >= _nextErrorLogTime)
            {
                Debug.LogWarning($"[SelectionPoller] Poll error: {e.Message}");
                _nextErrorLogTime = Time.time + ERROR_LOG_COOLDOWN;
            }
        }
    }
}