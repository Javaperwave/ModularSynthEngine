using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class SelectionManager : MonoBehaviour,
    IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public static SelectionManager Instance { get; private set; }

    [Header("Apariencia caja de selección")]
    public Color selectionBoxFill   = new Color(0.2f, 0.6f, 1f, 0.18f);
    public Color selectionBoxBorder = new Color(0.2f, 0.6f, 1f, 0.9f);

    [Header("Resaltado de módulo seleccionado")]
    public Color selectionOutlineColor = new Color(1f, 0.85f, 0.1f, 1f);

    [Header("Pegado")]
    public Vector2 pasteOffset = new Vector2(30f, -30f);


    private RectTransform modulesContainer;

    private RectTransform selectionBoxRT;
    private Vector2 dragStartLocal;
    private bool isBoxSelecting;

    private List<ModuleUI> selected = new List<ModuleUI>();

    private List<ClipboardModule> clipboardModules = new List<ClipboardModule>();
    private List<ClipboardCable>  clipboardCables  = new List<ClipboardCable>();

    private class ClipboardModule
    {
        public string moduleId;
        public string moduleType;
        public Vector2 position;
        public List<ParamSaveData> parameters;
    }

    private class ClipboardCable
    {
        public string fromModuleId;
        public string fromPortId;
        public string toModuleId;
        public string toPortId;
    }


    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void Start()
    {
        if (ModuleUIFactory.Instance != null)
            modulesContainer = ModuleUIFactory.Instance.modulesContainer;
    }

    private void Update()
    {
        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        if (ctrl && Input.GetKeyDown(KeyCode.C))
            CopySelection();

        if (ctrl && Input.GetKeyDown(KeyCode.V))
            PasteFromClipboard();
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (CableManager.Instance != null && CableManager.Instance.IsDraggingCable) return;

        ClearSelection();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (modulesContainer == null) return;
        if (CableManager.Instance != null && CableManager.Instance.IsDraggingCable) return;

        isBoxSelecting = true;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            modulesContainer, eventData.position, eventData.pressEventCamera, out dragStartLocal);

        CreateSelectionBox();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isBoxSelecting) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            modulesContainer, eventData.position, eventData.pressEventCamera,
            out Vector2 currentLocal);

        UpdateSelectionBoxRect(dragStartLocal, currentLocal);
        UpdateSelectedModules(dragStartLocal, currentLocal);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isBoxSelecting) return;
        isBoxSelecting = false;

        if (selectionBoxRT != null)
        {
            Destroy(selectionBoxRT.gameObject);
            selectionBoxRT = null;
        }
    }


    private void CreateSelectionBox()
    {
        GameObject go = new GameObject("SelectionBox");
        go.transform.SetParent(modulesContainer, false);

        selectionBoxRT = go.AddComponent<RectTransform>();

        selectionBoxRT.anchorMin = Vector2.zero;
        selectionBoxRT.anchorMax = Vector2.zero;
        selectionBoxRT.pivot     = Vector2.zero;

        Image img = go.AddComponent<Image>();
        img.color = selectionBoxFill;
        img.raycastTarget = false;

        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = selectionBoxBorder;
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        selectionBoxRT.SetAsLastSibling();
    }

    private void UpdateSelectionBoxRect(Vector2 a, Vector2 b)
    {
        if (selectionBoxRT == null) return;

        Vector2 min = Vector2.Min(a, b);
        Vector2 max = Vector2.Max(a, b);

        selectionBoxRT.anchoredPosition = min;
        selectionBoxRT.sizeDelta = max - min;
    }


    private void UpdateSelectedModules(Vector2 a, Vector2 b)
    {
        Vector2 min = Vector2.Min(a, b);
        Vector2 max = Vector2.Max(a, b);
        Rect selectionRect = new Rect(min, max - min);

        for (int i = 0; i < selected.Count; i++)
        {
            if (selected[i] != null) selected[i].SetSelected(false);
        }
        selected.Clear();

        ModuleUI[] all = modulesContainer.GetComponentsInChildren<ModuleUI>();

        foreach (var modUI in all)
        {
            RectTransform modRT = modUI.GetComponent<RectTransform>();

            Bounds b3D = RectTransformUtility.CalculateRelativeRectTransformBounds(modulesContainer, modRT);
            Rect modRect = new Rect(b3D.min.x, b3D.min.y, b3D.size.x, b3D.size.y);

            if (selectionRect.Overlaps(modRect, true))
            {
                selected.Add(modUI);
                modUI.SetSelected(true);
            }
        }
    }

    public void ClearSelection()
    {
        for (int i = 0; i < selected.Count; i++)
            if (selected[i] != null) selected[i].SetSelected(false);
        selected.Clear();
    }


    public void NotifyModuleDestroyed(ModuleUI modUI)
    {
        selected.Remove(modUI);
    }

    public bool IsSelected(ModuleUI modUI)
    {
        return modUI != null && selected.Contains(modUI);
    }

    public IReadOnlyList<ModuleUI> GetSelected() => selected;



    private void CopySelection()
    {
        selected.RemoveAll(m => m == null);
        if (selected.Count == 0) return;

        clipboardModules.Clear();
        clipboardCables.Clear();

        var selectedIds = new HashSet<string>();
        foreach (var modUI in selected)
            selectedIds.Add(modUI.module.moduleId);

        foreach (var modUI in selected)
        {
            var entry = new ClipboardModule
            {
                moduleId   = modUI.module.moduleId,
                moduleType = modUI.module.moduleType,
                position   = modUI.GetPosition(),
                parameters = new List<ParamSaveData>()
            };

            foreach (var p in modUI.module.GetParameters())
                CollectParam(p, entry.parameters);

            clipboardModules.Add(entry);
        }

        foreach (var cable in PatchManager.Instance.GetCables())
        {
            if (selectedIds.Contains(cable.fromModuleId) &&
                selectedIds.Contains(cable.toModuleId))
            {
                clipboardCables.Add(new ClipboardCable
                {
                    fromModuleId = cable.fromModuleId,
                    fromPortId   = cable.fromPortId,
                    toModuleId   = cable.toModuleId,
                    toPortId     = cable.toPortId
                });
            }
        }

        Debug.Log($"[SelectionManager] Copiados {clipboardModules.Count} módulos y {clipboardCables.Count} cables.");
    }


    private void CollectParam(ModuleParameter param, List<ParamSaveData> list)
    {
        switch (param.type)
        {
            case ParameterType.KNOB:
            case ParameterType.SLIDER:
                list.Add(new ParamSaveData { id = param.id, floatValue = param.value });
                break;
            case ParameterType.TOGGLE:
                list.Add(new ParamSaveData { id = param.id, boolValue = param.boolValue });
                break;
            case ParameterType.DROPDOWN:
                list.Add(new ParamSaveData { id = param.id, intValue = param.selectedIndex });
                break;
            case ParameterType.GROUP:
                if (param.children != null)
                    foreach (var child in param.children)
                        CollectParam(child, list);
                break;
        }
    }


    private void PasteFromClipboard()
    {
        if (clipboardModules.Count == 0) return;

        ClearSelection();

        var idRemap = new Dictionary<string, string>();

        foreach (var entry in clipboardModules)
        {
            ModuleUI ui = ModuleUIFactory.Instance.CreateModule(
                entry.moduleType,
                entry.position + pasteOffset,
                entry.parameters);

            if (ui == null) continue;

            idRemap[entry.moduleId] = ui.module.moduleId;

            ui.SetSelected(true);
            selected.Add(ui);
        }

        foreach (var cc in clipboardCables)
        {
            if (!idRemap.TryGetValue(cc.fromModuleId, out string newFrom)) continue;
            if (!idRemap.TryGetValue(cc.toModuleId,   out string newTo))   continue;

            var cableData = PatchManager.Instance.Connect(
                newFrom, cc.fromPortId, newTo, cc.toPortId);

            if (cableData != null)
                CableManager.Instance.SpawnCableVisualForLoaded(cableData);
        }

        Debug.Log($"[SelectionManager] Pegados {clipboardModules.Count} módulos.");
    }
}