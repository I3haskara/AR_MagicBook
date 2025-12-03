using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectableHighlight : MonoBehaviour
{
    private Renderer[] renderers;
    private const float DIM_ALPHA = 0.5f;
    private const float FULL_ALPHA = 1.0f;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        SetDimmed();   // default 50% transparent on start
    }

    public void SetDimmed()
    {
        SetAlpha(DIM_ALPHA);
    }

    public void SetSelected()
    {
        SetAlpha(FULL_ALPHA);
    }

    private void SetAlpha(float alpha)
    {
        foreach (var r in renderers)
        {
            // use a material instance per renderer
            var mat = r.material;
            if (!mat.HasProperty("_Color")) continue;

            Color c = mat.color;
            c.a = alpha;
            mat.color = c;

            // make sure the material is in transparent mode
            if (alpha < 1f)
            {
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            else
            {
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                mat.SetInt("_ZWrite", 1);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.DisableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = -1;
            }
        }
    }
}
