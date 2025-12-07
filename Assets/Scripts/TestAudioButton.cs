using UnityEngine;

public class TestAudioButton : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("[TestAudio] Playing test beep!");
            PlayTestBeep();
        }
    }

    void PlayTestBeep()
    {
        GameObject testObj = new GameObject("TestAudioSource");
        AudioSource testSource = testObj.AddComponent<AudioSource>();
        
        // Generate a 1-second beep at 440Hz (A note)
        int sampleRate = 44100;
        float frequency = 440f;
        float duration = 1f;
        int samples = (int)(sampleRate * duration);
        
        AudioClip beep = AudioClip.Create("TestBeep", samples, 1, sampleRate, false);
        float[] data = new float[samples];
        
        for (int i = 0; i < samples; i++)
        {
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * 0.5f;
        }
        
        beep.SetData(data, 0);
        
        testSource.clip = beep;
        testSource.volume = 1f;
        testSource.spatialBlend = 0f;
        testSource.Play();
        
        Debug.Log($"[TestAudio] Beep playing: isPlaying={testSource.isPlaying}, volume={testSource.volume}");
        
        Destroy(testObj, duration + 0.5f);
    }
}
