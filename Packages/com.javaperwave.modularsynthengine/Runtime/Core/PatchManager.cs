using UnityEngine;
using System.Collections.Generic;

public class PatchManager : MonoBehaviour
{
    public static PatchManager Instance {
        get;
        private set;
    }

    private Dictionary <string, Module> modules = new Dictionary <string, Module>();

    private List <CableData> cables = new List <CableData>();

    private int moduleCounter = 0;

    // Contador independiente para IDs de cable — evita colisiones al reutilizar indices
    private int cableCounter = 0;


    [System.Serializable]
    public class CableData 
    {
        public string id;
        public string fromModuleId;
        public string fromPortId;
        public string toModuleId;
        public string toPortId;
        public CV cvComponent;
    }

    private void Awake() {
        if (Instance != null && Instance != this) { 
            Destroy(gameObject); 
            return; 
        }

        Instance = this;
    }


    public string RegisterModule(Module module, string type = null)
    {
        if (!string.IsNullOrEmpty(module.moduleId)
            && modules.TryGetValue(module.moduleId, out Module existing)
            && existing == module)
        {
            if (!string.IsNullOrEmpty(type)) module.moduleType = type;
            return module.moduleId;
        }

        string resolvedType = type ?? module.GetType().Name;
        string id = $"{resolvedType}_{moduleCounter++:D3}";
        module.moduleId = id;
        module.moduleType = resolvedType;
        modules[id] = module;
        //Debug.Log($"[PatchManager] Módulo registrado: {id}");
        return id;
    }

    public void UnregisterModule(string moduleId)
    {
        var toRemove = cables.FindAll(c => 
            c.fromModuleId == moduleId || c.toModuleId == moduleId);
        foreach (var cable in toRemove)
            DisconnectCable(cable.id);

        modules.Remove(moduleId);
    }



    public CableData Connect(string fromModuleId, string fromPortId,
                             string toModuleId, string toPortId)
    {
        if (!modules.TryGetValue(fromModuleId, out Module fromModule)) return null;
        if (!modules.TryGetValue(toModuleId, out Module toModule)) return null;

        Port fromPort = fromModule.GetPort(fromPortId);
        Port toPort = toModule.GetPort(toPortId);

        if (fromPort == null || toPort == null) return null;
        if (!fromPort.CanConnectTo(toPort)) 
        {
            //Debug.LogWarning($"[PatchManager] Puertos incompatibles: {fromPortId} → {toPortId}");
            return null;
        }

        // Crear componente CV
        GameObject cableGO = new GameObject($"Cable_{fromModuleId}_{toModuleId}");
        CV cv = cableGO.AddComponent<CV>();
        cv.source = fromModule;
        cv.portId = fromPortId;

        toModule.ConnectInput(toPortId, cv);

        var cableData = new CableData
        {
            id = $"cable_{cableCounter++:D3}",
            fromModuleId = fromModuleId,
            fromPortId = fromPortId,
            toModuleId = toModuleId,
            toPortId = toPortId,
            cvComponent = cv
        };

        cables.Add(cableData);
        //Debug.Log($"[PatchManager] Conectado: {fromModuleId}.{fromPortId} → {toModuleId}.{toPortId}");
        return cableData;
    }

    public void DisconnectCable(string cableId)
    {
        var cable = cables.Find(c => c.id == cableId);
        if (cable == null) return;

        if (modules.TryGetValue(cable.toModuleId, out Module toModule))
            toModule.DisconnectInput(cable.toPortId);

        if (cable.cvComponent != null)
            Destroy(cable.cvComponent.gameObject);

        cables.Remove(cable);
    }

    public void DisconnectPort(string moduleId, string portId)
    {
        var cable = cables.Find(c =>
            (c.toModuleId == moduleId && c.toPortId == portId) ||
            (c.fromModuleId == moduleId && c.fromPortId == portId));
        if (cable != null) DisconnectCable(cable.id);
    }

    public CableData GetCableForPort(string moduleId, string portId)
    {
        return cables.Find(c =>
            (c.fromModuleId == moduleId && c.fromPortId == portId) ||
            (c.toModuleId == moduleId && c.toPortId == portId));
    }

    public Module GetModule(string id)
    {
        modules.TryGetValue(id, out Module m);
        return m;
    }

    public T GetModule<T>(string id) where T : Module
    {
        modules.TryGetValue(id, out Module m);
        return m as T;
    }

    public List<CableData> GetCables() => cables;

    public Dictionary<string, Module> GetModules() => modules;

    public void ClearAll()
    {
        foreach (var cable in cables)
        {
            if (cable.cvComponent != null)
                Destroy(cable.cvComponent.gameObject);
        }
        cables.Clear();

        var kept = new Dictionary<string, Module>();
        
        foreach (var module in modules.Values)
        {
            if (module == null) continue;

            if (module is MasterOutModule)
            {
                foreach (var p in module.GetInputPorts())
                    module.DisconnectInput(p.id);
                kept[module.moduleId] = module;
            }
            else
            {
                Destroy(module.gameObject);
            }
        }
        modules = kept;

        moduleCounter = 0;
        cableCounter = 0;
    }

}