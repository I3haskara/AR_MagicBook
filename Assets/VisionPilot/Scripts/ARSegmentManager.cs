using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ARSegmentManager : MonoBehaviour
{
    public string currentSegmentId = "page_1";
    public List<ARSegmentObjectGroup> groups;
    private void Awake()
    {
        // If not assigned via Inspector, cache all groups in scene (even inactive ones)
        if (groups == null || groups.Count == 0)
        {
            var found = FindObjectsOfType<ARSegmentObjectGroup>(includeInactive: true);
            groups = new List<ARSegmentObjectGroup>(found);
        }

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

        // Update AR segment system if present using smooth transitions
        foreach (var group in groups)
        {
            if (group == null) continue;

            bool shouldBeActive = group.segmentId == segmentId;

            // NEW: smooth activation
            group.SetActiveSmooth(shouldBeActive);
        }
    }

    private void UpdateGroups()
    {
        foreach (var group in groups)
        {
            if (group == null) continue;
            bool shouldBeActive = group.segmentId == currentSegmentId;
            group.SetActiveSmooth(shouldBeActive);
        }
    }
}