using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

[Serializable]
public class Hyper3DGenerateRequest
{
    public string image_b64;
    public string name;
}

[Serializable]
public class Hyper3DGenerateResponse
{
    public int buddy_id;
    public string status;
    public string hyper_task_uuid;
    public string subscription_key;
}

[Serializable]
public class Hyper3DStatusResponse
{
    public int buddy_id;
    public string status;
    public string model_url;
}

public class BuddyCaptureController : MonoBehaviour
{
    [Header("Backend Settings")]
    [Tooltip("e.g. http://127.0.0.1:8000 or http://192.168.0.23:8000")]
    public string vpBrainBaseUrl = "http://127.0.0.1:8000";

    [Header("UI")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI hyper3DStatusText;

    [Header("Buddy")]
    public string defaultBuddyName = "Desk Buddy";

    [Header("Hyper3D Polling")]
    [Tooltip("Max seconds to wait for Hyper3D job before timing out.")]
    public float hyper3DTimeoutSeconds = 45f;
    [Tooltip("Seconds between Hyper3D status polls.")]
    public float hyper3DPollIntervalSeconds = 1.5f;

    private bool _isBusy = false;

    public void OnCaptureAndCreateBuddy()
    {
        if (_isBusy) return;
        StartCoroutine(CaptureAndSendRoutine());
    }

    private IEnumerator CaptureAndSendRoutine()
    {
        _isBusy = true;
        SetStatus("Capturing screenshot...");

        // Wait for end of frame so the render is done
        yield return new WaitForEndOfFrame();

        Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();
        if (tex == null)
        {
            SetStatus("Failed to capture screen.");
            _isBusy = false;
            yield break;
        }

        // Encode to PNG → base64
        byte[] pngBytes = tex.EncodeToJPG(); // JPG is fine for Hyper3D
        UnityEngine.Object.Destroy(tex);

        string imgB64 = Convert.ToBase64String(pngBytes);

        // Build request JSON
        var reqObj = new Hyper3DGenerateRequest
        {
            image_b64 = imgB64,
            name = defaultBuddyName
        };
        string json = JsonUtility.ToJson(reqObj);

        // POST /hyper3d/generate
        string url = $"{vpBrainBaseUrl}/hyper3d/generate";
        SetStatus("Sending to Hyper3D...");

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                SetStatus($"Error: {www.error}");
                _isBusy = false;
                yield break;
            }

            string respText = www.downloadHandler.text;
            Hyper3DGenerateResponse resp;
            try
            {
                resp = JsonUtility.FromJson<Hyper3DGenerateResponse>(respText);
            }
            catch (Exception e)
            {
                SetStatus("Parse error: " + e.Message);
                _isBusy = false;
                yield break;
            }

            SetStatus($"Hyper3D job submitted (buddy {resp.buddy_id})");
            yield return StartCoroutine(WaitForHyper3DAndSpawn(resp.buddy_id));
            _isBusy = false;
            yield break;
        }
    }

    private IEnumerator WaitForHyper3DAndSpawn(int buddyId)
    {
        string url = $"{vpBrainBaseUrl}/hyper3d/status/{buddyId}";
        float startTime = Time.time;
        int dots = 0;

        SetHyperStatus("Processing");

        while (true)
        {
            if (Time.time - startTime > hyper3DTimeoutSeconds)
            {
                Debug.LogWarning("[BuddyCaptureController] Hyper3D timed out.");
                SetHyperStatus("Generation timed out. Try again.");
                yield break;
            }

            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    SetHyperStatus($"Status error: {www.error}");
                    yield break;
                }

                string respText = www.downloadHandler.text;
                Hyper3DStatusResponse statusResp;
                try
                {
                    statusResp = JsonUtility.FromJson<Hyper3DStatusResponse>(respText);
                }
                catch (Exception e)
                {
                    SetHyperStatus("Status parse error: " + e.Message);
                    yield break;
                }

                if (statusResp == null)
                {
                    SetHyperStatus("Empty status response.");
                    yield break;
                }

                if (statusResp.status == "running" || statusResp.status == "submitted")
                {
                    dots = (dots + 1) % 4;
                    SetHyperStatus("Processing" + new string('.', dots), log: false);
                    yield return new WaitForSeconds(hyper3DPollIntervalSeconds);
                    continue;
                }

                if (statusResp.status == "failed")
                {
                    Debug.LogError("[BuddyCaptureController] Hyper3D failed.");
                    SetHyperStatus("Generation failed. Try again.");
                    yield break;
                }

                if (statusResp.status == "done")
                {
                    string modelUrl = statusResp.model_url;
                    Debug.Log($"[BuddyCaptureController] 3D buddy ready! URL: {modelUrl}");

                    if (string.IsNullOrEmpty(modelUrl))
                    {
                        SetHyperStatus("Job done but no model_url.");
                        yield break;
                    }

                    SetHyperStatus("Buddy ready!");
                    yield return SpawnBuddyFromUrlCoroutine(statusResp.model_url, defaultBuddyName);
                    yield break;
                }

                // Unexpected status, just bail
                SetHyperStatus("Unknown status: " + statusResp.status);
                yield break;
            }
        }
    }

    private void SetStatus(string msg, bool log = true)
    {
        if (statusText != null)
        {
            statusText.text = msg;
        }

        if (log)
        {
            Debug.Log("[BuddyCaptureController] " + msg);
        }
    }

    private void SetHyperStatus(string msg, bool log = true)
    {
        if (hyper3DStatusText != null)
        {
            hyper3DStatusText.text = msg;
        }

        SetStatus(msg, log);
    }

    // Called when Hyper3D finishes building the model.
    private async Task OnHyper3DJobCompleted(string glbUrl, string suggestedName)
    {
        string buddyName = string.IsNullOrEmpty(suggestedName) ? "My Buddy" : suggestedName;

        if (string.IsNullOrEmpty(glbUrl))
        {
            SetStatus("No model URL returned.");
            return;
        }

        SetStatus("3D Buddy ready. Spawning...");

        try
        {
            var spawner = BuddySpawner.Instance;
            if (spawner == null)
            {
                SetStatus("Buddy spawner not in scene.");
                return;
            }

            Debug.Log($"[BuddyCaptureController] Spawning buddy from {glbUrl} with name '{buddyName}'");
            var buddy = await spawner.SpawnFromUrlAsync(
                glbUrl,
                buddyName,
                voiceIdOverride: null,
                buddyId: null
            );

            if (buddy != null)
            {
                SetStatus($"{buddyName} spawned!");
            }
            else
            {
                SetStatus("Failed to spawn buddy.");
            }
        }
        catch (Exception e)
        {
            SetStatus("Spawn error: " + e.Message);
        }
    }

    private IEnumerator SpawnBuddyFromUrlCoroutine(string glbUrl, string suggestedName)
    {
        var spawnTask = OnHyper3DJobCompleted(glbUrl, suggestedName);
        while (!spawnTask.IsCompleted)
        {
            yield return null;
        }
    }
}
