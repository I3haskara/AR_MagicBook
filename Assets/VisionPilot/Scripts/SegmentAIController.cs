using System.Collections;
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
public class Effects
{
    public bool highlight;
    public string emoji;
    public bool hologram;
}

[System.Serializable]
public class AIResponse
{
    public string segment_group_id;
    public string intent;
    public string action;
    public string message;
    public string emotion;
    public Effects effects;
}

public class SegmentAIController : MonoBehaviour
{
    [Header("Backend Settings")]
    [SerializeField]
    private string backendUrl = "http://127.0.0.1:8000/ai/segment";

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
        var request = new UnityWebRequest(backendUrl, "POST");
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

        if (response.effects != null)
        {
            Debug.Log(
                "[SegmentAI] effects: highlight=" + response.effects.highlight +
                " emoji=" + response.effects.emoji +
                " hologram=" + response.effects.hologram
            );
        }

        if (aiMessageUI != null && !string.IsNullOrEmpty(response.message))
        {
            aiMessageUI.ShowMessage(response.message, response.intent);
        }
    }
}
