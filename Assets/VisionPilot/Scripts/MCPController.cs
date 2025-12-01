using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MCPController : MonoBehaviour
{
    public ObjectSelector objectSelector;

    // Example grid dimensions (set these appropriately)
    public int gridWidth = 10;
    public int gridHeight = 10;

    /// <summary>
    /// Selects a grid cell based on normalized coordinates (0-1 range)
    /// </summary>
    public void SelectFromNormalizedCoords(float normalizedX, float normalizedY)
    {
        // Clamp normalized coordinates to [0,1]
        normalizedX = Mathf.Clamp01(normalizedX);
        normalizedY = Mathf.Clamp01(normalizedY);

        // Convert normalized coordinates to grid indices
        int gridX = Mathf.RoundToInt(normalizedX * (gridWidth - 1));
        int gridY = Mathf.RoundToInt(normalizedY * (gridHeight - 1));

        // Clamp to valid grid bounds
        gridX = Mathf.Clamp(gridX, 0, gridWidth - 1);
        gridY = Mathf.Clamp(gridY, 0, gridHeight - 1);

        // Trigger selection at this grid position
        SelectCellAt(gridX, gridY);
    }

    // Stub: implement your grid selection logic here
    public void SelectCellAt(int x, int y)
    {
        Debug.Log($"[MCPController] Selected cell at ({x}, {y})");
        // TODO: Add your cell selection logic
    }

    // Fields that MCP's `update_component` tool can write to
    [Range(0f, 1f)] public float normalizedX;
    [Range(0f, 1f)] public float normalizedY;

    // Optional: simple debounce so it doesn't spam selection every frame
    private float _lastX = -1f;
    private float _lastY = -1f;

    private void Awake()
    {
        if (objectSelector == null)
        {
            objectSelector = FindObjectOfType<ObjectSelector>();
        }
    }

    private void Update()
    {
        // Only re-select if values actually changed
        if (Mathf.Approximately(normalizedX, _lastX) &&
            Mathf.Approximately(normalizedY, _lastY))
            return;

        _lastX = normalizedX;
        _lastY = normalizedY;

        if (objectSelector != null)
        {
            objectSelector.SelectAtNormalized(normalizedX, normalizedY);
        }
    }
}