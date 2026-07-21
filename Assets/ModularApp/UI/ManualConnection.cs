using UnityEngine;


public class ManualConnection : MonoBehaviour
{
    public Module fromModule;
    public string fromPortId;
    public Module toModule;
    public string toPortId;

    private void Start()
    {
        if (fromModule == null || toModule == null) return;
        
        if (string.IsNullOrEmpty(fromPortId) || string.IsNullOrEmpty(toPortId)) return;

        if (PatchManager.Instance == null)
        {
            return;
        }

        PatchManager.Instance.Connect(
            fromModule.moduleId, fromPortId,
            toModule.moduleId, toPortId);
    }
}