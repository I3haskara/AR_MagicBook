using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectSelector : MonoBehaviour
{
    [Header("Selection")]
    public LayerMask selectionMask;        // Which layers can be selected
    public Material highlightMaterial;     // Material to apply to the selected object

    private Camera _cam;
    private GameObject _current;
    private Material _originalMaterial;

    void Awake()
    {
        _cam = Camera.main;
        if (_cam == null)
        {
            Debug.LogError("[ObjectSelector] No Main Camera found.");
        }
    }

    /// <summary>
    /// Called with normalized screen coords (0–1) from Python.
    /// (0,0) = bottom-left; (1,1) = top-right.
    /// </summary>
    public void SelectAtNormalized(float nx, float ny)
    {
        if (_cam == null) return;

        // convert normalized to pixel coords
        float px = nx * Screen.width;
        float py = ny * Screen.height;
        Vector3 screenPos = new Vector3(px, py, 0f);

        Ray ray = _cam.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, selectionMask))
        {
            SetCurrent(hit.collider.gameObject);
        }
        else
        {
            ClearSelection();
        }
    }

    private void SetCurrent(GameObject go)
    {
        if (_current == go) return;

        ClearSelection();

        _current = go;

        var renderer = _current.GetComponent<Renderer>();
        if (renderer != null && highlightMaterial != null)
        {
            _originalMaterial = renderer.sharedMaterial;
            renderer.material = highlightMaterial;
        }

        Debug.Log($"[ObjectSelector] Selected: {_current.name}");
    }

    public void ClearSelection()
    {
        if (_current == null) return;

        var renderer = _current.GetComponent<Renderer>();
        if (renderer != null && _originalMaterial != null)
        {
            renderer.material = _originalMaterial;
        }

        _current = null;
        _originalMaterial = null;
    }

    // TEMP: Mouse test so you can verify logic without Python
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouse = Input.mousePosition;
            float nx = mouse.x / Screen.width;
            float ny = mouse.y / Screen.height;
            SelectAtNormalized(nx, ny);
        }
    }
}