using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ToggleUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Referencias")]
    public Image background;
    public Image indicator;
    public TextMeshProUGUI nameLabel;

    private ModuleParameter parameter;

    private static readonly Color colorOn  = new Color(0.2f, 0.9f, 0.4f);
    private static readonly Color colorOff = new Color(0.3f, 0.3f, 0.3f);

    private static readonly Color colorBgHighlightOn  = new Color(0.95f, 0.95f, 0.4f);
    private static readonly Color colorBgHighlightOff = new Color(0.55f, 0.55f, 0.55f);

    private Color baseBgColor;
    private bool isHighlighted = false;

    public void Initialize(ModuleParameter param)
    {
        parameter = param;
        nameLabel.text = param.label;

        if (background != null)
            baseBgColor = background.color;
        
        UpdateVisual();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        parameter.boolValue = !parameter.boolValue;
        parameter.onToggleChanged?.Invoke(parameter.boolValue);
        UpdateVisual();
    }

    public void SetHighlight(bool active)
    {
        if (isHighlighted == active) return;
        isHighlighted = active;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        bool on = parameter != null && parameter.boolValue;
        indicator.color = on ? colorOn : colorOff;
 
        if (background != null)
        {
            if (isHighlighted)
                background.color = on ? colorBgHighlightOn : colorBgHighlightOff;
            else
                background.color = baseBgColor;
        }
    }
}