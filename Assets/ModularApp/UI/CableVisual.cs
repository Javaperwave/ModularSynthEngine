using UnityEngine;
using System.Collections.Generic;

public class CableVisual : MonoBehaviour
{
    public PortUI fromPort;
    public PortUI toPort;
    public string cableDataId;

    [Range(0f, 1f)]
    public float stiffness = 0.5f;

    private UIRopeRenderer ropeRenderer;
    private RopeSimulation simulation;

    private Vector2 lastFromPos;
    private Vector2 lastToPos;
    private Vector2 dragEndPosition;

    private bool isConnected = false;

    public void Initialize(PortUI from, Color color)
    {
        fromPort = from;
        ropeRenderer = GetComponent<UIRopeRenderer>();
        ropeRenderer.ropeColor = color;

        simulation = new RopeSimulation(24, stiffness);

        Vector2 startPos = from.GetWorldPosition();
        simulation.Initialize(startPos, startPos + new Vector2(0, -100f));

        lastFromPos = startPos;
        lastToPos = startPos;
    }

    public void SetDragEnd(Vector2 screenPos)
    {
        dragEndPosition = screenPos;
    }

    public void ConnectTo(PortUI to)
    {
        toPort = to;
        isConnected = true;

        // Recalcular longitud para la nueva distancia
        simulation.UpdateSegmentLength();
    }

    private void Update()
    {
        Vector2 fromPos = fromPort.GetWorldPosition();
        Vector2 toPos = isConnected && toPort != null
            ? toPort.GetWorldPosition()
            : dragEndPosition;

        // Si los anclajes se han movido, actualizar longitud
        bool anchorsMoved =
            Vector2.Distance(fromPos, lastFromPos) > 0.5f ||
            Vector2.Distance(toPos, lastToPos) > 0.5f;

        if (anchorsMoved)
        {
            simulation.UpdateSegmentLength();
            lastFromPos = fromPos;
            lastToPos = toPos;
        }

        simulation.SetAnchors(fromPos, toPos);
        simulation.Step(Time.deltaTime);

        ropeRenderer.SetRopePoints(simulation.GetPoints());
    }

    public void RedrawLine()
    {}
}