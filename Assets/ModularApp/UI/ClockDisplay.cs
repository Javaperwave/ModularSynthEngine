using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class ClockDisplay : MaskableGraphic
{
    private Clock source;

    public Color backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);

    private static readonly Color triggerOn = new Color(1.00f, 0.40f, 0.20f, 1f);
    private static readonly Color triggerOff = new Color(0.22f, 0.10f, 0.05f, 1f);
    private static readonly Color gateOn = new Color(0.20f, 1.00f, 0.30f, 1f);
    private static readonly Color gateOff = new Color(0.05f, 0.22f, 0.07f, 1f);

    public float ledRadius = 4f;
    public int ledSegments = 16;

    private readonly bool[] lastStates = new bool[5];
    private bool firstDraw = true;


    public void SetSource(Clock module)
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

        bool s0 = source.ClockGateHigh;
        bool s1 = source.Div2GateHigh;
        bool s2 = source.Div4GateHigh;
        bool s3 = source.Div8GateHigh;
        bool s4 = source.IsRunning;

        if (firstDraw ||
            s0 != lastStates[0] || s1 != lastStates[1] ||
            s2 != lastStates[2] || s3 != lastStates[3] ||
            s4 != lastStates[4])
        {
            lastStates[0] = s0;
            lastStates[1] = s1;
            lastStates[2] = s2;
            lastStates[3] = s3;
            lastStates[4] = s4;
            firstDraw = false;
            SetVerticesDirty();
        }
    }


    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect r = rectTransform.rect;
        float w = r.width;
        float h = r.height;
        float x0 = r.xMin;
        float y0 = r.yMin;

        //Fondo
        AddQuad(vh,
            new Vector2(x0, y0),
            new Vector2(x0 + w, y0 + h),
            backgroundColor);

        if (source == null) return;

        float centerY = y0 + h * 0.5f;
        float slotW = w / 5f;

        for (int i = 0; i < 5; i++)
        {
            float cx = x0 + (i + 0.5f) * slotW;
            bool on = lastStates[i];

            Color col = (i < 4)
                ? (on ? triggerOn : triggerOff)
                : (on ? gateOn : gateOff);

            AddFilledCircle(vh, new Vector2(cx, centerY), ledRadius, ledSegments, col);
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

    private static void AddFilledCircle(VertexHelper vh, Vector2 center, float radius, int segments, Color c)
    {
        int centerIdx = vh.currentVertCount;
        UIVertex v = UIVertex.simpleVert; v.color = c;

        v.position = new Vector3(center.x, center.y, 0);
        vh.AddVert(v);

        for (int i = 0; i < segments; i++)
        {
            float a = (float)i / segments * Mathf.PI * 2f;
            v.position = new Vector3(
                center.x + Mathf.Cos(a) * radius,
                center.y + Mathf.Sin(a) * radius,
                0);
            vh.AddVert(v);
        }

        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            vh.AddTriangle(centerIdx, centerIdx + 1 + i, centerIdx + 1 + next);
        }
    }
}