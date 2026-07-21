using UnityEngine;
using System.Collections.Generic;

public class ModuleFactory : MonoBehaviour
{
    public static ModuleFactory Instance { get; private set; }

    //Open registry
    private readonly Dictionary <string, System.Type> registry = new Dictionary<string, System.Type>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Register(string moduleType, System.Type moduleClass)
    {
        if (string.IsNullOrEmpty(moduleType) || moduleClass == null) return;

        if (!typeof(Module).IsAssignableFrom(moduleClass)) return;

        registry[moduleType] = moduleClass;
    }

    public void Unregister(string moduleType) => registry.Remove(moduleType);

    public Module CreateModule(string moduleType, List<ParamSaveData> savedParams = null)
    {
        GameObject logicGO = new GameObject($"Logic_{moduleType}");

        logicGO.transform.SetParent(this.transform);

        Module module;

        if (registry.TryGetValue(moduleType, out System.Type customType))
        {
            module = (Module)logicGO.AddComponent(customType);
        }
        else
        {
            module = moduleType switch
            {
                "Oscillator" => logicGO.AddComponent<Oscilator>(),
                "Filter" => logicGO.AddComponent<Filter>(),
                "Amplifier" => logicGO.AddComponent<Amplifier>(),
                "Envelope" => logicGO.AddComponent<Envelope>(),
                "Mixer" => logicGO.AddComponent<Mixer>(),
                "Pitch Quantizer" => logicGO.AddComponent<Quantizer>(),
                "StepSequencer" => logicGO.AddComponent<StepSequencer>(),
                "LFO" => logicGO.AddComponent<LFO>(),
                "Distorsion" => logicGO.AddComponent<Distorsion>(),
                "Clock" => logicGO.AddComponent<Clock>(),
                "SampleHold"   => logicGO.AddComponent<SampleHold>(),
                "Oscilloscope" => logicGO.AddComponent<Oscilloscope>(),
                "Attenuverter" => logicGO.AddComponent<Attenuverter>(),
                "RingMod" => logicGO.AddComponent<RingMod>(),
                "Delay" => logicGO.AddComponent<Delay>(),
                "Reverb" => logicGO.AddComponent<Reverb>(),
                _ => null
            };
        }

        if (module == null)
        {
            Destroy(logicGO);
            return null;
        }

        PatchManager.Instance.RegisterModule(module, moduleType);

        if (savedParams != null && savedParams.Count > 0)
            module.ApplyParameters(savedParams);

        return module;
    }
}