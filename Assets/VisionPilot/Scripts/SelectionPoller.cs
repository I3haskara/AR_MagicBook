using System;
using System.Collections;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

public class SelectionPoller : MonoBehaviour
{
    [Header("FastAPI")]
    public string serverUrl = "http://127.0.0.1:8000/selection";
    [Tooltip("Seconds between polls.")]
    public float pollInterval = 0.1f;

    [Header("Selection")]
    public ObjectSelector objectSelector;
    public ARSegmentManager arSegmentManager;   // <- assign in Inspector

    private HttpClient _client;
    private bool _running;

    private float _lastX = -1f;
    private float _lastY = -1f;
    private string _lastSegmentId = null;

    private float _nextErrorLogTime = 0f;
    private const float ERROR_LOG_COOLDOWN = 5f; // seconds

    [Serializable]
    private class SelectionData
    {
        public float x;
        public float y;
        public string source;
        public string segment_id;
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
        yield return HealthCheckCoroutine();
        yield return PollLoopCoroutine();
    }

    private IEnumerator HealthCheckCoroutine()
    {
        string healthUrl = serverUrl.Replace("/selection", "/health");

        Task<HttpResponseMessage> t = _client.GetAsync(healthUrl);
        while (!t.IsCompleted)
            yield return null;

        if (t.Result.IsSuccessStatusCode)
            Debug.Log("[SelectionPoller] Selection server healthy.");
        else
            Debug.LogWarning("[SelectionPoller] Selection server NOT healthy: " + t.Result.StatusCode);
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

            float nx = Mathf.Clamp01(data.x);
            float ny = Mathf.Clamp01(data.y);

            // Update AR segment first
            if (!string.IsNullOrEmpty(data.segment_id) && arSegmentManager != null)
            {
                arSegmentManager.SetSegment(data.segment_id);
            }

            // Dedup only if BOTH coords and segment are unchanged
            bool sameCoords = Mathf.Approximately(nx, _lastX) &&
                              Mathf.Approximately(ny, _lastY);
            bool sameSegment = string.Equals(
                data.segment_id ?? string.Empty,
                _lastSegmentId ?? string.Empty,
                StringComparison.Ordinal
            );

            if (sameCoords && sameSegment)
                return;

            _lastX = nx;
            _lastY = ny;
            _lastSegmentId = data.segment_id;

            string src = string.IsNullOrEmpty(data.source) ? "unknown" : data.source;
            string seg = string.IsNullOrEmpty(data.segment_id) ? "none" : data.segment_id;
            Debug.Log($"[SelectionPoller] Got selection x={nx:F3}, y={ny:F3}, src={src}, segment={seg}");

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