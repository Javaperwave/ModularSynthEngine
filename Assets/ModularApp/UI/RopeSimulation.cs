using UnityEngine;
using System.Collections.Generic;

public class RopeSimulation
{
    // Configuración
    private int pointCount = 20;
    private float gravity = -2000f;
    private float damping = 0.85f;
    private int constraintIterations = 10;

    private float stiffness = 1f;
    private const float maxSlack = 5f;


    // Estado
    private Vector2[] positions;
    private Vector2[] prevPositions;
    private bool[] anchored;

    private float segmentLength;

    // Paso fijo y reposo
    private const float FIXED_DT = 1f / 60f;
    private float timeAccumulator = 0f;
    private float timeSinceLastMove = 0f;
    private bool isAtRest = false;

    public RopeSimulation(int points = 20, float stiffness = 0f)
    {
        pointCount = points;
        this.stiffness = Mathf.Clamp01(stiffness);
        positions = new Vector2[pointCount];
        prevPositions = new Vector2[pointCount];
        anchored = new bool[pointCount];

        // Anclar extremos
        anchored[0] = true;
        anchored[pointCount - 1] = true;
    }

    // Inicializar posiciones entre dos puntos
    public void Initialize(Vector2 start, Vector2 end)
    {
        float directDist = Vector2.Distance(start, end);
        float slack = maxSlack * (1f - stiffness);
        float ropeLength = directDist + slack;
        segmentLength = ropeLength / (pointCount - 1);

        for (int i = 0; i < pointCount; i++)
        {
            float t = i / (float)(pointCount - 1);
            positions[i] = Vector2.Lerp(start, end, t);
            prevPositions[i] = positions[i];
        }

        positions[0] = start;
        positions[pointCount - 1] = end;
        prevPositions[0] = start;
        prevPositions[pointCount - 1] = end;
    }

    // Actualizar posición de los anclajes
    public void SetAnchors(Vector2 start, Vector2 end)
    {
        if (Vector2.Distance(start, positions[0]) > 0.5f || Vector2.Distance(end, positions[pointCount - 1]) > 0.5f)
        {
            isAtRest = false;
            timeSinceLastMove = 0f;
        }

        positions[0] = start;
        prevPositions[0] = start;
        positions[pointCount - 1] = end;
        prevPositions[pointCount - 1] = end;
    }

    // Paso de simulación
    public void Step(float deltaTime)
    {
        timeSinceLastMove += deltaTime;

        // Entrar en reposo si los anclajes llevan quietos suficiente tiempo y los puntos no se mueven
        if (!isAtRest && timeSinceLastMove > 0.4f)
        {
            isAtRest = true;
            for (int i = 1; i < pointCount - 1; i++)
                if ((positions[i] - prevPositions[i]).magnitude > 0.05f) { isAtRest = false; break; }
        }
        if (isAtRest) return;

        // Paso fijo: acumular tiempo y ejecutar pasos de FIXED_DT
        timeAccumulator += Mathf.Min(deltaTime, 0.1f);
        while (timeAccumulator >= FIXED_DT)
        {
            float dt = FIXED_DT;
            float effectiveGravity = gravity * (1f - stiffness);

            // Integración de Verlet
            for (int i = 0; i < pointCount; i++)
            {
                if (anchored[i]) continue;

                Vector2 velocity = (positions[i] - prevPositions[i]) * damping;
                prevPositions[i] = positions[i];
                positions[i] += velocity;
                positions[i] += new Vector2(0, effectiveGravity * dt * dt);
            }

            // Resolver restricciones de distancia
            for (int iter = 0; iter < constraintIterations; iter++)
            {
                for (int i = 0; i < pointCount - 1; i++)
                {
                    Vector2 delta = positions[i + 1] - positions[i];
                    float dist = delta.magnitude;
                    if (dist < 0.001f) continue;

                    float diff = (dist - segmentLength) / dist;

                    if (!anchored[i] && !anchored[i + 1])
                    {
                        positions[i] += delta * 0.5f * diff;
                        positions[i + 1] -= delta * 0.5f * diff;
                    }
                    else if (anchored[i])
                    {
                        positions[i + 1] -= delta * diff;
                    }
                    else
                    {
                        positions[i] += delta * diff;
                    }
                }

                // Re-anclar extremos después de cada iteración
                positions[0] = prevPositions[0];
                positions[pointCount - 1] = prevPositions[pointCount - 1];
            }

            timeAccumulator -= FIXED_DT;
        }
    }

    public List<Vector2> GetPoints()
    {
        return new List<Vector2>(positions);
    }

    // Recalcular longitud al mover anclajes
    public void UpdateSegmentLength()
    {
        float directDist = Vector2.Distance(positions[0], positions[pointCount - 1]);
        float slack = maxSlack * (1f - stiffness);
        float ropeLength = directDist + slack;
        segmentLength = ropeLength / (pointCount - 1);
        isAtRest = false;
        timeSinceLastMove = 0f;
    }
}