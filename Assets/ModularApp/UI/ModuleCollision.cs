using UnityEngine;
using System.Collections.Generic;

public class ModuleCollision : MonoBehaviour
{
    public static ModuleCollision Instance { get; private set; }

    private const int MaxIterations = 20;
    private const float Padding = 8f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // Llamado después de soltar un módulo
    public void ResolveOverlaps(ModuleUI movedModule)
    {
        var allModules = FindObjectsByType<ModuleUI>(FindObjectsSortMode.None);

        for (int iter = 0; iter < MaxIterations; iter++)
        {
            bool anyOverlap = false;

            foreach (var other in allModules)
            {
                if (other == movedModule) continue;

                Vector2 push = GetOverlapPush(movedModule, other);

                if (push != Vector2.zero)
                {
                    anyOverlap = true;
                    movedModule.SetPosition(movedModule.GetPosition() + push);
                    CableManager.Instance?.UpdateCablesForModule(movedModule.module.moduleId);
                }
            }

            if (!anyOverlap) break;
        }
    }

    // Devuelve el vector de empuje necesario para separar dos módulos
    private Vector2 GetOverlapPush(ModuleUI a, ModuleUI b)
    {
        Rect rectA = GetRect(a);
        Rect rectB = GetRect(b);

        // Expandir con padding
        rectA = Expand(rectA, Padding);
        rectB = Expand(rectB, Padding);

        if (!rectA.Overlaps(rectB)) return Vector2.zero;

        // Calcular cuánto se solapan en cada eje
        float overlapX = Mathf.Min(rectA.xMax, rectB.xMax) - Mathf.Max(rectA.xMin, rectB.xMin);
        float overlapY = Mathf.Min(rectA.yMax, rectB.yMax) - Mathf.Max(rectA.yMin, rectB.yMin);

        // Empujar por el eje con menor solapamiento
        if (overlapX < overlapY)
        {
            float direction = rectA.center.x < rectB.center.x ? -1f : 1f;
            return new Vector2(overlapX * direction, 0);
        }
        else
        {
            float direction = rectA.center.y < rectB.center.y ? -1f : 1f;
            return new Vector2(0, overlapY * direction);
        }
    }

    private Rect GetRect(ModuleUI module)
    {
        RectTransform rt = module.GetComponent<RectTransform>();
        Vector2 pos = module.GetPosition();
        Vector2 size = rt.rect.size;
        Vector2 pivot = rt.pivot;

        return new Rect(
            pos.x - size.x * pivot.x,
            pos.y - size.y * pivot.y,
            size.x,
            size.y
        );
    }

    private Rect Expand(Rect rect, float amount)
    {
        return new Rect(
            rect.x - amount,
            rect.y - amount,
            rect.width + amount * 2,
            rect.height + amount * 2
        );
    }
}