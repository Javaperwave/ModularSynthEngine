using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PatchFile{
    public string name;
    public string version = "1.0";
    public List<ModuleSaveData> modules = new List<ModuleSaveData>();
    public List<CableSaveData>  cables  = new List<CableSaveData>();
}

[Serializable]
public class ModuleSaveData{
    public string moduleId;
    public string moduleType;
    public Vector2 position;
    public List<ParamSaveData> parameters = new List<ParamSaveData>();
}

[Serializable]
public class ParamSaveData{
    public string id;
    public float floatValue;
    public bool boolValue;
    public int intValue;
}

[Serializable]
public class CableSaveData{
    public string fromModuleId;
    public string fromPortId;
    public string toModuleId;
    public string toPortId;
}