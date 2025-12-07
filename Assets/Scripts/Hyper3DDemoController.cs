using System.Collections;
using UnityEngine;
using TMPro;

public class Hyper3DDemoController : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;                 // assign Main Camera
    public GameObject hyper3DPrefab;         // assign fake 3D result prefab
    public Transform spawnParent;            // optional, can be null
    public TextMeshProUGUI statusText;       // assign TMP UI text

    [Header("Timing")]
    public float capturePause = 0.4f;
    public float uploadPause = 0.8f;
    public float generatePause = 1.5f;

    private bool isProcessing;

    private void Awake()
    {
        if (!mainCamera)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        // Keyboard shortcut for quick testing
        if (Input.GetKeyDown(KeyCode.H))
        {
            RunHyper3DDemo();
        }
    }

    public void RunHyper3DDemo()
    {
        if (isProcessing) return;
        StartCoroutine(Hyper3DFlow());
    }

    private IEnumerator Hyper3DFlow()
    {
        isProcessing = true;

        // STEP 1 – Fake capture
        SetStatus("📸 Capturing image...");
        yield return new WaitForSeconds(capturePause);

        // STEP 2 – Fake upload
        SetStatus("☁ Uploading to Hyper3D...");
        yield return new WaitForSeconds(uploadPause);

        // STEP 3 – Fake processing
        SetStatus("🧠 Generating 3D model...");
        yield return new WaitForSeconds(generatePause);

        // STEP 4 – Spawn prefab where the camera is looking
        Vector3 spawnPos;
        Quaternion spawnRot;

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            spawnPos = hit.point;
            spawnRot = Quaternion.LookRotation(-ray.direction, Vector3.up);
        }
        else
        {
            spawnPos = mainCamera.transform.position + mainCamera.transform.forward * 2f;
            spawnRot = Quaternion.identity;
        }

        if (hyper3DPrefab != null)
        {
            Instantiate(hyper3DPrefab, spawnPos, spawnRot, spawnParent);
        }

        SetStatus("✅ Hyper3D model ready");
        yield return new WaitForSeconds(1.5f);
        SetStatus("");

        isProcessing = false;
    }

    private void SetStatus(string msg)
    {
        if (statusText != null)
            statusText.text = msg;
        Debug.Log("[Hyper3DDemo] " + msg);
    }
}
