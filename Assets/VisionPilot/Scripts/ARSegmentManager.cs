using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ARSegmentManager : MonoBehaviour
{
    public string currentSegmentId = "page_1";
    public List<ARSegmentObjectGroup> groups;

    [Header("AI Integration")]
    [SerializeField] private SegmentAIController segmentAIController;

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

        UpdateGroups();

        if (segmentAIController != null)
        {
            segmentAIController.RequestAIForSegment(currentSegmentId, currentSegmentId);
        }
        else
        {
            Debug.LogWarning("[ARSegmentManager] segmentAIController not assigned.");
        }
    }

    private void UpdateGroups()
    {
        // TEMP MODE:
        // Do NOT hide any groups. Keep everything visible.
        // We still update currentSegmentId for the AI, but no SetActive toggling.
        var groups = FindObjectsOfType<ARSegmentObjectGroup>(includeInactive: true);

        foreach (var group in groups)
        {
            if (!group.gameObject.activeSelf)
                group.gameObject.SetActive(true);
        }
    }
}