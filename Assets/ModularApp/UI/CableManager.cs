using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class CableManager : MonoBehaviour
{
    public static CableManager Instance { get; private set; }

    [Header("Prefab del cable visual")]
    public GameObject cableVisualPrefab;

    [Header("Contenedor de cables")]
    public RectTransform cableLayer;

    private CableVisual draggingCable;
    private PortUI draggingFromPort;

    public bool IsDraggingCable => draggingCable != null;

    private Dictionary<string, CableVisual> connectedCables
        = new Dictionary<string, CableVisual>();

    private static readonly Dictionary<PortType, Color> cableColors
        = new Dictionary<PortType, Color>
    {
        { PortType.AUDIO, new Color(0.9f, 0.9f, 0.9f) },
        { PortType.PITCHCV, new Color(1.0f, 0.8f, 0.1f) },
        { PortType.MODCV, new Color(0.3f, 0.8f, 1.0f) },
        { PortType.GATE, new Color(0.2f, 1.0f, 0.3f) },
        { PortType.TRIGGER, new Color(1.0f, 0.4f, 0.2f) },
    };

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (draggingCable == null) return;

        draggingCable.SetDragEnd(Input.mousePosition);

        PortUI hoveredPort = GetPortUnderCursor();
        UpdateHoverHighlight(hoveredPort);

        if (Input.GetMouseButtonUp(1))
        {
            if (hoveredPort != null && CanConnectTo(hoveredPort))
                EndCable(hoveredPort);
            else
                CancelCable();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
            CancelCable();
    }

    private PortUI hoveredPortLast;

    private void UpdateHoverHighlight(PortUI current)
    {
        if (current == hoveredPortLast) return;

        if (hoveredPortLast != null)
            hoveredPortLast.SetHighlight(draggingFromPort.port.CanConnectTo(hoveredPortLast.port));

        if (current != null)
            current.portCircle.color = CanConnectTo(current) ? Color.white : new Color(1f, 0.2f, 0.2f);

        hoveredPortLast = current;
    }

    private PortUI GetPortUnderCursor()
    {
        float detectRadius = 30f;
        PortUI closest = null;
        float closestDist = float.MaxValue;

        foreach (var portUI in FindObjectsByType<PortUI>(FindObjectsSortMode.None))
        {
            if (portUI == draggingFromPort) continue;
            if (portUI.port == null) continue;

            float dist = Vector2.Distance(Input.mousePosition, portUI.GetWorldPosition());
            if (dist < detectRadius && dist < closestDist)
            {
                closestDist = dist;
                closest = portUI;
            }
        }

        return closest;
    }


    public void BeginCable(PortUI fromPort)
    {
        if (draggingCable != null) CancelCable();

        draggingFromPort = fromPort;

        GameObject go = Instantiate(cableVisualPrefab, cableLayer);
        draggingCable = go.GetComponent<CableVisual>();

        Color color = cableColors.TryGetValue(fromPort.port.type, out Color c) ? c : Color.white;
        draggingCable.Initialize(fromPort, color);

        HighlightCompatiblePorts(fromPort, true);
    }


    public void EndCable(PortUI toPort)
    {
        if (draggingCable == null) return;

        HighlightCompatiblePorts(draggingFromPort, false);
        hoveredPortLast = null;

        if (!CanConnectTo(toPort)) { CancelCable(); return; }

        PortUI outputPort = draggingFromPort.port.dir == PortDir.OUTPUT
            ? draggingFromPort : toPort;
        PortUI inputPort = draggingFromPort.port.dir == PortDir.INPUT
            ? draggingFromPort : toPort;

        string fromModuleId = outputPort.moduleUI != null
            ? outputPort.moduleUI.module.moduleId
            : outputPort.port.owner.moduleId;

        string toModuleId = inputPort.moduleUI != null
            ? inputPort.moduleUI.module.moduleId
            : inputPort.port.owner.moduleId;

        var existing = PatchManager.Instance.GetCableForPort(toModuleId, inputPort.port.id);
        if (existing != null)
        {
            RemoveCableVisual(existing.id);
            PatchManager.Instance.DisconnectCable(existing.id);
        }

        var cableData = PatchManager.Instance.Connect(
            fromModuleId, outputPort.port.id,
            toModuleId, inputPort.port.id);

        if (cableData == null) { CancelCable(); return; }

        draggingCable.ConnectTo(toPort);
        draggingCable.cableDataId = cableData.id;
        connectedCables[cableData.id] = draggingCable;

        draggingCable = null;
        draggingFromPort = null;
    }

    public void DisconnectCablesAtPort(PortUI portUI)
    {
        string moduleId = portUI.port.owner.moduleId;
        string portId = portUI.port.id;

        var cablesAtPort = PatchManager.Instance.GetCables().FindAll(c =>
            (c.fromModuleId == moduleId && c.fromPortId == portId) ||
            (c.toModuleId == moduleId && c.toPortId == portId));

        foreach (var cable in cablesAtPort)
        {
            RemoveCableVisual(cable.id);
            PatchManager.Instance.DisconnectCable(cable.id);
        }

    }

    public void CancelCable()
    {
        if (draggingFromPort != null)
            HighlightCompatiblePorts(draggingFromPort, false);

        if (draggingCable != null)
            Destroy(draggingCable.gameObject);

        draggingCable = null;
        draggingFromPort = null;
    }

    public void UpdateCablesForModule(string moduleId)
    {
        var module = PatchManager.Instance.GetModule(moduleId);
        if (module == null) return;

        foreach (var cableData in PatchManager.Instance.GetCables())
        {
            if (cableData.fromModuleId == moduleId || cableData.toModuleId == moduleId)
            {
                if (connectedCables.TryGetValue(cableData.id, out CableVisual visual))
                    visual.RedrawLine();
            }
        }
    }


    public void RemoveCablesForModule(string moduleId)
    {
        var toRemove = PatchManager.Instance.GetCables().FindAll(c =>
            c.fromModuleId == moduleId || c.toModuleId == moduleId);

        foreach (var cable in toRemove)
            RemoveCableVisual(cable.id);
    }

    private void RemoveCableVisual(string cableId)
    {
        if (connectedCables.TryGetValue(cableId, out CableVisual visual))
        {
            Destroy(visual.gameObject);
            connectedCables.Remove(cableId);
        }
    }

    public void RemoveAllCables()
    {
        foreach (var visual in connectedCables.Values)
        {
            if (visual != null) Destroy(visual.gameObject);
        }
        connectedCables.Clear();
    }

    public void SpawnCableVisualForLoaded(PatchManager.CableData cable)
    {
        PortUI fromPortUI = FindPortUI(cable.fromModuleId, cable.fromPortId);
        PortUI toPortUI   = FindPortUI(cable.toModuleId,   cable.toPortId);

        if (fromPortUI == null || toPortUI == null)
        {
            //Debug.LogWarning($"[CableManager] No se encontró puerto para cable {cable.id}");
            return;
        }

        Color color = cableColors.TryGetValue(fromPortUI.port.type, out Color c) ? c : Color.white;

        GameObject go = Instantiate(cableVisualPrefab, cableLayer);
        CableVisual visual = go.GetComponent<CableVisual>();

        visual.Initialize(fromPortUI, color);
        visual.ConnectTo(toPortUI);
        visual.cableDataId = cable.id;

        connectedCables[cable.id] = visual;
    }

    private PortUI FindPortUI(string moduleId, string portId)
    {
        ModuleUI moduleUI = ModuleUIFactory.Instance.GetModuleUI(moduleId);
        if (moduleUI != null)
        {
            PortUI p = moduleUI.GetPortUI(portId);
            if (p != null) return p;
        }

        foreach (var portUI in FindObjectsByType<PortUI>(FindObjectsSortMode.None))
        {
            if (portUI.port?.owner?.moduleId == moduleId && portUI.port.id == portId)
                return portUI;
        }

        return null;
    }


    private void HighlightCompatiblePorts(PortUI fromPort, bool on)
    {
        foreach (var portUI in FindObjectsByType<PortUI>(FindObjectsSortMode.None))
        {
            if (portUI == fromPort) continue;
            portUI.SetHighlight(on && fromPort.port.CanConnectTo(portUI.port));
        }
    }

    public bool CanConnectTo(PortUI toPort)
    {
        if (draggingFromPort == null) return false;
        if (toPort == draggingFromPort) return false;
        return draggingFromPort.port.CanConnectTo(toPort.port);
    }
}