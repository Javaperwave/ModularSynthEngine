using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class DropdownUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Referencias")]
    public TextMeshProUGUI nameLabel;
    public TextMeshProUGUI valueLabel;
    public Image background;

    private static readonly Color colorNormal = new Color(0.133f, 0.133f, 0.133f, 1f);
    private static readonly Color colorHover = new Color(0.25f, 0.25f, 0.25f, 1f);
    private static readonly Color colorPressed = new Color(1f, 1f, 1f, 0.2f);

    private ModuleParameter parameter;

    public void Initialize(ModuleParameter param)
    {
        parameter = param;
        nameLabel.text = param.label;
        UpdateVisual();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        parameter.selectedIndex = (parameter.selectedIndex + 1) % parameter.options.Length;
        parameter.onDropdownChanged?.Invoke(parameter.selectedIndex);
        UpdateVisual();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        background.color = colorHover;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        background.color = colorNormal;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        background.color = colorPressed;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        background.color = colorNormal;
    }

    private void UpdateVisual()
    {
        if (parameter.options != null && parameter.options.Length > 0)
            valueLabel.text = parameter.options[parameter.selectedIndex];
    }
}