using UnityEngine;

public class BuddyManager : MonoBehaviour
{
    public static BuddyManager Instance { get; private set; }

    [Header("Spawn Settings")]
    public Transform playerCamera;
    public float spawnDistance = 1.2f;
    public Vector3 spawnOffset = new Vector3(0, -0.2f, 0);

    [Header("Runtime")]
    public GameObject activeBuddy;

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

    public Vector3 GetSpawnPosition()
    {
        if (playerCamera == null)
        {
            var cam = Camera.main;
            if (cam != null) playerCamera = cam.transform;
        }

        if (playerCamera == null) return Vector3.zero;

        return playerCamera.position + playerCamera.forward * spawnDistance + spawnOffset;
    }

    public Quaternion GetSpawnRotation()
    {
        if (playerCamera == null)
        {
            var cam = Camera.main;
            if (cam != null) playerCamera = cam.transform;
        }

        if (playerCamera == null) return Quaternion.identity;
        return Quaternion.Euler(0f, playerCamera.eulerAngles.y, 0f);
    }

    public void SetActiveBuddy(GameObject buddy)
    {
        if (activeBuddy != null && activeBuddy != buddy)
        {
            Destroy(activeBuddy);
        }

        activeBuddy = buddy;
    }
}
