using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

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
                aiMessageUI.ShowMessage("Manual UI test message.", "debug");
            }
            else
            {
                Debug.LogWarning("[SegmentAI] aiMessageUI reference is NULL.");
            }
        }
    }

    public void SendTestRequest()
    {
        var payload = new SegmentContextPayload
        {
            segment_group_id = testSegmentId,
            label = testLabel
        };

        StartCoroutine(SendSegmentContext(payload));
    }

    public void RequestAIForSegment(string segmentGroupId, string label = "")
    {
        var payload = new SegmentContextPayload
        {
            segment_group_id = segmentGroupId,
            label = label
        };

        StartCoroutine(SendSegmentContext(payload));
    }

    private IEnumerator SendSegmentContext(SegmentContextPayload payload)
    {
        string json = JsonUtility.ToJson(payload);
        var url = $"{baseUrl}/ai/segment";
        Debug.Log("[SegmentAIController] Calling: " + url);

        var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[SegmentAI] Request failed: " + request.error);
            yield break;
        }

        string jsonResponse = request.downloadHandler.text;
        Debug.Log("[SegmentAI] Raw response: " + jsonResponse);

        var aiResponse = JsonUtility.FromJson<AIResponse>(jsonResponse);
        HandleAIResponse(aiResponse);
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
            aiMessageUI.ShowMessage(response.message, response.emotion);
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
