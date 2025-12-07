using UnityEngine;

public class BuddyIdentity : MonoBehaviour
{
    [Header("Buddy Identity")]
    public string buddyId;       // from DB / backend if you have one
    public string buddyName;     // "Plant Buddy", "Desk Buddy", etc.
    public string personality;   // short description/persona
    public string voiceId;       // ElevenLabs voice id

    public void Init(string id, string name, string persona, string voice)
    {
        buddyId = id;
        buddyName = name;
        personality = persona;
        voiceId = voice;
    }
}
