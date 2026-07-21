using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
//using UnityEditor.Experimental.GraphView;

public class ModuleUI : MonoBehaviour
{
    [Header("Referencias")]
    public TextMeshProUGUI titleLabel;
    public Image headerImage;
    public Image bodyImage;
    public Button deleteButton;
    public RectTransform dragHandle;
    public RectTransform inputPortsColumn;
    public RectTransform outputPortsColumn;

    [Header("Prefab de puerto")]
    public GameObject portUIPrefab;

    [Header("Prefabs de controles")]
    public GameObject knobPrefab;
    public GameObject togglePrefab;
    public GameObject dropdownPrefab;
    public GameObject sliderPrefab;
    public RectTransform bodyContainer;

    private int KnobsPerRow = 3;


    public Module module { get; private set; }

    private Dictionary<string, PortUI> portUIs = new Dictionary<string, PortUI>();
    private RectTransform rectTransform;

    private Outline headerSelectionOutline;
    private Outline bodySelectionOutline;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void Initialize(Module module, Color color)
    {
        this.module = module;
        titleLabel.text = module.moduleType;
        headerImage.color = color;
        bodyImage.color = new Color(color.r * 0.25f, color.g * 0.25f, color.b * 0.25f, 1f);

        deleteButton.onClick.AddListener(OnDeleteClicked);

        var dragHandler = dragHandle.gameObject.AddComponent<ModuleDragHandler>();
        dragHandler.Initialize(this);

        SpawnPortUIs();
        SpawnControls(); 
    }

    private void SpawnPortUIs()
    {
        foreach (var port in module.GetInputPorts())
        {
            GameObject go = Instantiate(portUIPrefab, inputPortsColumn);
            var portUI = go.GetComponent<PortUI>();
            portUI.Initialize(port, this);
            portUIs[port.id] = portUI;
        }

        foreach (var port in module.GetOutputPorts())
        {
            GameObject go = Instantiate(portUIPrefab, outputPortsColumn);
            var portUI = go.GetComponent<PortUI>();
            portUI.Initialize(port, this);
            portUIs[port.id] = portUI;
        }
    }

    private void SpawnControls()
    {
        var parameters = module.GetParameters();
        if (parameters == null || parameters.Count == 0) return;

        // Vertical layout para las filas
        var vertical = bodyContainer.gameObject.GetComponent<VerticalLayoutGroup>();
        if (vertical == null)
            vertical = bodyContainer.gameObject.AddComponent<VerticalLayoutGroup>();

        vertical.spacing = 2;
        vertical.padding = new RectOffset(3, 3, 3, 3);
        vertical.childAlignment = TextAnchor.UpperCenter;
        vertical.childControlWidth = true;
        vertical.childControlHeight = true;
        vertical.childForceExpandWidth = true;
        vertical.childForceExpandHeight = false;

        var csf = bodyContainer.gameObject.GetComponent<ContentSizeFitter>();
        if (csf == null)
            csf = bodyContainer.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Acumular knobs sueltos para meterlos en filas de KnobsPerRow
        var pendingKnobs = new List<ModuleParameter>();

        foreach (var param in parameters)
        {
            if (param.type == ParameterType.KNOB)
            {
                pendingKnobs.Add(param);
                if (pendingKnobs.Count >= KnobsPerRow)
                {
                    FlushKnobRow(pendingKnobs, bodyContainer);
                    pendingKnobs.Clear();
                }
            }
            else
            {
                if (pendingKnobs.Count > 0)
                {
                    FlushKnobRow(pendingKnobs, bodyContainer);
                    pendingKnobs.Clear();
                }

                if (param.type == ParameterType.GROUP)
                    SpawnGroup(param, bodyContainer);
                else if (param.type == ParameterType.DISPLAY)
                    SpawnDisplay(param, bodyContainer);
                else
                    SpawnSingleControl(param, bodyContainer);
            }
        }

        if (pendingKnobs.Count > 0)
            FlushKnobRow(pendingKnobs, bodyContainer);
    }

    private void FlushKnobRow(List<ModuleParameter> knobs, RectTransform parent)
    {
        GameObject rowGO = new GameObject("KnobRow", typeof(RectTransform));
        rowGO.transform.SetParent(parent, false);

        var row = rowGO.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 2;
        row.childAlignment = TextAnchor.UpperCenter;
        row.childControlWidth = false;
        row.childControlHeight = false;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = false;

        var rowLE = rowGO.AddComponent<LayoutElement>();
        rowLE.minHeight = 60;
        rowLE.preferredHeight = 60;

        foreach (var k in knobs)
        {
            GameObject go = Instantiate(knobPrefab, rowGO.GetComponent<RectTransform>());
            go.GetComponent<KnobUI>().Initialize(k);
        }
    }
    
