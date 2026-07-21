using System.Collections;
using System.Collections.Generic;
//using UnityEditor.U2D.Path;
using UnityEngine;

public class Distorsion : Module
{
    //ENTRADAS
    //Audio In - Audio a distorsionar
    //CV In - nivel de drive
    //CV In - nivel de dry/wet mix

    //SALIDAS
    //Audio Out - forma de onda generada

    [Range(0f, 10f)]
    public float drive = 1f;

    [Range(0f, 1f)]
    public float drywet = 1f;

    public DistortionType distortionType;

    private const float audio_peak = CVStandard.UNIPOLAR_MAX;

    private float bitcrushPhase = 0f;
    private float bitcrushHeld  = 0f;

    public override List<ModuleParameter> GetParameters() => new List<ModuleParameter>
    {
        ModuleParameter.Knob("drive",  "Drive",   drive,  0f, 10f, v => drive  = v),
        ModuleParameter.Knob("drywet", "Dry/Wet", drywet, 0f, 1f,  v => drywet = v),
        ModuleParameter.Dropdown("type", "Type", new string[] { "SOFT", "HARD", "FOLD", "BIT" }, (int)distortionType, i => distortionType = (DistortionType)i)
    };

    protected override void Initialize()
    {
        AddPort("audio_in", "AUDIO IN", PortType.AUDIO, PortDir.INPUT);
        AddPort("drive_cv", "DRIVE CV", PortType.MODCV, PortDir.INPUT);
        AddPort("drywet_cv", "MIX CV", PortType.MODCV, PortDir.INPUT);
        AddPort("audio_out", "AUDIO OUT", PortType.AUDIO, PortDir.OUTPUT);

        //sampling_freq = AudioSettings.outputSampleRate;
    }


    public override float[] execute(float[] data, CV cv)
    {
        if (TryGetFrameCache(data.Length, out float[] cache))
        {
            System.Array.Copy(cache, data, data.Length);
            return data;
        }

        float[] signal = ReadInputPort("audio_in", data.Length);
        float[] driveCV = ReadInputPort("drive_cv", data.Length);
        float[] drywetCV = ReadInputPort("drywet_cv", data.Length);

        if (signal == null)
        {
            System.Array.Clear(data, 0, data.Length);
            SaveToFrameCache(data);
 
            return data;
        }

        for (int i = 0; i < data.Length; i++)
        {
            float d = drive;
 
            if (driveCV != null)
            {
                if (distortionType == DistortionType.BITCRUSH)
                    d = Mathf.Clamp(drive + driveCV[i], 0f, 10f);
                else
                    d = Mathf.Clamp(drive + driveCV[i], 1f, 50f);
                //d = 1f + CVStandard.BipolarToUnipolar(driveCV[i]) * 49f;
            }
            
            float mix = drywet;

            if (drywetCV != null)
            {
                //mix = Mathf.Clamp01(drywet + drywetCV[i] / (CVStandard.MOD_CV_MAX * 2f));
                mix = Mathf.Clamp01(drywet + drywetCV[i] / (CVStandard.MOD_CV_MAX));
            }

            //float dry = signal[i];
            float dry = signal[i] / audio_peak; //Normalized

            float input = (distortionType == DistortionType.BITCRUSH) ? dry : dry * d;

            float wet = distort(input, distortionType, d);

            float mixed = Mathf.Lerp(dry, wet, mix);

            //data[i] = Mathf.Lerp(dry, wet, drywet);
            data[i] = mixed * audio_peak;
        }
 
        SaveToFrameCache(data);

        return data;
    }


    private float distort(float x, DistortionType type, float drive)
    {
        switch (type)
        {
            case DistortionType.SOFTCLIP:
                return x / (1f + Mathf.Abs(x));

            case DistortionType.HARDCLIP:
                return Mathf.Clamp(x, -1f, 1f);

            case DistortionType.WAVEFOLD:
                float t = ((x + 1f) % 4f + 4f) % 4f;
                if (t > 2f) t = 4f - t;
                return t - 1f;

            case DistortionType.BITCRUSH:
                float crush = Mathf.Clamp01(drive / 10f);
 
                float phaseInc = Mathf.Lerp(1f, 0.02f, crush);
                bitcrushPhase += phaseInc;
                
                if (bitcrushPhase >= 1f)
                {
                    bitcrushPhase -= 1f;
                    
                    int bits = Mathf.RoundToInt(Mathf.Lerp(12f, 1f, crush));
                    float levels = Mathf.Pow(2f, bits - 1f);
                    float xNorm = Mathf.Clamp(x, -1f, 1f);
                    bitcrushHeld = Mathf.Round(xNorm * levels) / levels;
                }
                return bitcrushHeld;
            default: 
                return x;
        }
    }

    public enum DistortionType
    {
        SOFTCLIP,
        HARDCLIP,
        WAVEFOLD,
        BITCRUSH
    }
}
