using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class OscilloscopeDisplay : MaskableGraphic
{
    private Oscilloscope source;

    private float[] snapshotBuffer;

    public float traceThickness = 1.5f;

    public Color backgroundColor = new Color(0.05f, 0.08f, 0.05f, 1f);
    public Color gridColor = new Color(0.25f, 0.3f, 0.25f, 1f);

    public void SetSource(Oscilloscope module)
    {
        source = module;
        SetVerticesDirty();
    }

    protected override void Awake()
    {
        base.Awake();
        color = Color.white;
    }

    void Update()
    {
        if (source == null) return;
        SetVerticesDirty();
    }


    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect r = rectTransform.rect;
        float w = r.width;
        float h = r.height;
        float x0 = r.xMin;
        float y0 = r.yMin;
        float centerY = y0 + h * 0.5f;

        //Background
        AddQuad(vh,
            new Vector2(x0, y0),
            new Vector2(x0 + w, y0 + h),
            backgroundColor);

        //Mesh
        AddHLine(vh, x0, x0 + w, centerY, 1f, gridColor);
        AddHLine(vh, x0, x0 + w, y0 + h - 0.5f, 1f, gridColor);
        AddHLine(vh, x0, x0 + w, y0 + 0.5f, 1f, gridColor);

        //Vertical mesh division
        for (int i = 1; i < 4; i++)
        {
            float gx = x0 + w * i / 4f;
            AddVLine(vh, gx, y0, y0 + h, 1f, gridColor);
        }

        //Wave
        if (source == null) return;

        int samplesToShow = source.timebase;
        if (samplesToShow < 2) samplesToShow = 2;

        int bigCount = samplesToShow * 2;
        if (snapshotBuffer == null || snapshotBuffer.Length < bigCount)
            snapshotBuffer = new float[bigCount];

        source.GetSnapshot(snapshotBuffer, bigCount);

        //FREE - offset 0 -> muestra los mas recientes a la derecha.
        //RISING: buscar cruce ascendente por 0 en la primera mitad del buffer.

        int offset = samplesToShow;

        if (source.triggerMode == Oscilloscope.TriggerMode.RISING)
        {
            int found = -1;
            for (int i = 1; i < samplesToShow; i++)
            {
                if (snapshotBuffer[i - 1] < 0f && snapshotBuffer[i] >= 0f) //wave cycle
                {
                    found = i;
                    break;
                }
            }

            if (found >= 0) offset = found;
        }

        float range = Mathf.Max(0.01f, source.range);
        float xStep = w / (samplesToShow - 1);

        Color trace = source.traceColor;

        Vector2 prev = new Vector2(
            x0,
            centerY + Mathf.Clamp(snapshotBuffer[offset] / range, -1f, 1f) * (h * 0.5f)
        );

        for (int i = 1; i < samplesToShow; i++)
        {
            Vector2 curr = new Vector2(
                x0 + i * xStep,
                centerY + Mathf.Clamp(snapshotBuffer[offset + i] / range, -1f, 1f) * (h * 0.5f)
            );

            AddLineSegment(vh, prev, curr, traceThickness, trace);
            prev = curr;
        }
    }


    private static void AddQuad(VertexHelper vh, Vector2 min, Vector2 max, Color c)
    {
        int i0 = vh.currentVertCount;
        UIVertex v = UIVertex.simpleVert; v.color = c;
        v.position = new Vector3(min.x, min.y, 0); vh.AddVert(v);
        v.position = new Vector3(min.x, max.y, 0); vh.AddVert(v);
        v.position = new Vector3(max.x, max.y, 0); vh.AddVert(v);
        v.position = new Vector3(max.x, min.y, 0); vh.AddVert(v);
        vh.AddTriangle(i0, i0 + 1, i0 + 2);
        vh.AddTriangle(i0, i0 + 2, i0 + 3);
    }

    private static void AddHLine(VertexHelper vh, float x0, float x1, float y, float thickness, Color c)
    {
        AddQuad(vh,
            new Vector2(x0, y - thickness * 0.5f),
            new Vector2(x1, y + thickness * 0.5f),
            c);
    }

    private static void AddVLine(VertexHelper vh, float x, float y0, float y1, float thickness, Color c)
    {
        AddQuad(vh,
            new Vector2(x - thickness * 0.5f, y0),
            new Vector2(x + thickness * 0.5f, y1),
            c);
    }

    private static void AddLineSegment(VertexHelper vh, Vector2 a, Vector2 b, float thickness, Color c)
    {
        Vector2 dir = b - a;
        float len = dir.magnitude;
        if (len < 0.0001f) return;
        dir /= len;

        Vector2 perp = new Vector2(-dir.y, dir.x) * thickness * 0.5f;

        int i0 = vh.currentVertCount;
        UIVertex v = UIVertex.simpleVert; v.color = c;
        v.position = a - perp; vh.AddVert(v);
        v.position = a + perp; vh.AddVert(v);
        v.position = b + perp; vh.AddVert(v);
        v.position = b - perp; vh.AddVert(v);
        vh.AddTriangle(i0, i0 + 1, i0 + 2);
        vh.AddTriangle(i0, i0 + 2, i0 + 3);
    }
}