    private void SpawnDisplay(ModuleParameter param, RectTransform parent)
    {
        GameObject go = new GameObject($"Display_{param.id}", typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = param.displayHeight;
        le.preferredHeight = param.displayHeight;
        le.flexibleWidth = 1;
        param.onDisplaySpawn?.Invoke(rect);

    }


    private void SpawnGroup(ModuleParameter groupParam, RectTransform parent)
    {
        GameObject rowGO = new GameObject($"Group_{groupParam.id}", typeof(RectTransform));
        rowGO.transform.SetParent(parent, false);

        var row = rowGO.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 1;
        row.childAlignment = TextAnchor.MiddleLeft;
        row.childControlWidth = false;
        row.childControlHeight = false;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = false;
        row.padding = new RectOffset(0, 0, 0, 0);

        var rowLE = rowGO.AddComponent<LayoutElement>();
        rowLE.minHeight = 20;
        rowLE.preferredHeight = 20;

        foreach (var child in groupParam.children)
            SpawnCompactControl(child, rowGO.GetComponent<RectTransform>());
    }

    private void SpawnSingleControl(ModuleParameter param, RectTransform parent)
    {
        if (param.hidden) return;

        GameObject prefab = param.type switch
        {
            ParameterType.TOGGLE => togglePrefab,
            ParameterType.DROPDOWN => dropdownPrefab,
            ParameterType.SLIDER => sliderPrefab,
            _ => null
        };

        if (prefab == null) return;

        GameObject go = Instantiate(prefab, parent);
        InitControl(go, param);

        /*
        switch (param.type)
        {
            case ParameterType.KNOB:
                go.GetComponent<KnobUI>().Initialize(param);
                break;
            case ParameterType.TOGGLE:
                go.GetComponent<ToggleUI>().Initialize(param);
                break;
            case ParameterType.DROPDOWN:
                go.GetComponent<DropdownUI>().Initialize(param);
                break;
        }
        */
    }

    private void SpawnCompactControl(ModuleParameter param, RectTransform parent)
    {
        if (param.hidden) return;
        
        GameObject prefab = param.type switch
        {
            ParameterType.KNOB => knobPrefab,
            ParameterType.TOGGLE => togglePrefab,
            ParameterType.DROPDOWN => dropdownPrefab,
            ParameterType.SLIDER => sliderPrefab,
            _ => null
        };
        if (prefab == null) return;

        GameObject go = Instantiate(prefab, parent);
        InitControl(go, param);

        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();

        switch (param.type)
        {
            case ParameterType.KNOB:
                go.GetComponent<RectTransform>().localScale = new Vector3(0.4f, 0.4f, 1f);
                le.minWidth = 22; le.preferredWidth = 22;
                le.minHeight = 20; le.preferredHeight = 20;
                break;

            case ParameterType.TOGGLE:
                go.GetComponent<RectTransform>().localScale = new Vector3(0.7f, 0.7f, 1f);
                le.minWidth = 38; le.preferredWidth = 38;
                le.minHeight = 20; le.preferredHeight = 20;
                break;

            case ParameterType.DROPDOWN:
                go.GetComponent<RectTransform>().localScale = new Vector3(0.75f, 0.75f, 1f);
                le.minWidth = 48; le.preferredWidth = 48;
                le.minHeight = 20; le.preferredHeight = 20;
                break;
            case ParameterType.SLIDER:
                le.minWidth = 60; le.preferredWidth = 60; le.flexibleWidth = 1;
                le.minHeight = 14; le.preferredHeight = 14;
                break;
        }
    }

    private void InitControl(GameObject go, ModuleParameter param)
    {
        switch (param.type)
        {
            case ParameterType.KNOB:
                go.GetComponent<KnobUI>().Initialize(param);
                break;
            case ParameterType.TOGGLE:
                go.GetComponent<ToggleUI>().Initialize(param);
                break;
            case ParameterType.DROPDOWN:
                go.GetComponent<DropdownUI>().Initialize(param);
                break;
            case ParameterType.SLIDER:
                go.GetComponent<SliderUI>().Initialize(param);
                break;
        }
    }


    public PortUI GetPortUI(string portId)
    {
        portUIs.TryGetValue(portId, out PortUI p);
        return p;
    }

    public Dictionary<string, PortUI> GetAllPortUIs() => portUIs;

    private void OnDeleteClicked()
    {
        SelectionManager.Instance?.NotifyModuleDestroyed(this);

        // Eliminar cables visuales primero
        CableManager.Instance.RemoveCablesForModule(module.moduleId);
        PatchManager.Instance.UnregisterModule(module.moduleId);
        ModuleUIFactory.Instance.RemoveModuleUI(module.moduleId);
        Destroy(module.gameObject);
        Destroy(gameObject);
    }

    public Vector2 GetPosition() => rectTransform.anchoredPosition;
    public void SetPosition(Vector2 pos) => rectTransform.anchoredPosition = pos;
    public void BringToFront() => transform.SetAsLastSibling();


    public void SetSelected(bool sel)
    {
        Color outlineColor = SelectionManager.Instance != null
            ? SelectionManager.Instance.selectionOutlineColor
            : new Color(1f, 0.85f, 0.1f, 1f);
        if (headerSelectionOutline == null && headerImage != null)
        {
            headerSelectionOutline = headerImage.gameObject.AddComponent<Outline>();
            headerSelectionOutline.effectDistance = new Vector2(3f, -3f);
        }
        if (bodySelectionOutline == null && bodyImage != null)
        {
            bodySelectionOutline = bodyImage.gameObject.AddComponent<Outline>();
            bodySelectionOutline.effectDistance = new Vector2(3f, -3f);
        }
        if (headerSelectionOutline != null)
        {
            headerSelectionOutline.effectColor = outlineColor;
            headerSelectionOutline.enabled = sel;
        }
        if (bodySelectionOutline != null)
        {
            bodySelectionOutline.effectColor = outlineColor;
            bodySelectionOutline.enabled = sel;
        }
    }

}