using UnityEngine;
using System.Collections.Generic;

public class ModuleUIFactory : MonoBehaviour
{
    public static ModuleUIFactory Instance { get; private set; }

    [Header("Prefab base UI")]
    public GameObject modulePrefabUI;

    [Header("Contenedor en escena")]
    public RectTransform modulesContainer;

    private static readonly Dictionary<string, Color> moduleColors = new Dictionary<string, Color>
    {
        // Audio Generation
        {"Oscillator", new Color(0.392f, 0.573f, 0.796f)},

        // Processing and FX
        {"Amplifier", new Color(0.996f, 0.404f, 0.165f)},
        {"Filter", new Color(0.165f, 0.537f, 0.494f)},
        {"Distorsion", new Color(0.910f, 0.333f, 0.373f)},
        {"Delay", new Color(0.231f, 0.310f, 0.576f)},
        {"Reverb", new Color(0.820f, 0.412f, 0.549f)},
        {"RingMod", new Color(1.000f, 0.478f, 0.204f)},

        // Modulation
        {"LFO", new Color(0.435f, 0.267f, 0.514f)},
        {"Envelope", new Color(0.373f, 0.325f, 0.584f)},
        {"SampleHold", new Color(0.384f, 0.690f, 0.863f)},

        // Control and CV Generation
        {"StepSequencer", new Color(0.855f, 0.200f, 0.165f)},
        {"Pitch Quantizer",new Color(0.157f, 0.498f, 0.569f)},
        {"MIDI to CV", new Color(0.286f, 0.459f, 0.769f)},

        // Utilities
        {"Mixer", new Color(1.000f, 0.698f, 0.173f)},
        {"Attenuverter", new Color(0.196f, 0.263f, 0.435f)},
        {"Clock", new Color(0.514f, 0.729f, 0.412f)},
        {"Oscilloscope", new Color(0.298f, 0.651f, 0.306f)},
    };

    private static readonly Dictionary<string, System.Type> moduleVisuals = new Dictionary<string, System.Type>
    {
        {"Oscilloscope", typeof(OscilloscopeVisuals)},
        {"Envelope", typeof(EnvelopeVisuals)},
        {"Clock", typeof(ClockVisuals)},
        {"StepSequencer", typeof(StepSequencerVisuals)},
    };

    private Dictionary<string, ModuleUI> moduleUIs = new Dictionary<string, ModuleUI>();

    private int spawnColumn = 0;
    private int spawnRow = 0;
    private const int MaxColumns = 4;
    private const float SpawnOffsetX = 200f;
    private const float SpawnRowOffset = -80f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public ModuleUI CreateModule(string moduleType, Vector2? customPosition = null,
                                  List<ParamSaveData> savedParams = null)
    {
        Module module = ModuleFactory.Instance.CreateModule(moduleType, savedParams);
        if (module == null) return null;

        Vector2 position;
        if (customPosition.HasValue)
        {
            position = customPosition.Value;
        }
        else
        {
            position = new Vector2(
                -300f + spawnColumn * SpawnOffsetX,
                150f + spawnRow * SpawnRowOffset
            );
            spawnColumn++;
            if (spawnColumn >= MaxColumns)
            {
                spawnColumn = 0;
                spawnRow--;
            }
        }

        GameObject uiGO = Instantiate(modulePrefabUI, modulesContainer);
        ModuleUI moduleUI = uiGO.GetComponent<ModuleUI>();

        Color color = moduleColors.TryGetValue(moduleType, out Color c) ? c : Color.gray;
        moduleUI.Initialize(module, color);

        if (moduleVisuals.TryGetValue(moduleType, out System.Type visualsType))
        {
            var visuals = (ModuleVisuals)uiGO.AddComponent(visualsType);
            visuals.Initialize(module, moduleUI);
        }

        moduleUI.SetPosition(position);
        uiGO.name = $"UI_{module.moduleId}";

        moduleUIs[module.moduleId] = moduleUI;
        ModuleCollision.Instance?.ResolveOverlaps(moduleUI);
        return moduleUI;
    }

    public void ClearAllModules()
    {
        foreach (var ui in moduleUIs.Values)
        {
            if (ui != null) Destroy(ui.gameObject);
        }
        moduleUIs.Clear();
        spawnColumn = 0;
        spawnRow    = 0;
    }

    public ModuleUI GetModuleUI(string moduleId)
    {
        moduleUIs.TryGetValue(moduleId, out ModuleUI ui);
        return ui;
    }

    public void RemoveModuleUI(string moduleId)
    {
        moduleUIs.Remove(moduleId);
    }
}