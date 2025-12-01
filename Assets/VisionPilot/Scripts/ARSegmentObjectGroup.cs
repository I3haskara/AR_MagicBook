using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ARSegmentObjectGroup : MonoBehaviour
{
    [Header("The AR segment this group belongs to")]
    public string segmentId;

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }
}
