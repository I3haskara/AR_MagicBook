using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

[System.Serializable]
public class SegmentContextPayload
{
    public string segment_group_id;
    public string label;
}

[System.Serializable]
public class AIResponse
{
    public string segment_group_id;
    public string intent;
    public string action;
    public string message;
    public string emotion;
    public Dictionary<string, bool> effects;
    public string model_url;
}

public class SegmentAIController : MonoBehaviour
{
    [Header("Selection")]
    [SerializeField] private ObjectSelector objectSelector;
    [SerializeField] private HologramSpawner hologramSpawner;

    [Header("Backend Settings")]
    [SerializeField]
    private string baseUrl = "http://127.0.0.1:8000";

    [Header("Test Settings")]
    [SerializeField]
    private string testSegmentId = "plant_01";
    [SerializeField]
    private string testLabel = "Peace Lily";

    [Header("UI")]
    [SerializeField] private AIMessageUI aiMessageUI;
    [SerializeField] private TextMeshProUGUI aiStatusText;

    [Header("Timeouts")]
    [SerializeField] private float aiRequestTimeoutSeconds = 20f;

    private HttpClient _httpClient;

    private void Awake()
    {
        _httpClient = new HttpClient();
    }

    private void OnDestroy()
    {
        _httpClient?.Dispose();
    }

    // TEMP: press T in Play mode to fire a test call
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("[SegmentAI] Sending test request...");
            SendTestRequest();
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            if (aiMessageUI != null)
            {
                aiMessageUI.ShowMessage(null, "Manual UI test message.", "debug");
            }
            else
            {
                Debug.LogWarning("[SegmentAI] aiMessageUI reference is NULL.");
            }
        }
    }

    public void SendTestRequest()
    {
        StartCoroutine(CallAISegmentCoroutine(testSegmentId, testLabel));
    }

    public void RequestAIForSegment(string segmentGroupId, string label = "")
    {
        StartCoroutine(CallAISegmentCoroutine(segmentGroupId, label));
    }

    private IEnumerator CallAISegmentCoroutine(string segmentGroupId, string label = "")
    {
        var task = CallAISegmentAsync(segmentGroupId, label);
        while (!task.IsCompleted)
        {
            yield return null;
        }
    }

    public async Task CallAISegmentAsync(string segmentGroupId, string label = "")
    {
        var payload = new SegmentContextPayload
        {
            segment_group_id = segmentGroupId,
            label = label
        };

        string json = JsonUtility.ToJson(payload);
        var url = $"{baseUrl}/ai/segment";

        if (aiStatusText != null)
        {
            aiStatusText.text = "Processing...";
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(aiRequestTimeoutSeconds));
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content, cts.Token);

            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            Debug.Log("[SegmentAI] Raw response: " + jsonResponse);

            var aiResponse = JsonUtility.FromJson<AIResponse>(jsonResponse);

            if (aiStatusText != null)
            {
                aiStatusText.text = "";
            }

            HandleAIResponse(aiResponse);
        }
        catch (TaskCanceledException ex)
        {
            Debug.LogWarning($"[SegmentAI] Request cancelled or timed out: {ex.Message}");
            if (aiStatusText != null)
            {
                aiStatusText.text = "AI timed out. Try again.";
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SegmentAI] Error calling AI: {ex}");
            if (aiStatusText != null)
            {
                aiStatusText.text = "AI error. Check server.";
            }
        }
    }

    private void HandleAIResponse(AIResponse response)
    {
        if (response == null)
        {
            Debug.LogError("[SegmentAI] Failed to parse AI response.");
            return;
        }

        Debug.Log(
            "[SegmentAI] intent=" + response.intent +
            " action=" + response.action +
            " message=\"" + response.message + "\""
        );

        if (aiMessageUI != null && !string.IsNullOrEmpty(response.message))
        {
            aiMessageUI.ShowMessage(null, response.message, response.emotion);
        }

        if (response.effects != null && response.effects.TryGetValue("highlight", out var highlight) && highlight)
        {
            TryHighlightCurrentObject();
        }

        if (response.effects != null && response.effects.TryGetValue("hologram", out var hologram) && hologram)
        {
            TrySpawnHologram(response);
        }
    }

    private void TryHighlightCurrentObject()
    {
        if (objectSelector == null)
        {
            Debug.LogWarning("[SegmentAIController] No ObjectSelector reference set.");
            return;
        }

        var selected = objectSelector.CurrentSelectedObject;
        if (selected == null)
        {
            Debug.LogWarning("[SegmentAIController] No current selected object to highlight.");
            return;
        }

        var effect = selected.GetComponent<HighlightEffect>();
        if (effect == null)
        {
            Debug.LogWarning($"[SegmentAIController] No HighlightEffect on {selected.name}.");
            return;
        }

        effect.PlayHighlight();
    }

    private void TrySpawnHologram(AIResponse response)
    {
        if (hologramSpawner == null)
        {
            Debug.LogWarning("[SegmentAIController] No HologramSpawner assigned.");
            return;
        }

        if (objectSelector == null)
        {
            Debug.LogWarning("[SegmentAIController] No ObjectSelector reference.");
            return;
        }

        var selected = objectSelector.CurrentSelectedObject;
        if (selected == null)
        {
            Debug.LogWarning("[SegmentAIController] No selected object to anchor hologram.");
            return;
        }

        hologramSpawner.SpawnHologram(
            selected.transform,
            response.segment_group_id,
            response.model_url
        );
    }
}
