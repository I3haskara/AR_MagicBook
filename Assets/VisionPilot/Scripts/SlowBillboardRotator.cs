using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlowBillboardRotator : MonoBehaviour
{
    public Camera targetCamera;
    public float rotationSpeed = 40f;

    void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    void Update()
    {
        if (targetCamera == null) return;

        // Face camera
        transform.rotation = Quaternion.LookRotation(
            (transform.position - targetCamera.transform.position).normalized,
            Vector3.up
        );

        // Slow spin
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
    }
}