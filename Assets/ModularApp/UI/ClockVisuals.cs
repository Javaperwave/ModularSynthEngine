using UnityEngine;
using UnityEngine.UI;

public class ClockVisuals : ModuleVisuals
{
    private const float DISPLAY_HEIGHT = 18f;

    public override void Initialize(Module module, ModuleUI moduleUI)
    {
        base.Initialize(module, moduleUI);

        Clock clock = (Clock)module;

        GameObject container = new GameObject("Display_clock_leds", typeof(RectTransform));
        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.SetParent(moduleUI.bodyContainer, false);

        var le = container.AddComponent<LayoutElement>();
        le.minHeight = DISPLAY_HEIGHT;
        le.preferredHeight = DISPLAY_HEIGHT;
        le.flexibleWidth = 1;

        var display = container.AddComponent<ClockDisplay>();
        display.SetSource(clock);

        containerRect.SetAsFirstSibling();
    }
}