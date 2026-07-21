using System.Collections.Generic;
using UnityEngine;

public class Oscilloscope : Module
{
    //ENTRADAS
    //Signal In  - senal a visualizar
    //Trig In

    //SALIDAS
    //Signal Out - passthrough de la entrada

    private const int RING_SIZE = 8192;

    [System.NonSerialized]
    public float[] ringBuffer = new float[RING_SIZE];


    [System.NonSerialized]
    public volatile int writeIndex = 0;

    [Range(64, 4096)]
    public int timebase = 1024;

    [Range(0.1f, 10f)]
    public float range = 5f;

    public TriggerMode triggerMode = TriggerMode.RISING;

    public Color traceColor = new Color(0.3f, 1f, 0.4f, 1f);


    public override List<ModuleParameter> GetParameters()
    {
        return new List<ModuleParameter>
        {
            ModuleParameter.Knob("timebase", "Time", timebase, 64f, 4096f, v => timebase = Mathf.RoundToInt(v), isInt: true, curve: ParameterCurve.LOGARITHMIC),
            ModuleParameter.Knob("range", "Range", range, 0.1f, 10f, v => range = v),
            ModuleParameter.Dropdown("trig_mode", "Trigger", new string[] { "FREE", "RISING" }, (int)triggerMode, i => triggerMode = (TriggerMode)i),
        };
    }


    protected override void Initialize()
    {
        AddPort("signal_in", "IN", PortType.AUDIO, PortDir.INPUT);
        AddPort("signal_out", "OUT", PortType.AUDIO, PortDir.OUTPUT);
    }


    public override float[] execute(float[] data, CV cv)
    {
        if (TryGetFrameCache(data.Length, out float[] cache))
        {
            System.Array.Copy(cache, data, data.Length);
            return data;
        }

        float[] input = ReadInputPort("signal_in", data.Length);

        if (input == null)
        {
            System.Array.Clear(data, 0, data.Length);
            SaveToFrameCache(data);
            return data;
        }


        System.Array.Copy(input, data, data.Length);

        int w = writeIndex;
        for (int i = 0; i < data.Length; i++)
        {
            ringBuffer[w] = data[i];
            w++;
            if (w >= RING_SIZE) w = 0;
        }

        writeIndex = w;

        SaveToFrameCache(data);
        return data;
    }

    public void GetSnapshot(float[] dest, int count)
    {
        if (dest == null || count <= 0) return;
        if (count > dest.Length) count = dest.Length;
        if (count > RING_SIZE) count = RING_SIZE;

        int w = writeIndex;
        int start = w - count;
        if (start < 0) start += RING_SIZE;

        for (int i = 0; i < count; i++)
        {
            int idx = start + i;
            if (idx >= RING_SIZE) idx -= RING_SIZE;
            dest[i] = ringBuffer[idx];
        }
    }


    public enum TriggerMode
    {
        FREE,
        RISING
    }
}