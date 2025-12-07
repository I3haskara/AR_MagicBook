using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class TTSResponse
{
    public string audio_url;
}

[System.Serializable]
public class TTSRequest
{
    public string text;
    public string voice_id;
    public string buddy_id;
}

public class BuddyConversationController : MonoBehaviour
{
    [Header("Backend")]
    public string backendBaseUrl = "http://127.0.0.1:8000";

    [Header("UI")]
    public AIMessageUI aiMessageUI;

    public IEnumerator HandleAiReply(string replyText, string intent = null)
    {
        ShowBubble(replyText, intent);
        yield return StartCoroutine(RequestTts(replyText));
    }

    private IEnumerator RequestTts(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            yield break;
        }

        var buddy = BuddyManager.Instance != null ? BuddyManager.Instance.activeBuddy : null;
        if (buddy == null)
        {
            Debug.LogWarning("[BuddyConversation] No active buddy for TTS");
            yield break;
        }

        var identity = buddy.GetComponent<BuddyIdentity>();
        var voiceId = identity != null && !string.IsNullOrEmpty(identity.voiceId)
            ? identity.voiceId
            : (BuddyManager.Instance != null ? BuddyManager.Instance.defaultVoiceId : null);

        var payload = new TTSRequest
        {
            text = text,
            voice_id = voiceId,
            buddy_id = identity != null ? identity.buddyId : null
        };

        string json = JsonUtility.ToJson(payload);
        string url = $"{backendBaseUrl}/tts";

        using (var req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogError($"[BuddyConversation] TTS error: {req.error}");
                yield break;
            }

            var jsonResp = req.downloadHandler.text;
            TTSResponse ttsResp;
            try
            {
                ttsResp = JsonUtility.FromJson<TTSResponse>(jsonResp);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[BuddyConversation] TTS parse error: " + e.Message);
                yield break;
            }

            if (ttsResp == null || string.IsNullOrEmpty(ttsResp.audio_url))
            {
                Debug.LogError("[BuddyConversation] Invalid TTS response");
                yield break;
            }

            var voicePlayer = buddy.GetComponent<BuddyVoicePlayer>();
            if (voicePlayer != null)
            {
                voicePlayer.PlayFromUrl(ttsResp.audio_url);
            }
            else
            {
                Debug.LogWarning("[BuddyConversation] BuddyVoicePlayer missing on active buddy.");
            }
        }
    }

    private void ShowBubble(string text, string intent = null)
    {
        if (aiMessageUI != null && !string.IsNullOrEmpty(text))
        {
            aiMessageUI.ShowMessage(null, text, intent);
        }
    }
}
