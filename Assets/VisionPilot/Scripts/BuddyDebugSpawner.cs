using UnityEngine;
using System.Threading.Tasks;

public class BuddyDebugSpawner : MonoBehaviour
{
    public string testGlbUrl;      // paste a known GLB URL here
    public string buddyName = "Test Buddy";

    public bool spawnOnStart = true;

    private async void Start()
    {
        if (spawnOnStart && BuddySpawner.Instance != null && !string.IsNullOrEmpty(testGlbUrl))
        {
            await BuddySpawner.Instance.SpawnFromUrlAsync(testGlbUrl, buddyName);
        }
    }
}
