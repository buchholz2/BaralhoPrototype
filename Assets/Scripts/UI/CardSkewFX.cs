using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(Graphic))]
public class CardSkewFX : BaseMeshEffect
{
    [Range(0.8f, 1.3f)]
    public float topWidth = 1.05f;

    [Range(0f, 1f)]
    [Tooltip("Quanto da altura recebe o efeito. 1 = altura toda, 0.5 = so metade de cima.")]
    public float falloff = 1f;

    public void SetTopWidth(float value)
    {
        topWidth = value;
        if (graphic != null)
            graphic.SetVerticesDirty();
        else
            Debug.LogWarning($"CardSkewFX '{gameObject.name}': Graphic component nulo, nao e possivel aplicar skew.");
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive())
        {
            return;
        }
        
        if (vh == null)
        {
            Debug.LogWarning($"CardSkewFX '{gameObject.name}': VertexHelper nulo.");
            return;
        }

        var verts = new List<UIVertex>();
        vh.GetUIVertexStream(verts);
        if (verts.Count == 0) return;

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        for (int i = 0; i < verts.Count; i++)
        {
            var p = verts[i].position;
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
        }

        float height = maxY - minY;
        if (height <= 0.0001f) return;

        float centerX = (minX + maxX) * 0.5f;
        float startY = maxY - height * Mathf.Clamp01(falloff);

        for (int i = 0; i < verts.Count; i++)
        {
            var v = verts[i];
            float t = Mathf.InverseLerp(startY, maxY, v.position.y);
            float widthScale = Mathf.Lerp(1f, topWidth, t);
            float x = v.position.x - centerX;
            v.position.x = centerX + x * widthScale;
            verts[i] = v;
        }

        vh.Clear();
        vh.AddUIVertexTriangleStream(verts);
    }
}
