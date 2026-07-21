using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIRopeRenderer : MaskableGraphic
{
    [Header("Visual")]
    public Color ropeColor = Color.white;
    public float lineWidth = 3f;

    // Puntos de la cuerda (en screen space)
    private List<Vector2> ropePoints = new List<Vector2>();

    public void SetRopePoints(List<Vector2> points)
    {
        ropePoints = points;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (ropePoints == null || ropePoints.Count < 2) return;

        RectTransform rt = GetComponent<RectTransform>();

        for (int i = 0; i < ropePoints.Count - 1; i++)
        {
            Vector2 a = ScreenToLocal(ropePoints[i], rt);
            Vector2 b = ScreenToLocal(ropePoints[i + 1], rt);
            AddSegment(vh, a, b);
        }
    }

    private void AddSegment(VertexHelper vh, Vector2 a, Vector2 b)
    {
        Vector2 dir = (b - a).normalized;
        Vector2 perp = new Vector2(-dir.y, dir.x) * (lineWidth * 0.5f);

        int idx = vh.currentVertCount;
        UIVertex v = UIVertex.simpleVert;
        v.color = ropeColor;

        v.position = a - perp; vh.AddVert(v);
        v.position = a + perp; vh.AddVert(v);
        v.position = b + perp; vh.AddVert(v);
        v.position = b - perp; vh.AddVert(v);

        vh.AddTriangle(idx, idx + 1, idx + 2);
        vh.AddTriangle(idx, idx + 2, idx + 3);
    }

    private Vector2 ScreenToLocal(Vector2 screenPoint, RectTransform rt)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rt, screenPoint, null, out Vector2 local);
        return local;
    }
}