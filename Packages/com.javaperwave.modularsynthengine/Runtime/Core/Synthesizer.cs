using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Synthesizer : MonoBehaviour
{
    public static Synthesizer Instance {
        get; 
        private set; 
    }

    public bool run = true;

    private MasterOutModule masterOut;

    private void Awake()
    {
        if (Instance != null && Instance != this) { 
            Destroy(gameObject); 
            return; 
        }

        Instance = this;

        masterOut = gameObject.AddComponent<MasterOutModule>();

        masterOut.isPermanent = true;

        int bufferSize;
        int numBuffers;
        AudioSettings.GetDSPBufferSize(out bufferSize, out numBuffers);
        Debug.Log($"Buffer size: {bufferSize} | Num buffers: {numBuffers} | SR: {AudioSettings.outputSampleRate}");
    }

    public void Start()
    {
        PatchManager.Instance.RegisterModule(masterOut, "MasterOut");
    }

    public MasterOutModule GetMasterOut() => masterOut;

    private int debugCounter = 0;

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (!run) { return; }

        var inputs = masterOut.GetInputPorts();

        if (inputs.Count == 0 || inputs[0].connection == null) { return; }

        masterOut.execute(data, null);

        for (int i = 0; i < data.Length; i += channels)
            if (channels == 2) data[i + 1] = data[i];
    }
}
