using UnityEngine;
using UnityEngine.UI;

public class EnvelopeVisuals : ModuleVisuals
{
    private const float DISPLAY_HEIGHT = 70f;

    public override void Initialize(Module module, ModuleUI moduleUI)
    {
        base.Initialize(module, moduleUI);

        Envelope env = (Envelope)module;

        GameObject container = new GameObject("Display_envelope_shape", typeof(RectTransform));
        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.SetParent(moduleUI.bodyContainer, false);

        var le = container.AddComponent<LayoutElement>();
        le.minHeight = DISPLAY_HEIGHT;
        le.preferredHeight = DISPLAY_HEIGHT;
        le.flexibleWidth = 1;

        var display = container.AddComponent<EnvelopeDisplay>();
        display.SetSource(env);

        containerRect.SetAsFirstSibling();
    }
}