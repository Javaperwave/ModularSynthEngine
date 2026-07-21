using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ModuleDragHandler : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    private ModuleUI moduleUI;
    private RectTransform moduleRect;
    private RectTransform rackWorld;
    private Vector2 dragOffset;

    private bool isMultiDragging;
    private Vector2 multiDragMouseStart;
    private List<MultiDragTarget> multiDragTargets;

    private struct MultiDragTarget
    {
        public ModuleUI ui;
        public RectTransform rt;
        public Vector2 startPos;
    }

    public void Initialize(ModuleUI moduleUI)
    {
        this.moduleUI = moduleUI;
        moduleRect = moduleUI.GetComponent<RectTransform>();
        rackWorld = GameObject.Find("RackWorld")?.GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        moduleUI.BringToFront();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rackWorld, eventData.position, eventData.pressEventCamera,
            out Vector2 mouseInWorld);
        dragOffset = moduleRect.anchoredPosition - mouseInWorld;

        isMultiDragging = false;
        var sm = SelectionManager.Instance;
        if (sm != null && sm.IsSelected(moduleUI))
        {
            var sel = sm.GetSelected();
            if (sel.Count > 1)
            {
                isMultiDragging = true;
                multiDragMouseStart = mouseInWorld;
                multiDragTargets = new List<MultiDragTarget>(sel.Count);

                foreach (var m in sel)
                {
                    if (m == null) continue;
                    var rt = m.GetComponent<RectTransform>();
                    if (rt == null) continue;

                    multiDragTargets.Add(new MultiDragTarget
                    {
                        ui       = m,
                        rt       = rt,
                        startPos = rt.anchoredPosition
                    });
                }
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rackWorld, eventData.position, eventData.pressEventCamera,
            out Vector2 mouseInWorld);

        if (isMultiDragging)
        {
            Vector2 delta = mouseInWorld - multiDragMouseStart;

            foreach (var t in multiDragTargets)
            {
                if (t.ui == null || t.rt == null) continue;
                t.rt.anchoredPosition = t.startPos + delta;
                CableManager.Instance?.UpdateCablesForModule(t.ui.module.moduleId);
            }
        }
        else
        {
            moduleRect.anchoredPosition = mouseInWorld + dragOffset;
            CableManager.Instance?.UpdateCablesForModule(moduleUI.module.moduleId);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isMultiDragging)
        {
            foreach (var t in multiDragTargets)
            {
                if (t.ui == null) continue;
                CableManager.Instance?.UpdateCablesForModule(t.ui.module.moduleId);
            }

            isMultiDragging = false;
            multiDragTargets = null;
        }
        else
        {
            CableManager.Instance?.UpdateCablesForModule(moduleUI.module.moduleId);
            ModuleCollision.Instance?.ResolveOverlaps(moduleUI);
        }
    }
}