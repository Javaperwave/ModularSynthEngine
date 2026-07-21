using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mixer : Module
{
    //Channel volume
    [Range(0f, 1f)]
    public float channel1 = 1.0f;

    [Range(0f, 1f)]
    public float channel2 = 1.0f;

    [Range(0f, 1f)]
    public float channel3 = 1.0f;

    [Range(0f, 1f)]
    public float channel4 = 1.0f;

    private static readonly string[] channelPortIds = { "in_1", "in_2", "in_3", "in_4" };


    public override List<ModuleParameter> GetParameters()
    {
        return new List<ModuleParameter>
        {
            ModuleParameter.Knob("ch1", "Ch 1", channel1, 0f, 1f, v => channel1 = v),
            ModuleParameter.Knob("ch2", "Ch 2", channel2, 0f, 1f, v => channel2 = v),
            ModuleParameter.Knob("ch3", "Ch 3", channel3, 0f, 1f, v => channel3 = v),
            ModuleParameter.Knob("ch4", "Ch 4", channel4, 0f, 1f, v => channel4 = v)
        };
    }

    protected override void Initialize() {

        AddPort("in_1", "INPUT 1", PortType.AUDIO, PortDir.INPUT);
        AddPort("in_2", "INPUT 2", PortType.AUDIO, PortDir.INPUT);
        AddPort("in_3", "INPUT 3", PortType.AUDIO, PortDir.INPUT);
        AddPort("in_4", "INPUT 4", PortType.AUDIO, PortDir.INPUT);
        AddPort("audio_out", "AUDIO OUT", PortType.AUDIO, PortDir.OUTPUT);

    }

    public override float[] execute(float[] data, CV cv)
    {
        if (TryGetFrameCache(data.Length, out float[] cache))
        {
            System.Array.Copy(cache, data, data.Length);
            return data;
        }

        float ch1 = channel1;
        float ch2 = channel2;
        float ch3 = channel3;
        float ch4 = channel4;

        System.Array.Clear(data, 0, data.Length);

        for (int ch = 0; ch < 4; ch++)
        {
            float[] channelData = ReadInputPort(channelPortIds[ch], data.Length);
            if (channelData == null) continue;
 
            float level;
            switch (ch)
            {
                case 0: 
                    level = ch1; 
                    break;
                case 1: 
                    level = ch2; 
                    break;
                case 2: 
                    level = ch3; 
                    break;
                default: 
                    level = ch4; 
                    break;
            }
 
            for (int i = 0; i < data.Length; i++)
                data[i] += channelData[i] * level;
        }
 
 
        SaveToFrameCache(data);
        return data;
    }
}
