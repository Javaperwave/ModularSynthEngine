using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIBezierLine : MaskableGraphic
{
    public Color color = Color.white;
    public float lineWidth = 3f;
    public int segments = 30;

    private Vector2 p0, p1, p2, p3;

    public void SetPoints(Vector2 start, Vector2 cp1, Vector2 cp2, Vector2 end)
    {
        p0 = start; p1 = cp1; p2 = cp2; p3 = end;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        List<Vector2> points = new List<Vector2>();
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            points.Add(CubicBezier(t));
        }

        for (int i = 0; i < points.Count - 1; i++)
        {
            AddLineSegment(vh, points[i], points[i + 1], lineWidth, color);
        }
    }

    private Vector2 CubicBezier(float t)
    {
        float u = 1 - t;
        return u * u * u * p0
             + 3 * u * u * t * p1
             + 3 * u * t * t * p2
             + t * t * t * p3;
    }

    private void AddLineSegment(VertexHelper vh, Vector2 a, Vector2 b, float width, Color col)
    {
        Vector2 dir = (b - a).normalized;
        Vector2 perp = new Vector2(-dir.y, dir.x) * (width * 0.5f);

        RectTransform rt = GetComponent<RectTransform>();
        Vector2 al = ScreenToLocal(a, rt);
        Vector2 bl = ScreenToLocal(b, rt);
        Vector2 perpL = new Vector2(-((bl - al).normalized.y), (bl - al).normalized.x) * (width * 0.5f);

        int idx = vh.currentVertCount;

        UIVertex v = UIVertex.simpleVert;
        v.color = col;

        v.position = al - perpL; vh.AddVert(v);
        v.position = al + perpL; vh.AddVert(v);
        v.position = bl + perpL; vh.AddVert(v);
        v.position = bl - perpL; vh.AddVert(v);

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