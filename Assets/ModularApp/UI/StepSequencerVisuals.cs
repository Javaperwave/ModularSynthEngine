using UnityEngine;

public class StepSequencerVisuals : ModuleVisuals
{
    private StepSequencer sequencer;
    private ToggleUI[] cachedStepToggles;

    public override void Initialize(Module module, ModuleUI moduleUI)
    {
        base.Initialize(module, moduleUI);
        sequencer = (StepSequencer)module;
    }

    void Update()
    {
        EnsureToggleCache();
        UpdateStepHighlights();
    }

    private void EnsureToggleCache()
    {
        if (cachedStepToggles != null) return;
        if (moduleUI == null) return;
        if (sequencer == null) return;

        cachedStepToggles = new ToggleUI[sequencer.steps];
        var allToggles = moduleUI.GetComponentsInChildren<ToggleUI>(true);
        const string prefix = "Group_step_";
        foreach (var t in allToggles)
        {
            Transform parent = t.transform.parent;
            if (parent == null) continue;
            if (!parent.name.StartsWith(prefix)) continue;
            string idxStr = parent.name.Substring(prefix.Length);
            if (int.TryParse(idxStr, out int idx) && idx >= 0 && idx < cachedStepToggles.Length)
                cachedStepToggles[idx] = t;
        }
    }

    private void UpdateStepHighlights()
    {
        if (cachedStepToggles == null) return;
        for (int i = 0; i < cachedStepToggles.Length; i++)
        {
            if (cachedStepToggles[i] != null)
                cachedStepToggles[i].SetHighlight(i == sequencer.CurrentStep);
        }
    }
}