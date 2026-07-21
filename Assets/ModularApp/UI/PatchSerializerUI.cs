using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PatchSerializerUI : MonoBehaviour
{
    public static PatchSerializerUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (!string.IsNullOrEmpty(PatchLoader.PatchToLoad))
        {
            LoadPatchFromPath(PatchLoader.PatchToLoad);
            PatchLoader.PatchToLoad = null;
        }
    }

    public void SavePatchToPath(string fullPath)
    {
        string name = Path.GetFileName(fullPath).Replace(".patch.json", "");

        PatchFile patch = PatchSerializer.Instance.CapturePatch(name);

        foreach (var md in patch.modules)
        {
            ModuleUI ui = ModuleUIFactory.Instance.GetModuleUI(md.moduleId);
            if (ui != null) md.position = ui.GetPosition();
        }

        string json = JsonUtility.ToJson(patch, prettyPrint: true);
        File.WriteAllText(fullPath, json);
        Debug.Log($"[PatchSerializerUI] Guardado: {fullPath}");
    }

    public bool LoadPatchFromPath(string fullPath)
    {
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning($"[PatchSerializerUI] No encontrado: {fullPath}");
            return false;
        }

        string json = File.ReadAllText(fullPath);
        PatchFile patch = JsonUtility.FromJson<PatchFile>(json);
        ApplyPatchUI(patch);
        return true;
    }

    private void ApplyPatchUI(PatchFile patch)
    {
        CableManager.Instance.RemoveAllCables();
        PatchSerializer.Instance.ClearSession();
        ModuleUIFactory.Instance.ClearAllModules();

        var idRemap = new Dictionary<string, string>();

        foreach (var md in patch.modules)
        {
            ModuleUI ui = ModuleUIFactory.Instance.CreateModule(md.moduleType, md.position, md.parameters);
            if (ui == null) continue;

            idRemap[md.moduleId] = ui.module.moduleId;
        }

        var masterOutModule = Synthesizer.Instance?.GetMasterOut();
        
        if (masterOutModule != null)
        {
            var savedMasterOut = patch.modules.Find(m => m.moduleType == "MasterOut");
            if (savedMasterOut != null)
                idRemap[savedMasterOut.moduleId] = masterOutModule.moduleId;
        }

        foreach (var cd in patch.cables)
        {
            if (!idRemap.TryGetValue(cd.fromModuleId, out string newFrom)) continue;
            if (!idRemap.TryGetValue(cd.toModuleId,   out string newTo))   continue;

            var cableData = PatchManager.Instance.Connect(newFrom, cd.fromPortId, newTo, cd.toPortId);
            if (cableData != null)
                CableManager.Instance.SpawnCableVisualForLoaded(cableData);
        }

        Debug.Log($"[PatchSerializerUI] '{patch.name}' — {patch.modules.Count} módulos, {patch.cables.Count} cables.");
    }
}