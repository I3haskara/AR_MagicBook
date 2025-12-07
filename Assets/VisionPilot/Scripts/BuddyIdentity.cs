using UnityEngine;

public class BuddyIdentity : MonoBehaviour
{
    [Header("Buddy Identity")]
    public string buddyId;
    public string buddyName;
    public string personality;

    public void Init(string id, string name, string persona)
    {
        buddyId = id;
        buddyName = name;
        personality = persona;
    }
}
