using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ARSegmentManager : MonoBehaviour
{
    public string currentSegmentId = "page_1";
    private ARSegmentObjectGroup[] segmentGroups;
    private void Awake()
    {
        // Cache all groups in scene (even inactive ones)
        segmentGroups = FindObjectsOfType<ARSegmentObjectGroup>(includeInactive: true);

        // Apply initial state
        UpdateGroups();
    }

    public void SetSegment(string segmentId)
    {
        if (string.IsNullOrEmpty(segmentId))
            segmentId = "page_1";

        if (segmentId == currentSegmentId)
            return;

        currentSegmentId = segmentId;
        Debug.Log("[ARSegmentManager] Switched to segment: " + currentSegmentId);

        UpdateGroups();
    }

    private void UpdateGroups()
    {
         foreach (var group in segmentGroups)
        {
            if (group == null) continue;
            bool shouldBeActive = group.segmentId == currentSegmentId;
            group.SetActive(shouldBeActive);
        }
    }
}