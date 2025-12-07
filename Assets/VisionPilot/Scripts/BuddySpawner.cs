using System.Threading.Tasks;
using UnityEngine;
using GLTFast;   // only this namespace is needed

public class BuddySpawner : MonoBehaviour
{
    public static BuddySpawner Instance { get; private set; }

    [Header("Defaults")]
    public string defaultPersonality = "Friendly, curious desk companion.";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Spawns buddy from remote GLB URL.
    /// </summary>
    public async Task<GameObject> SpawnFromUrlAsync(
        string glbUrl,
        string buddyName,
        string voiceIdOverride = null,
        string buddyId = null)
    {
        if (string.IsNullOrEmpty(glbUrl))
        {
            Debug.LogError("[BuddySpawner] glbUrl is null/empty");
            return null;
        }

        Debug.Log($"[BuddySpawner] Loading GLB from: {glbUrl}");

        // 1) Load GLB with glTFast
        var gltf = new GltfImport();
        bool loaded = await gltf.Load(glbUrl);

        if (!loaded)
        {
            Debug.LogError("[BuddySpawner] Failed to load GLB");
            return null;
        }

        // 2) Create a root object for this buddy
        string finalName = string.IsNullOrEmpty(buddyName) ? "Buddy" : buddyName;
        var buddyRoot = new GameObject(finalName);

        // 3) Instantiate main scene under buddyRoot (use async API for safety)
        await gltf.InstantiateMainSceneAsync(buddyRoot.transform);

        // 4) Position buddy in front of camera
        var bm = BuddyManager.Instance;
        if (bm != null)
        {
            buddyRoot.transform.position = bm.GetSpawnPosition();
            buddyRoot.transform.rotation = bm.GetSpawnRotation();
            bm.SetActiveBuddy(buddyRoot);
        }

        // 5) Identity data
        var identity = buddyRoot.AddComponent<BuddyIdentity>();
        identity.Init(
            buddyId ?? System.Guid.NewGuid().ToString(),
            finalName,
            defaultPersonality,
            voiceIdOverride ?? (bm != null ? bm.defaultVoiceId : null)
        );

        // 6) AudioSource for voice
        var src = buddyRoot.GetComponent<AudioSource>();
        if (src == null)
        {
            src = buddyRoot.AddComponent<AudioSource>();
        }
        src.spatialBlend = 1f;
        src.playOnAwake = false;

        // 7) Optional: Voice player script (if you added it already)
        if (buddyRoot.GetComponent<BuddyVoicePlayer>() == null)
        {
            buddyRoot.AddComponent<BuddyVoicePlayer>();
        }

        Debug.Log("[BuddySpawner] Buddy spawned successfully");
        return buddyRoot;
    }
}
