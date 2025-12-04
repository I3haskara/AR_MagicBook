using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HologramVisualMode
{
    BlueprintOnly,
    RemoteModelIfAvailable   // C: future mode
}

public class HologramSpawner : MonoBehaviour
{
    [Header("Mode")]
    public HologramVisualMode mode = HologramVisualMode.BlueprintOnly;

    [Header("Blueprint Settings (B)")]
    public GameObject blueprintPrefab;
    public float heightOffset = 0.3f;
    public float scaleMultiplier = 1.0f;
    public float rotationSpeed = 40f;

    private readonly List<GameObject> _activeHolograms = new List<GameObject>();

    public void ClearAll()
    {
        foreach (var h in _activeHolograms)
        {
            if (h != null) Destroy(h);
        }
        _activeHolograms.Clear();
    }

    public void SpawnHologram(Transform anchor, string segmentId, string modelUrl)
    {
        if (anchor == null)
        {
            Debug.LogWarning("[HologramSpawner] No anchor transform provided.");
            return;
        }

        Vector3 spawnPos = anchor.position + Vector3.up * heightOffset;

        if (mode == HologramVisualMode.RemoteModelIfAvailable && !string.IsNullOrEmpty(modelUrl))
        {
            // C: hook real model loader here later
            StartCoroutine(SpawnRemoteOrFallback(spawnPos, anchor, modelUrl));
        }
        else
        {
            SpawnBlueprint(spawnPos, anchor);
        }
    }

    private void SpawnBlueprint(Vector3 position, Transform anchor)
    {
        if (blueprintPrefab == null)
        {
            Debug.LogWarning("[HologramSpawner] No blueprintPrefab assigned.");
            return;
        }

        var holo = Instantiate(blueprintPrefab, position, Quaternion.identity, this.transform);
        holo.transform.localScale *= scaleMultiplier;

        // Add simple billboard+spin
        var rotator = holo.AddComponent<SlowBillboardRotator>();
        rotator.targetCamera = Camera.main;
        rotator.rotationSpeed = rotationSpeed;

        _activeHolograms.Add(holo);
    }

    private IEnumerator SpawnRemoteOrFallback(Vector3 position, Transform anchor, string modelUrl)
    {
        Debug.Log("[HologramSpawner] (stub) Would download model from: " + modelUrl);

        // TODO: integrate glTF loader (Hyper3D / glTFast) here.
        // For now, we just spawn the blueprint so the demo never looks broken.
        SpawnBlueprint(position, anchor);
        yield break;
    }
}