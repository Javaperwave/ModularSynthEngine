using System.Collections.Generic;
using UnityEngine;

public class SampleHold : Module
{
    //ENTRADAS
    //CV In    - senal a muestrear
    //Trig In  - dispara el muestreo en cada flanco de subida

    //SALIDAS
    //CV Out   - Ultimo valor muestreado, mantenido hasta el proximo trigger

    private float heldValue = 0f;

    private bool wasTrigHigh = false;

    public override List<ModuleParameter> GetParameters()
    {
        return new List<ModuleParameter>();
    }

    protected override void Initialize()
    {
        AddPort("cv_in",   "CV IN",   PortType.MODCV,   PortDir.INPUT);
        AddPort("trig_in", "TRIG IN", PortType.TRIGGER, PortDir.INPUT);
        AddPort("cv_out",  "CV OUT",  PortType.MODCV,   PortDir.OUTPUT);
    }

    public override float[] execute(float[] data, CV cv)
    {
        if (TryGetFrameCache(data.Length, out float[] cache))
        {
            System.Array.Copy(cache, data, data.Length);
            return data;
        }

        float[] cvIn = ReadInputPort("cv_in",   data.Length);
        float[] trig = ReadInputPort("trig_in", data.Length);

        if (trig == null)
        {
            for (int i = 0; i < data.Length; i++)
                data[i] = heldValue;

            SaveToFrameCache(data);
            return data;
        }

        for (int i = 0; i < data.Length; i++)
        {
            bool isTrigHigh = CVStandard.IsGateActive(trig[i]);

            if (isTrigHigh && !wasTrigHigh)
            {
                heldValue = (cvIn != null) ? cvIn[i] : 0f;
            }
            
            wasTrigHigh = isTrigHigh;

            data[i] = heldValue;
        }

        SaveToFrameCache(data);
        return data;
    }
}