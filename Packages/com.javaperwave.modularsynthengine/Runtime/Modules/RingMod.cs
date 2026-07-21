using System.Collections.Generic;
using UnityEngine;

public class RingMod : Module
{
    //ENTRADAS
    //Audio In X - primera señal (carrier)
    //Audio In Y - segunda señal (modulator)
    //CV In - dry/wet

    //SALIDAS
    //Audio Out - X*Y (RING) o X*(1+Y)/2 (AM)

    public RingType ringType = RingType.RING;

    [Range(0f, 1f)]
    public float drywet = 1f;

    private const float audio_peak = CVStandard.UNIPOLAR_MAX; // +-5V

    public override List<ModuleParameter> GetParameters() => new List<ModuleParameter>
    {
        ModuleParameter.Knob("drywet", "Dry/Wet", drywet, 0f, 1f, v => drywet = v),
        ModuleParameter.Dropdown("type", "Type", new string[] { "RING", "AM" }, (int)ringType, i => ringType = (RingType)i)
    };

    protected override void Initialize()
    {
        AddPort("audio_x", "AUDIO X", PortType.AUDIO, PortDir.INPUT);
        AddPort("audio_y", "AUDIO Y", PortType.AUDIO, PortDir.INPUT);
        AddPort("drywet_cv", "MIX CV", PortType.MODCV, PortDir.INPUT);
        AddPort("audio_out", "AUDIO OUT", PortType.AUDIO, PortDir.OUTPUT);
    }

    public override float[] execute(float[] data, CV cv)
    {
        if (TryGetFrameCache(data.Length, out float[] cache))
        {
            System.Array.Copy(cache, data, data.Length);
            return data;
        }

        float[] x = ReadInputPort("audio_x", data.Length);
        float[] y = ReadInputPort("audio_y", data.Length);
        float[] mixCV = ReadInputPort("drywet_cv", data.Length);

        if (x == null || y == null)
        {
            System.Array.Clear(data, 0, data.Length);
            SaveToFrameCache(data);

            return data;
        }

        for (int i = 0; i < data.Length; i++)
        {
            float xn = x[i] / audio_peak;
            float yn = y[i] / audio_peak;

            float wet;

            if (ringType == RingType.RING)
            {
                //Ring
                wet = xn * yn;
            }
            else
            {
                //AM
                //out = x * (1 + y) / 2
                wet = xn * (1f + yn) * 0.5f;
            }

            //Dry = X (carrier)
            float dry = xn;

            float mix = drywet;

            if (mixCV != null)
            {
                mix = Mathf.Clamp01(drywet + mixCV[i] / (CVStandard.MOD_CV_MAX * 2f));
            }

            float mixed = Mathf.Lerp(dry, wet, mix);

            data[i] = mixed * audio_peak;
        }

        SaveToFrameCache(data);

        return data;
    }


    public enum RingType
    {
        RING,
        AM
    }
}