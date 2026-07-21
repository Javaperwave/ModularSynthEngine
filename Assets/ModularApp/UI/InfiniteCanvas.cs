using UnityEngine;
using UnityEngine.EventSystems;

public class InfiniteCanvas : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Referencias")]
    public RectTransform rackWorld;

    [Header("Zoom")]
    public float zoomMin = 0.2f;
    public float zoomMax = 2.5f;
    public float zoomSpeed = 0.1f;
    public float zoomSmoothSpeed = 8f;

    [Header("Pan")]
    public float panSmoothSpeed = 12f;

    private float currentZoom = 1f;
    private float targetZoom = 1f;
    private Vector2 targetPan;
    private bool isPanning = false;
    private Vector2 lastMousePosition;
    private RectTransform rectTransform;
    private Canvas parentCanvas;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
        targetPan = rackWorld.anchoredPosition;
    }

    private void Update()
    {
        HandleZoomInput();
        HandlePanInput();
        ApplyTransform();
    }

    private void HandleZoomInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.001f) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, Input.mousePosition, null, out Vector2 localMouse);

        float oldZoom = targetZoom;
        targetZoom = Mathf.Clamp(targetZoom + scroll * zoomSpeed * 10f, zoomMin, zoomMax);

        float zoomDelta = targetZoom - oldZoom;
        targetPan -= localMouse * zoomDelta;
    }

    private void HandlePanInput()
    {
        if (Input.GetMouseButtonDown(2) && !CableManager.Instance.IsDraggingCable)
        {
            isPanning = true;
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(2)) isPanning = false;

        if (isPanning && Input.GetMouseButton(2))
        {
            Vector2 delta = (Vector2)Input.mousePosition - lastMousePosition;
            targetPan += delta / parentCanvas.scaleFactor;
            lastMousePosition = Input.mousePosition;
        }
    }

    private void ApplyTransform()
    {
        currentZoom = Mathf.Lerp(currentZoom, targetZoom, Time.unscaledDeltaTime * zoomSmoothSpeed);
        rackWorld.anchoredPosition = Vector2.Lerp(
            rackWorld.anchoredPosition, targetPan,
            Time.unscaledDeltaTime * panSmoothSpeed);
        rackWorld.localScale = Vector3.one * currentZoom;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
            eventData.Use();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
            eventData.Use();
    }

    public void ResetView()
    {
        targetZoom = 1f;
        targetPan = Vector2.zero;
    }
}