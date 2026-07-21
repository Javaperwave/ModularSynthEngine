using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UIElements;

public class PortUI : MonoBehaviour,
    IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Referencias")]
    public UnityEngine.UI.Image portCircle;
    public TextMeshProUGUI portLabel;

    public Port port { get; private set; }
    public ModuleUI moduleUI { get; private set; }

    // Colores por tipo de puerto
    private static readonly Color colorAudio = new Color(0.9f, 0.9f, 0.9f);
    private static readonly Color colorPitchCV = new Color(1.0f, 0.8f, 0.1f);
    private static readonly Color colorModCV = new Color(0.3f, 0.8f, 1.0f);
    private static readonly Color colorGate = new Color(0.2f, 1.0f, 0.3f);
    private static readonly Color colorTrigger = new Color(1.0f, 0.4f, 0.2f);

    private Color baseColor;
    private bool isHighlighted = false;

    public void Initialize(Port port, ModuleUI moduleUI)
    {
        this.port = port;
        this.moduleUI = moduleUI;

        portLabel.text = port.label;

        if (port.dir == PortDir.OUTPUT)
            portLabel.alignment = TextAlignmentOptions.Right;
        else
            portLabel.alignment = TextAlignmentOptions.Left;

        baseColor = GetColorForType(port.type);
        portCircle.color = baseColor;

        if (port.dir == PortDir.INPUT)
        {
            portCircle.transform.SetAsFirstSibling();
        }
    }

    private Color GetColorForType(PortType type)
    {
        return type switch
        {
            PortType.AUDIO => colorAudio,
            PortType.PITCHCV => colorPitchCV,
            PortType.MODCV => colorModCV,
            PortType.GATE => colorGate,
            PortType.TRIGGER => colorTrigger,
            _ => Color.white
        };
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (CableManager.Instance.IsDraggingCable)
                return;


            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                CableManager.Instance.DisconnectCablesAtPort(this);
            }
            else
            {
                CableManager.Instance.BeginCable(this);
            }
            eventData.Use();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (CableManager.Instance.IsDraggingCable)
        {
            // Mostrar si es compatible o no
            bool compatible = CableManager.Instance.CanConnectTo(this);
            portCircle.color = compatible
                ? Color.white
                : new Color(1f, 0.2f, 0.2f);
        }
        else
        {
            portCircle.color = Color.white;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isHighlighted)
            portCircle.color = baseColor;
        else
            SetHighlight(true);
    }

    public void SetHighlight(bool on)
    {
        isHighlighted = on;
        if (on)
        {
            // Pulsar brillo
            portCircle.color = new Color(
                baseColor.r + 0.3f,
                baseColor.g + 0.3f,
                baseColor.b + 0.3f,
                1f);
        }
        else
        {
            portCircle.color = baseColor;
        }
    }

    public Vector2 GetWorldPosition()
    {
        return portCircle.rectTransform.position;
    }
}