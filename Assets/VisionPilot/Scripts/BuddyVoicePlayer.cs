using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(AudioSource))]
public class BuddyVoicePlayer : MonoBehaviour
{
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;
    }

    public void PlayFromUrl(string audioUrl)
    {
        StartCoroutine(PlayRoutine(audioUrl));
    }

    private IEnumerator PlayRoutine(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogWarning("[BuddyVoicePlayer] Audio URL is null/empty");
            yield break;
        }

        using (var req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            yield return req.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogError($"[BuddyVoicePlayer] Error downloading audio: {req.error}");
                yield break;
            }

            var clip = DownloadHandlerAudioClip.GetContent(req);
            if (clip == null)
            {
                Debug.LogError("[BuddyVoicePlayer] Audio clip is null");
                yield break;
            }

            audioSource.clip = clip;
            audioSource.Stop();
            audioSource.Play();
        }
    }
}
