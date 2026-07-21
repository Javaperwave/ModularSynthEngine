using UnityEngine;
using UnityEngine.UI;

public class GradientOverlay : MonoBehaviour
{
    [Header("Colores del degradado")]
    public Color colorA = new Color(0.4f, 0.1f, 0.6f, 0.35f); // morado
    public Color colorB = new Color(0.0f, 0.3f, 0.6f, 0.35f); // azul
    public Color colorC = new Color(0.0f, 0.5f, 0.4f, 0.35f); // teal

    [Header("Velocidad de transición")]
    public float speed = 0.4f;

    private Image _image;
    private float _time;

    void Awake()
    {
        _image = GetComponent<Image>();
    }

    void Update()
    {
        _time += Time.deltaTime * speed;

        float t1 = (Mathf.Sin(_time) + 1f) * 0.5f;
        float t2 = (Mathf.Sin(_time + 2.094f) + 1f) * 0.5f;

        Color blended = Color.Lerp(Color.Lerp(colorA, colorB, t1), colorC, t2);
        _image.color = blended;
    }
}