using UnityEngine;
using UnityEngine.UI;

public class OscilloscopeVisuals : ModuleVisuals
{
    private const float DISPLAY_HEIGHT = 140f;

    public override void Initialize(Module module, ModuleUI moduleUI)
    {
        base.Initialize(module, moduleUI);

        Oscilloscope osc = (Oscilloscope)module;

        GameObject container = new GameObject("Display_scope_display", typeof(RectTransform));
        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.SetParent(moduleUI.bodyContainer, false);

        var le = container.AddComponent<LayoutElement>();
        le.minHeight = DISPLAY_HEIGHT;
        le.preferredHeight = DISPLAY_HEIGHT;
        le.flexibleWidth = 1;

        GameObject scope = new GameObject("ScopeDisplay", typeof(RectTransform));
        scope.transform.SetParent(containerRect, false);

        RectTransform rt = scope.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var display = scope.AddComponent<OscilloscopeDisplay>();
        display.SetSource(osc);

        containerRect.SetAsFirstSibling();
    }
}