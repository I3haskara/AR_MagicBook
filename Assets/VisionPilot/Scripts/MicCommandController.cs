using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.IO;
using System;

[RequireComponent(typeof(Button))]
public class MicCommandController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public TextMeshProUGUI statusLabel;
    [Tooltip("FastAPI /ai/voice_command endpoint URL (point to your real backend, not the stub)")]
    public string voiceCommandUrl = "http://127.0.0.1:8000/ai/voice_command";

    // Hook this into your existing systems:
    public AIMessageUI aiMessageUI;   // drag in inspector if you want HUD update
    public BuddyConversationController conversationController; // optional: handles bubble + TTS
    public BuddyVoicePlayer buddyVoicePlayer; // The buddy that will speak the response

    private enum MicState { Idle, Listening, Processing }
    private MicState state = MicState.Idle;

    private AudioClip recordingClip;
    private string micDevice;
    private const int sampleRate = 16000;
    private float maxRecordSeconds = 15f;

    void Start()
    {
        SetState(MicState.Idle);
        
        if (Microphone.devices.Length > 0)
        {
            Debug.Log($"[MicCommandController] Available microphones:");
            for (int i = 0; i < Microphone.devices.Length; i++)
            {
                Debug.Log($"  [{i}] {Microphone.devices[i]}");
            }
        }
        else
        {
            Debug.LogWarning("[MicCommandController] No microphones detected!");
        }
    }

    void Update()
    {
        if (state == MicState.Listening && recordingClip != null)
        {
            int micPos = Microphone.GetPosition(micDevice);
            if (micPos > 100 && statusLabel)
            {
                float[] samples = new float[128];
                int startPos = Mathf.Max(0, micPos - 128);
                recordingClip.GetData(samples, startPos);
                
                float sum = 0;
                for (int i = 0; i < samples.Length; i++)
                {
                    sum += Mathf.Abs(samples[i]);
                }
                float avgVolume = sum / samples.Length;
                
                if (avgVolume > 0.01f)
                {
                    statusLabel.text = $"Listening... 🎤 {Mathf.RoundToInt(avgVolume * 100)}%";
                }
            }
        }
    }

    void SetState(MicState newState)
    {
        state = newState;
        switch (state)
        {
            case MicState.Idle:
                if (statusLabel) statusLabel.text = "Tap to speak";
                break;
            case MicState.Listening:
                if (statusLabel) statusLabel.text = "Listening... tap to stop";
                break;
            case MicState.Processing:
                if (statusLabel) statusLabel.text = "Processing...";
                break;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (state == MicState.Idle)
        {
            StartRecording();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (state == MicState.Listening)
        {
            StopRecordingAndSend();
        }
    }

    public void StartRecording()
    {
        if (state != MicState.Idle)
        {
            return;
        }

        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("[MicCommandController] No microphone detected");
            if (statusLabel) statusLabel.text = "No mic found";
            return;
        }

        micDevice = Microphone.devices[0];
        Debug.Log($"[MicCommandController] Starting recording with mic: {micDevice}");
        recordingClip = Microphone.Start(micDevice, false, (int)maxRecordSeconds, sampleRate);
        
        if (recordingClip == null)
        {
            Debug.LogError("[MicCommandController] Failed to start microphone!");
            if (statusLabel) statusLabel.text = "Mic failed";
            return;
        }
        
        SetState(MicState.Listening);
    }

    public void StopRecordingAndSend()
    {
        if (state != MicState.Listening)
        {
            return;
        }

        int micPosition = Microphone.GetPosition(micDevice);
        Debug.Log($"[MicCommandController] Stopping recording. Mic position: {micPosition} samples");

        if (!string.IsNullOrEmpty(micDevice))
        {
            Microphone.End(micDevice);
        }

        if (recordingClip == null)
        {
            Debug.LogError("[MicCommandController] Recording clip is null!");
            SetState(MicState.Idle);
            return;
        }

        if (micPosition == 0)
        {
            Debug.LogWarning("[MicCommandController] No audio captured! Hold the button longer and speak.");
            if (statusLabel) statusLabel.text = "No audio - try again";
            SetState(MicState.Idle);
            return;
        }

        SetState(MicState.Processing);

        AudioClip trimmedClip = TrimAudioClip(recordingClip, micPosition);
        byte[] wavData = WavUtility.FromAudioClip(trimmedClip, "voice_command");
        Debug.Log($"[MicCommandController] Audio captured: {micPosition} samples, WAV size: {wavData.Length} bytes");
        StartCoroutine(SendAudioToServer(wavData));
    }

    AudioClip TrimAudioClip(AudioClip sourceClip, int samples)
    {
        if (samples >= sourceClip.samples)
            return sourceClip;

        float[] data = new float[samples * sourceClip.channels];
        sourceClip.GetData(data, 0);

        AudioClip trimmedClip = AudioClip.Create("TrimmedAudio", samples, sourceClip.channels, sourceClip.frequency, false);
        trimmedClip.SetData(data, 0);
        return trimmedClip;
    }

    IEnumerator SendAudioToServer(byte[] wavData)
    {
        Debug.Log($"[MicCommandController] Sending {wavData.Length} bytes to: {voiceCommandUrl}");
        
        // Verify audio data
        if (wavData == null || wavData.Length == 0)
        {
            Debug.LogError("[MicCommandController] WAV data is null or empty!");
            SetState(MicState.Idle);
            yield break;
        }
        
        WWWForm form = new WWWForm();
        form.AddBinaryData("audio_file", wavData, "voice.wav", "audio/wav");
        
        Debug.Log("[MicCommandController] Form created with field 'audio_file', filename 'voice.wav', content-type 'audio/wav'");

        using (UnityWebRequest www = UnityWebRequest.Post(voiceCommandUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[MicCommandController] Voice command error: {www.error}");
                Debug.LogError($"[MicCommandController] Response code: {www.responseCode}");
                
                // Log the actual error response from backend
                if (!string.IsNullOrEmpty(www.downloadHandler.text))
                {
                    Debug.LogError($"[MicCommandController] Backend error response: {www.downloadHandler.text}");
                }
                
                if (statusLabel) statusLabel.text = "Error. Tap to retry.";
                SetState(MicState.Idle);
            }
            else
            {
                string json = www.downloadHandler.text;
                Debug.Log($"[MicCommandController] Voice response: {json}");

                VoiceAIResponse resp = JsonUtility.FromJson<VoiceAIResponse>(json);

                if (resp != null && resp.success)
                {
                    Debug.Log($"[MicCommandController] Transcript: '{resp.transcript}'");
                    Debug.Log($"[MicCommandController] Reply: '{resp.reply_text}'");
                    
                    // Show AI message in UI
                    if (aiMessageUI != null && !string.IsNullOrEmpty(resp.reply_text))
                    {
                        aiMessageUI.ShowMessage(resp.transcript, resp.reply_text, "voice");
                    }

                    // Play voice response from audio_url
                    if (!string.IsNullOrEmpty(resp.audio_url))
                    {
                        string fullAudioUrl = $"http://127.0.0.1:8000{resp.audio_url}";
                        Debug.Log($"[MicCommandController] Playing audio from: {fullAudioUrl}");
                        
                        if (buddyVoicePlayer != null)
                        {
                            buddyVoicePlayer.PlayFromUrl(fullAudioUrl);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[MicCommandController] No audio_url in response!");
                    }
                }
                else
                {
                    Debug.LogError($"[MicCommandController] Request failed or invalid response. Success: {resp?.success}");
                    if (statusLabel) statusLabel.text = "Failed. Tap to retry.";
                }

                SetState(MicState.Idle);
            }
        }
    }

    [Serializable]
    public class VoiceAIResponse
    {
        public bool success;
        public string transcript;
        public string reply_text;
        public string audio_url;
    }
}

/// <summary>
/// Minimal WAV encoder for Unity AudioClip.
/// </summary>
public static class WavUtility
{
    const int HEADER_SIZE = 44;

    public static byte[] FromAudioClip(AudioClip clip, string name = "wav")
    {
        if (clip == null) throw new ArgumentNullException(nameof(clip));

        using (MemoryStream stream = new MemoryStream())
        {
            int sampleCount = clip.samples * clip.channels;
            int frequency = clip.frequency;
            float[] samples = new float[sampleCount];
            clip.GetData(samples, 0);

            // Reserve header
            for (int i = 0; i < HEADER_SIZE; i++)
                stream.WriteByte(0);

            short[] intData = new short[sampleCount];
            byte[] bytesData = new byte[sampleCount * 2];

            const float rescaleFactor = 32767f;
            for (int i = 0; i < sampleCount; i++)
            {
                intData[i] = (short)(samples[i] * rescaleFactor);
                byte[] byteArr = BitConverter.GetBytes(intData[i]);
                byteArr.CopyTo(bytesData, i * 2);
            }

            stream.Write(bytesData, 0, bytesData.Length);

            // Write header
            stream.Seek(0, SeekOrigin.Begin);
            WriteHeader(stream, clip, bytesData.Length);

            return stream.ToArray();
        }
    }

    static void WriteHeader(Stream stream, AudioClip clip, int dataLength)
    {
        int sampleRate = clip.frequency;
        short channels = (short)clip.channels;
        short bitsPerSample = 16;
        int byteRate = sampleRate * channels * bitsPerSample / 8;
        short blockAlign = (short)(channels * bitsPerSample / 8);
        int fileSize = HEADER_SIZE + dataLength - 8;

        using (BinaryWriter writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
        {
            writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
            writer.Write(fileSize);
            writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));

            writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write(channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write(blockAlign);
            writer.Write(bitsPerSample);

            writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
            writer.Write(dataLength);
        }
    }
}
