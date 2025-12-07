using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(AudioSource))]
public class BuddyVoicePlayer : MonoBehaviour
{
    [Header("Audio Source that will play the buddy voice")]
    [SerializeField] private AudioSource audioSource;

    private void Awake()
    {
        // Auto-grab AudioSource on same GameObject if not set in Inspector
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Play an MP3/WAV clip from a URL (e.g. from backend TTS).
    /// </summary>
    public void PlayFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogWarning("[BuddyVoicePlayer] PlayFromUrl called with empty URL.");
            return;
        }

        Stop(); // stop any previous clip
        StartCoroutine(PlayFromUrlCoroutine(url));
    }

    private IEnumerator PlayFromUrlCoroutine(string url)
    {
        Debug.Log($"[BuddyVoicePlayer] Downloading audio from: {url}");

        using (var req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
#if UNITY_2020_1_OR_NEWER
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
#else
            yield return req.SendWebRequest();
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogError($"[BuddyVoicePlayer] Audio download error: {req.error}");
                yield break;
            }

            var clip = DownloadHandlerAudioClip.GetContent(req);
            if (clip == null)
            {
                Debug.LogError("[BuddyVoicePlayer] Downloaded audio clip is null.");
                yield break;
            }

            Debug.Log($"[BuddyVoicePlayer] Clip loaded: length={clip.length}s, channels={clip.channels}, freq={clip.frequency}");
            
            audioSource.clip = clip;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 1f;
            audioSource.priority = 0;
            audioSource.bypassEffects = true;
            audioSource.bypassListenerEffects = true;
            audioSource.bypassReverbZones = true;
            audioSource.Play();

            Debug.Log($"[BuddyVoicePlayer] Playing buddy voice. AudioSource.isPlaying={audioSource.isPlaying}, volume={audioSource.volume}, mute={audioSource.mute}, time={audioSource.time}/{clip.length}");
            
            // Monitor playback
            StartCoroutine(MonitorPlayback(clip.length));
        }
    }

    private IEnumerator MonitorPlayback(float clipLength)
    {
        float elapsed = 0f;
        while (elapsed < clipLength + 0.5f && audioSource != null)
        {
            if (audioSource.isPlaying)
            {
                Debug.Log($"[BuddyVoicePlayer] Still playing... time={audioSource.time:F2}s, isPlaying={audioSource.isPlaying}");
            }
            else
            {
                Debug.LogWarning($"[BuddyVoicePlayer] Audio stopped unexpectedly at {audioSource.time:F2}s!");
                break;
            }
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
        }
        Debug.Log("[BuddyVoicePlayer] Playback monitoring complete.");
    }

    public void Stop()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }
}