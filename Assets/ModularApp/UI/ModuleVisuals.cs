using UnityEngine;

public abstract class ModuleVisuals : MonoBehaviour
{
    protected Module module;
    protected ModuleUI moduleUI;

    public virtual void Initialize(Module module, ModuleUI moduleUI)
    {
        this.module = module;
        this.moduleUI = moduleUI;
    }
}