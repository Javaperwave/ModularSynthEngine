using System.Collections.Generic;
using UnityEngine;

public class Attenuverter : Module
{
    //ENTRADAS
    //CV In

    //SALIDAS
    //CV Out - in * atten + offset
    //Si la entrada est  desconectada, sale offset constante (fuente DC)


    [Range(-1f, 1f)]
    public float atten = 1f;

    [Range(CVStandard.MOD_CV_MIN, CVStandard.MOD_CV_MAX)]
    public float offset = 0f;


    public override List<ModuleParameter> GetParameters()
    {
        return new List<ModuleParameter>
        {
            ModuleParameter.Knob("atten",  "Atten",  atten,  -1f, 1f, v => atten  = v),
            ModuleParameter.Knob("offset", "Offset", offset, CVStandard.MOD_CV_MIN, CVStandard.MOD_CV_MAX, v => offset = v),
        };
    }

    protected override void Initialize()
    {
        AddPort("cv_in",  "CV IN",  PortType.MODCV, PortDir.INPUT);
        AddPort("cv_out", "CV OUT", PortType.MODCV, PortDir.OUTPUT);
    }

    public override float[] execute(float[] data, CV cv)
    {
        if (TryGetFrameCache(data.Length, out float[] cache))
        {
            System.Array.Copy(cache, data, data.Length);
            return data;
        }

        float[] signal = ReadInputPort("cv_in", data.Length);

        if (signal == null)
        {
            for (int i = 0; i < data.Length; i++)
                data[i] = offset;

            SaveToFrameCache(data);
            return data;
        }

        for (int i = 0; i < data.Length; i++)
        {
            data[i] = signal[i] * atten + offset;
        }

        SaveToFrameCache(data);
        return data;
    }
}