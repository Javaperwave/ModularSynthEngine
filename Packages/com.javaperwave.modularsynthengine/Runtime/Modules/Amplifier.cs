using System.Collections.Generic;
using UnityEngine;

public class Amplifier : Module
{
    //ENTRADAS
    //Audio In - Audio a amplificar
    //CV In - nivel de ganancia

    //SALIDAS
    //Audio Out - señal de audio amplificada

    [Range(0f, 2f)]
    public float gain = 1f;

    public ResponseType response = ResponseType.LIN;

    public override List<ModuleParameter> GetParameters()
    {
        return new List<ModuleParameter>
        {
            ModuleParameter.Knob("gain", "Gain", gain, 0f, 2f, v => gain = v),
            ModuleParameter.Dropdown("response", "Resp", new string[] { "LIN", "EXP" }, (int)response, i => response = (ResponseType)i)
        };
    }

    protected override void Initialize() {

        AddPort("audio_in", "AUDIO IN", PortType.AUDIO, PortDir.INPUT);
        AddPort("gain_cv", "GAIN CV", PortType.MODCV, PortDir.INPUT);
        AddPort("audio_out", "AUDIO OUT", PortType.AUDIO, PortDir.OUTPUT);

    }

    public override float[] execute(float[] data, CV cv)
    {
        if (TryGetFrameCache(data.Length, out float[] cache))
        {
            System.Array.Copy(cache, data, data.Length);
            return data;
        }
        
        float[] audioSignal = ReadInputPort("audio_in", data.Length);
        float[] cvGain = ReadInputPort("gain_cv",  data.Length);

        if (audioSignal == null)
        {
            System.Array.Clear(data, 0, data.Length);
            SaveToFrameCache(data);
            
            return data;
        }

        if (cvGain == null)
        {
            for (int i = 0; i < data.Length; i++)
                data[i] = audioSignal[i] * gain;

            SaveToFrameCache(data);
            return data;
        }

        for (int i = 0; i < data.Length; i++)
        {
            float cvAmount = Mathf.Clamp01(cvGain[i] / CVStandard.UNIPOLAR_MAX);
            //sampleGain = gain * Mathf.Clamp01(cvGain[i] / CVStandard.UNIPOLAR_MAX);
            //sampleGain = Mathf.Clamp(gain + cvGain[i] / CVStandard.UNIPOLAR_MAX, 0f, 2f);
 
            if (response == ResponseType.EXP)
                cvAmount = cvAmount * cvAmount;
 
            float sampleGain = gain * cvAmount;
 
            data[i] = audioSignal[i] * sampleGain;
        }
 

        SaveToFrameCache(data);
        
        return data;
    }

    public enum ResponseType
    {
        LIN,
        EXP
    }

}
