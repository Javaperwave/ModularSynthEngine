using UnityEngine;

[System.Serializable]
public class Port
{
    public string id;
    public string label;
    public PortType type;

    public PortDir dir;

    public Module owner;

    public CV connection;

    [System.NonSerialized]
    public float[] buffer;

    public bool isConnected => connection != null && connection.source != null;

    public Port (string id, string label, PortType type, PortDir dir, Module owner) {
        this.id = id;
        this.label = label;
        this.type = type;
        this.dir = dir;
        this.owner = owner;
    }

    public bool CanConnectTo(Port other) {
        
        if (dir == other.dir) {
            return false;
        }

        return true;
    }
    
}

public enum PortType {
    AUDIO,
    PITCHCV,
    MODCV,
    GATE,
    TRIGGER
}

public enum PortDir {
    INPUT,
    OUTPUT
}