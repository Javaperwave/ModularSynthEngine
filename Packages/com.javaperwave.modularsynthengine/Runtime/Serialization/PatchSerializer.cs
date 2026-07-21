using System.Collections.Generic;
using System.IO;
using UnityEngine;


public class PatchSerializer : MonoBehaviour
{
    public static PatchSerializer Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SavePatchToPath(string fullPath)
    {
        string name = Path.GetFileName(fullPath).Replace(".patch.json", "");

        PatchFile patch = CapturePatch(name);
        string json = JsonUtility.ToJson(patch, prettyPrint: true);

        File.WriteAllText(fullPath, json);
        Debug.Log($"[PatchSerializer] Guardado: {fullPath}");
    }


    public bool LoadPatchFromPath(string fullPath)
    {
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning($"[PatchSerializer] No encontrado: {fullPath}");
            return false;
        }

        string json = File.ReadAllText(fullPath);
        PatchFile patch = JsonUtility.FromJson<PatchFile>(json);
        ApplyPatch(patch);
        return true;
    }

    public PatchFile CapturePatch(string name)
    {
        var patch = new PatchFile { name = name };

        foreach (var kvp in PatchManager.Instance.GetModules())
        {
            Module module = kvp.Value;

            var moduleData = new ModuleSaveData
            {
                moduleId   = module.moduleId,
                moduleType = module.moduleType,
                position   = Vector2.zero
            };

            foreach (var param in module.GetParameters())
                CollectParam(param, moduleData.parameters);

            patch.modules.Add(moduleData);
        }

        foreach (var cable in PatchManager.Instance.GetCables())
        {
            patch.cables.Add(new CableSaveData
            {
                fromModuleId = cable.fromModuleId,
                fromPortId   = cable.fromPortId,
                toModuleId   = cable.toModuleId,
                toPortId     = cable.toPortId
            });
        }

        return patch;
    }

    private void CollectParam(ModuleParameter param, List<ParamSaveData> list)
    {
        switch (param.type)
        {
            case ParameterType.KNOB:
            case ParameterType.SLIDER:
                list.Add(new ParamSaveData { id = param.id, floatValue = param.value });
                break;
            case ParameterType.TOGGLE:
                list.Add(new ParamSaveData { id = param.id, boolValue = param.boolValue });
                break;
            case ParameterType.DROPDOWN:
                list.Add(new ParamSaveData { id = param.id, intValue = param.selectedIndex });
                break;
            case ParameterType.GROUP:
                if (param.children != null)
                    foreach (var child in param.children)
                        CollectParam(child, list);
                break;
        }
    }

    public void ApplyPatch(PatchFile patch)
    {
        ClearSession();

        var idRemap = new Dictionary<string, string>();

        foreach (var md in patch.modules)
        {
            if (md.moduleType == "MasterOut")
            {
                idRemap[md.moduleId] = Synthesizer.Instance.GetMasterOut().moduleId;
                continue;
            }

            Module module = ModuleFactory.Instance.CreateModule(md.moduleType, md.parameters);
            if (module == null) continue;

            idRemap[md.moduleId] = module.moduleId;
        }

        foreach (var cd in patch.cables)
        {
            if (!idRemap.TryGetValue(cd.fromModuleId, out string newFrom)) continue;
            if (!idRemap.TryGetValue(cd.toModuleId,   out string newTo)) continue;

            PatchManager.Instance.Connect(newFrom, cd.fromPortId, newTo, cd.toPortId);
        }

        Debug.Log($"[PatchSerializer] '{patch.name}' — {patch.modules.Count} módulos, {patch.cables.Count} cables.");
    }

    public void ClearSession()
    {
        PatchManager.Instance.ClearAll();
    }
}