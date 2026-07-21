using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class EnvelopeDisplay : MaskableGraphic
{
    private Envelope source;

    public float traceThickness = 1.5f;

    public Color backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
    public Color gridColor = new Color(0.25f, 0.25f, 0.30f, 1f);
    public Color phaseLineColor = new Color(0.18f, 0.18f, 0.22f, 1f);
    public Color traceColor = new Color(0.95f, 0.60f, 0.95f, 1f);

    [Range(0.05f, 0.5f)]
    public float sustainPortion = 0.20f;

    public int curveSegments = 24;


    private float lastAttack = -1f;
    private float lastDecay = -1f;
    private float lastSustain = -1f;
    private float lastRelease = -1f;
    private float lastACurve = -1f;
    private float lastDCurve = -1f;
    private float lastRCurve = -1f;


    public void SetSource(Envelope module)
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

        if (source.attackTime != lastAttack ||
            source.decayTime != lastDecay ||
            source.sustainLevel != lastSustain ||
            source.releaseTime != lastRelease ||
            source.AttackCurve != lastACurve ||
            source.DecayCurve != lastDCurve ||
            source.ReleaseCurve != lastRCurve)
        {
            lastAttack = source.attackTime;
            lastDecay = source.decayTime;
            lastSustain = source.sustainLevel;
            lastRelease = source.releaseTime;
            lastACurve = source.AttackCurve;
            lastDCurve = source.DecayCurve;
            lastRCurve = source.ReleaseCurve;

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

        //Background
        AddQuad(vh,
            new Vector2(x0, y0),
            new Vector2(x0 + w, y0 + h),
            backgroundColor);

        //Mesh
        AddHLine(vh, x0, x0 + w, y0 + h - 0.5f, 1f, gridColor);
        AddHLine(vh, x0, x0 + w, y0 + h * 0.5f, 1f, gridColor);
        AddHLine(vh, x0, x0 + w, y0 + 0.5f, 1f, gridColor);

        if (source == null) return;

        float A = source.attackTime;
        float D = source.decayTime;
        float R = source.releaseTime;
        float S = Mathf.Clamp01(source.sustainLevel);

        float aCurve = Mathf.Max(0.1f, source.AttackCurve);
        float dCurve = Mathf.Max(0.1f, source.DecayCurve);
        float rCurve = Mathf.Max(0.1f, source.ReleaseCurve);


        // El mínimo real del knob es 0.001f. Valores en ese umbral se tratan como 0
        // tanto para el cálculo de anchos como para el dibujado.
        const float INSTANT = 0.001f;
        float Av = (A <= INSTANT) ? 0f : A;
        float Dv = (D <= INSTANT) ? 0f : D;
        float Rv = (R <= INSTANT) ? 0f : R;

        float adrSum = Av + Dv + Rv;

        // A, D y R se reparten proporcionalmente el espacio no-sustain.
        // El sustain ocupa el espacio restante, expandiéndose hasta llenar
        // todo el display cuando A, D y R son 0.
        float availW = w * (1f - sustainPortion);

        float aWidth = (adrSum > 0f) ? (Av / adrSum) * availW : 0f;
        float dWidth = (adrSum > 0f) ? (Dv / adrSum) * availW : 0f;
        float rWidth = (adrSum > 0f) ? (Rv / adrSum) * availW : 0f;

        // El sustain toma todo el espacio que A, D y R no usan.
        float sWidth = w - aWidth - dWidth - rWidth;

        float topY = y0 + h - 1f;
        float botY = y0 + 1f;
        float plotH = topY - botY;

        float sustainY = botY + S * plotH;

        float xA = x0 + aWidth;
        float xD = xA + dWidth;
        float xS = xD + sWidth;
        AddVLine(vh, xA, y0, y0 + h, 1f, phaseLineColor);
        AddVLine(vh, xD, y0, y0 + h, 1f, phaseLineColor);
        AddVLine(vh, xS, y0, y0 + h, 1f, phaseLineColor);


        //ATTACK
        Vector2 prev = new Vector2(x0, botY);
        float startX = x0;

        if (A > INSTANT)
        {
            for (int i = 1; i <= curveSegments; i++)
            {
                float t = (float)i / curveSegments;
                float v = ExponentialAttack(t, aCurve);
                Vector2 curr = new Vector2(startX + t * aWidth, botY + v * plotH);
                AddLineSegment(vh, prev, curr, traceThickness, traceColor);
                prev = curr;
            }
        }
        else
        {
            // Fase instantánea: línea vertical de botY a topY
            AddLineSegment(vh, new Vector2(xA, botY), new Vector2(xA, topY), traceThickness, traceColor);
            prev = new Vector2(xA, topY);
        }

        //DECAY
        startX = xA;
        prev = new Vector2(startX, topY);

        if (D > INSTANT)
        {
            for (int i = 1; i <= curveSegments; i++)
            {
                float t = (float)i / curveSegments;
                float v = ExponentialDecay(t, dCurve, 1f, S);
                Vector2 curr = new Vector2(startX + t * dWidth, botY + v * plotH);
                AddLineSegment(vh, prev, curr, traceThickness, traceColor);
                prev = curr;
            }
        }
        else
        {
            // Fase instantánea: línea vertical de topY a sustainY
            AddLineSegment(vh, new Vector2(xD, topY), new Vector2(xD, sustainY), traceThickness, traceColor);
        }

        //SUSTAIN
        Vector2 sustainStart = new Vector2(xD, sustainY);
        Vector2 sustainEnd   = new Vector2(xS, sustainY);
        AddLineSegment(vh, sustainStart, sustainEnd, traceThickness, traceColor);

        //RELEASE
        startX = xS;
        prev = sustainEnd;

        if (R > INSTANT)
        {
            for (int i = 1; i <= curveSegments; i++)
            {
                float t = (float)i / curveSegments;
                float v = ExponentialDecay(t, rCurve, S, 0f);
                Vector2 curr = new Vector2(startX + t * rWidth, botY + v * plotH);
                AddLineSegment(vh, prev, curr, traceThickness, traceColor);
                prev = curr;
            }
        }
        else
        {
            // Fase instantánea: línea vertical de sustainY a botY
            AddLineSegment(vh, new Vector2(xS, sustainY), new Vector2(xS, botY), traceThickness, traceColor);
        }
    }


    private static float ExponentialAttack(float t, float curve)
    {
        //1 - e^(-t/curve) normalizada
        return (1f - Mathf.Exp(-t / curve)) / (1f - Mathf.Exp(-1f / curve));
    }

    private static float ExponentialDecay(float t, float curve, float from, float to)
    {
        float factor = (1f - Mathf.Exp(-t / curve)) / (1f - Mathf.Exp(-1f / curve));
        return Mathf.Lerp(from, to, factor);
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