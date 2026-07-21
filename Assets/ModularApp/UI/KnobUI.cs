using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class KnobUI : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Referencias")]
    public Image knobRing;
    public Image knobDot;
    public TextMeshProUGUI valueLabel;
    public TextMeshProUGUI nameLabel;

    private ModuleParameter parameter;
    private float dragStartY;
    private float dragStartT;
    private bool isDragging;

    private const float MinAngle = -135f;
    private const float MaxAngle = 135f;

    public void Initialize(ModuleParameter param)
    {
        parameter = param;
        nameLabel.text = param.label;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        float t = parameter.ValueToNormalized(parameter.value);
        float angle = Mathf.Lerp(MinAngle, MaxAngle, t);

        if (knobDot != null)
            knobDot.rectTransform.localRotation = Quaternion.Euler(0, 0, -angle);

        if (knobRing != null)
            knobRing.fillAmount = t;

        if (valueLabel != null)
        {
            valueLabel.text = parameter.isInt
                ? Mathf.RoundToInt(parameter.value).ToString()
                : parameter.value.ToString("F2");
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        isDragging = true;
        dragStartY = eventData.position.y;

        dragStartT = parameter.ValueToNormalized(parameter.value);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        float dragDelta = eventData.position.y - dragStartY;

        float newT = Mathf.Clamp01(dragStartT + dragDelta / 200f);

        float newValue = parameter.NormalizedToValue(newT);

        if (parameter.isInt)
            newValue = Mathf.Round(newValue);

        parameter.value = newValue;
        parameter.onKnobChanged?.Invoke(newValue);
        UpdateVisual();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (knobRing != null)
            knobRing.color = new Color(1f, 1f, 1f, 0.9f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (knobRing != null)
            knobRing.color = new Color(1f, 1f, 1f, 0.5f);
    }
}