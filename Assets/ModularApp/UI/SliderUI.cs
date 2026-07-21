using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Globalization;
using System.Collections;

public class SliderUI : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [Header("Referencias")]
    public Image fill;
    public TextMeshProUGUI valueLabel;

    private ModuleParameter parameter;
    private RectTransform trackRect;

    private bool editing = false;

    public void Initialize(ModuleParameter param)
    {
        parameter = param;

        trackRect = fill.transform.parent.GetComponent<RectTransform>();

        if (valueLabel == null)
        {
            Transform t = transform.Find("ValueLabel");
            if (t != null) valueLabel = t.GetComponent<TextMeshProUGUI>();
        }

        //Detectar doble click sobre el valueLabel
        if (valueLabel != null)
        {
            var clickDetector = valueLabel.gameObject.AddComponent<LabelClickDetector>();
            clickDetector.onDoubleClick = StartEditing;
        }

        UpdateVisual();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (editing) return;
        HandleDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (editing) return;
        HandleDrag(eventData);
    }

    private void HandleDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            trackRect, eventData.position, eventData.pressEventCamera, out Vector2 local);

        float halfW = trackRect.rect.width * 0.5f;
        float t = Mathf.Clamp01((local.x + halfW) / trackRect.rect.width);

        parameter.value = parameter.NormalizedToValue(t);
        if (parameter.isInt)
            parameter.value = Mathf.Round(parameter.value);

        parameter.onKnobChanged?.Invoke(parameter.value);
        UpdateVisual();
    }

    private void StartEditing()
    {
        if (editing) return;
        StartCoroutine(EditingCoroutine());
    }

    private IEnumerator EditingCoroutine()
    {
        editing = true;
        string inputBuffer = "";
        bool cursorVisible = true;
        const float CURSOR_BLINK_RATE = 0.5f;
        float cursorTimer = 0f;

        //Guardar valor previo para poder cancelar
        float previousValue = parameter.value;

        UpdateEditingLabel(inputBuffer, cursorVisible);

        while (editing)
        {
            //Cursor parpadeante
            cursorTimer += Time.unscaledDeltaTime;
            if (cursorTimer >= CURSOR_BLINK_RATE)
            {
                cursorTimer = 0f;
                cursorVisible = !cursorVisible;
                UpdateEditingLabel(inputBuffer, cursorVisible);
            }

            //Confirmar
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                ApplyInput(inputBuffer);
                break;
            }

            //Cancelar con Escape o click fuera — restaurar valor previo
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(0))
            {
                parameter.value = previousValue;
                parameter.onKnobChanged?.Invoke(parameter.value);
                UpdateVisual();
                break;
            }

            //Borrar
            if (Input.GetKeyDown(KeyCode.Backspace) && inputBuffer.Length > 0)
            {
                inputBuffer = inputBuffer.Substring(0, inputBuffer.Length - 1);
                UpdateEditingLabel(inputBuffer, cursorVisible);
            }

            //Capturar caracteres válidos para un número decimal
            foreach (char c in Input.inputString)
            {
                if (char.IsDigit(c) || c == '.' || c == ',' || (c == '-' && inputBuffer.Length == 0))
                {
                    inputBuffer += c;
                    UpdateEditingLabel(inputBuffer, cursorVisible);
                }
            }

            yield return null;
        }

        editing = false;
    }

    private void UpdateEditingLabel(string buffer, bool cursorVisible)
    {
        if (valueLabel == null) return;
        valueLabel.text = buffer + (cursorVisible ? "|" : " ");
    }

    private void ApplyInput(string raw)
    {
        raw = raw.Replace(',', '.');

        if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
        {
            parameter.value = Mathf.Clamp(parsed, parameter.min, parameter.max);
            if (parameter.isInt)
                parameter.value = Mathf.Round(parameter.value);
            parameter.onKnobChanged?.Invoke(parameter.value);
        }

        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (parameter == null || fill == null) return;

        float t = parameter.ValueToNormalized(parameter.value);
        fill.fillAmount = t;

        if (valueLabel != null)
        {
            if (parameter.isInt)
                valueLabel.text = ((int)parameter.value).ToString();
            else
                valueLabel.text = parameter.value.ToString("F2");
        }
    }
}


public class LabelClickDetector : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
{
    public System.Action onDoubleClick;
    private float lastClickTime = -1f;
    private const float DOUBLE_CLICK_THRESHOLD = 0.3f;

    public void OnPointerDown(PointerEventData eventData)
    {
        eventData.Use();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Time.unscaledTime - lastClickTime < DOUBLE_CLICK_THRESHOLD)
            onDoubleClick?.Invoke();
        lastClickTime = Time.unscaledTime;
    }
}