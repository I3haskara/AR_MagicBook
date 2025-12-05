using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.IO;
using System;

[RequireComponent(typeof(Button))]
public class MicCommandController : MonoBehaviour
{
    public TextMeshProUGUI statusLabel;
    [Tooltip("FastAPI /voice_command endpoint URL")]
    public string voiceCommandUrl = "https://YOUR-RENDER-URL/voice_command";

    // Hook this into your existing systems:
    public AIMessageUI aiMessageUI;   // drag in inspector if you want HUD update

    private enum MicState { Idle, Listening, Processing }
    private MicState state = MicState.Idle;

    private AudioClip recordingClip;
    private string micDevice;
    private const int sampleRate = 16000;
    private float maxRecordSeconds = 15f;

    void Awake()
    {
        var btn = GetComponent<Button>();
        btn.onClick.AddListener(OnMicButtonPressed);
    }

    void Start()
    {
        SetState(MicState.Idle);
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

    void OnMicButtonPressed()
    {
        switch (state)
        {
            case MicState.Idle:
                StartRecording();
                break;
            case MicState.Listening:
                StopRecordingAndSend();
                break;
            case MicState.Processing:
                // ignore taps while processing
                break;
        }
    }

    void StartRecording()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("No microphone detected");
            return;
        }

        micDevice = Microphone.devices[0];
        recordingClip = Microphone.Start(micDevice, false, (int)maxRecordSeconds, sampleRate);
        SetState(MicState.Listening);
    }

    void StopRecordingAndSend()
    {
        if (!string.IsNullOrEmpty(micDevice))
        {
            Microphone.End(micDevice);
        }

        if (recordingClip == null)
        {
            SetState(MicState.Idle);
            return;
        }

        SetState(MicState.Processing);

        byte[] wavData = WavUtility.FromAudioClip(recordingClip, "voice_command");
        StartCoroutine(SendAudioToServer(wavData));
    }

    IEnumerator SendAudioToServer(byte[] wavData)
    {
        WWWForm form = new WWWForm();
        form.AddBinaryData("audio", wavData, "voice.wav", "audio/wav");

        using (UnityWebRequest www = UnityWebRequest.Post(voiceCommandUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Voice command error: " + www.error);
                if (statusLabel) statusLabel.text = "Error. Tap to retry.";
                SetState(MicState.Idle);
            }
            else
            {
                string json = www.downloadHandler.text;
                Debug.Log("Voice response: " + json);

                VoiceAIResponse resp = JsonUtility.FromJson<VoiceAIResponse>(json);

                // Update HUD if wired
                if (aiMessageUI != null && resp != null && !string.IsNullOrEmpty(resp.ai_text))
                {
                    aiMessageUI.ShowMessage(resp.ai_text, resp.action);
                }

                // Later: hook resp.effects + resp.model_url into your hologram / highlight system
                // Later: if resp.voice_b64 != null -> decode and play audio

                SetState(MicState.Idle);
            }
        }
    }

    [Serializable]
    public class VoiceAIResponse
    {
        public string user_text;
        public string ai_text;
        public string action;
        public Effects effects;
        public string model_url;
        public string voice_b64;
    }

    [Serializable]
    public class Effects
    {
        public bool highlight;
        public bool hologram;
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
