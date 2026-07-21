using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Module : MonoBehaviour
{
    public string moduleId;
    public string moduleType;

    public bool isPermanent = false;

    //new for managing patch
    protected Dictionary <string, Port> ports = new Dictionary <string, Port> ();

    private List<Port> cachedInputPorts;
    private List<Port> cachedOutputPorts;

    public abstract float[] execute(float[] data, CV cv);

    protected virtual void Awake()
    {
        Initialize();

        if (string.IsNullOrEmpty(moduleId) && PatchManager.Instance != null)
            PatchManager.Instance.RegisterModule(this);
    }
    protected abstract void Initialize();

    private float[] frameCache;
    private double lastCacheDspTime = -1.0;
 

    protected bool TryGetFrameCache(int bufferSize, out float[] cache)
    {
        double now = AudioSettings.dspTime;
 
        if (now == lastCacheDspTime && frameCache != null && frameCache.Length == bufferSize)
        {
            cache = frameCache;
            return true;
        }
 
        if (frameCache == null || frameCache.Length != bufferSize)
            frameCache = new float[bufferSize];
 
        lastCacheDspTime = now;
        cache = frameCache;
        return false;
    }
 
    protected void SaveToFrameCache(float[] data)
    {
        if (frameCache != null && frameCache.Length == data.Length)
            Array.Copy(data, frameCache, data.Length);
    }

    protected float[] ReadInputPort(string portId, int bufferSize)
    {
        if (!ports.TryGetValue(portId, out Port port)) return null;
        if (port.connection == null || port.connection.source == null) return null;
 
        if (port.buffer == null || port.buffer.Length != bufferSize)
            port.buffer = new float[bufferSize];
 
        return port.connection.source.execute(port.buffer, port.connection);
    }


    //new for managing patch
    protected Port AddPort(string id, string label, PortType type, PortDir dir) {
 
        var port = new Port(id, label, type, dir, this);
        ports[id] = port;
 
        cachedInputPorts = null;
        cachedOutputPorts = null;
 
        return port;
    }

    
    public Port GetPort(string id) {

        ports.TryGetValue(id, out Port port);
        
        return port;
    }

    public List<Port> GetInputPorts() {
 
        if (cachedInputPorts != null) return cachedInputPorts;
 
        cachedInputPorts = new List<Port>();

        foreach (var p in ports.Values)
        {
            if (p.dir == PortDir.INPUT) cachedInputPorts.Add(p);
        }

        return cachedInputPorts;
    }

    public List<Port> GetOutputPorts() {
 
        if (cachedOutputPorts != null) return cachedOutputPorts;
 
        cachedOutputPorts = new List<Port>();

        foreach (var p in ports.Values)
        {
            if (p.dir == PortDir.OUTPUT) cachedOutputPorts.Add(p);
        }

        return cachedOutputPorts;
    }


    public void ConnectInput(string portId, CV cv) {
 
        if (ports.TryGetValue(portId, out Port port))
        {
            port.connection = cv;
        }
    }

    public void DisconnectInput(string portId) {
 
        if (ports.TryGetValue(portId, out Port port))
        {
            port.connection = null;
        }
    }

    public virtual List<ModuleParameter> GetParameters() => new List<ModuleParameter>();

    public void ApplyParameters(List<ParamSaveData> savedParams)
    {
        if (savedParams == null || savedParams.Count == 0) return;
 
        var currentParams = GetParameters();
        foreach (var saved in savedParams)
            ApplyParamById(saved, currentParams);
    }

    private void ApplyParamById(ParamSaveData saved, List<ModuleParameter> paramList)
    {
        foreach (var param in paramList)
        {
            if (param.id == saved.id)
            {
                switch (param.type)
                {
                    case ParameterType.KNOB:
                    case ParameterType.SLIDER:
                        param.onKnobChanged?.Invoke(saved.floatValue);
                        break;
                    case ParameterType.TOGGLE:
                        param.onToggleChanged?.Invoke(saved.boolValue);
                        break;
                    case ParameterType.DROPDOWN:
                        param.onDropdownChanged?.Invoke(saved.intValue);
                        break;
                }
                return;
            }
 
            if (param.type == ParameterType.GROUP && param.children != null)
                ApplyParamById(saved, param.children);
        }
    }


    //Public helpers
    public void SetParameter(string id, float value)
    {
        var p = FindParam(id, GetParameters());
        if (p == null) return;
        if (p.type == ParameterType.KNOB || p.type == ParameterType.SLIDER)
            p.onKnobChanged?.Invoke(value);
    }

    public void SetParameter(string id, bool value)
    {
        var p = FindParam(id, GetParameters());
        if (p == null) return;
        if (p.type == ParameterType.TOGGLE)
            p.onToggleChanged?.Invoke(value);
    }

    public void SetParameter(string id, int value)
    {
        var p = FindParam(id, GetParameters());
        if (p == null) return;
        if (p.type == ParameterType.DROPDOWN)
            p.onDropdownChanged?.Invoke(value);
    }

    private ModuleParameter FindParam(string id, List<ModuleParameter> list)
    {
        if (list == null) return null;
        foreach (var param in list)
        {
            if (param.id == id) return param;
            if (param.type == ParameterType.GROUP && param.children != null)
            {
                var found = FindParam(id, param.children);
                if (found != null) return found;
            }
        }
        return null;
    }

}
