using System.Collections.Generic;
using UnityEngine;

public class Filter : Module
{
    public FilterType filter;

    [Range(20f, 22000f)]
    public float cutoffFrequency = 1000f;

    [Range(0.1f, 10f)]
    public float resonance = 1f;

    //BiQuad Coefficients

    //Feedforward - input
    private float b0, b1, b2;
    //Feedback - output
    private float a1, a2;

    //Input delays (x[n-1], x[n-2])
    private float x1, x2;
    //Output delays (y[n-1], y[n-2])
    private float y1, y2;

    private double sampling_freq;

    private float lastComputedCutoff = -1f;

    public override List<ModuleParameter> GetParameters()
    {
        return new List<ModuleParameter>
        {
            ModuleParameter.Knob("cutoff", "Cutoff", cutoffFrequency, 20f, 22000f, v => { cutoffFrequency = v; updateCoefficients(cutoffFrequency, resonance); }, curve: ParameterCurve.LOGARITHMIC),
            ModuleParameter.Knob("resonance", "Res", resonance, 0.1f, 10f, v => { resonance = v; updateCoefficients(cutoffFrequency, resonance); }, curve: ParameterCurve.LOGARITHMIC),
            ModuleParameter.Dropdown("type", "Type", new string[] { "LP", "HP", "BP" }, (int)filter, i => filter = (FilterType)i)
        };
    }

    protected override void Initialize()
    {

        AddPort("audio_in", "AUDIO IN", PortType.AUDIO, PortDir.INPUT);
        AddPort("cutoff_cv", "CUTOFF CV", PortType.MODCV, PortDir.INPUT);
        AddPort("audio_out", "AUDIO OUT", PortType.AUDIO, PortDir.OUTPUT);

        sampling_freq = AudioSettings.outputSampleRate;

        filter = FilterType.LowPass;

        x1 = x2 = 0f;
        y1 = y2 = 0f;

        updateCoefficients(cutoffFrequency, resonance);
    }

    public override float[] execute(float[] data, CV cv)
    {
        if (TryGetFrameCache(data.Length, out float[] cache))
        {
            System.Array.Copy(cache, data, data.Length);
            return data;
        }

        float[] audio = ReadInputPort("audio_in", data.Length);
        float[] cutoffCV = ReadInputPort("cutoff_cv", data.Length);

        if (audio == null)
        {
            System.Array.Clear(data, 0, data.Length);
            SaveToFrameCache(data);
            return data;
        }

        for (int i = 0; i < data.Length; i++)
        {
            float effectiveCutoff = cutoffFrequency;

            if (cutoffCV != null)
            {
                effectiveCutoff = cutoffFrequency * Mathf.Pow(2f, cutoffCV[i] / CVStandard.VOLTS_PER_OCTAVE);
                effectiveCutoff = Mathf.Clamp(effectiveCutoff, 20f, 22000f);

                if (Mathf.Abs(effectiveCutoff - lastComputedCutoff) > 0.5f)
                {
                    updateCoefficients(effectiveCutoff, resonance);
                }

                /*
                float normalized = CVStandard.BipolarToUnipolar(cutoffCV[i]);
                float minLog  = Mathf.Log(20f, 2f);
                float maxLog  = Mathf.Log(22000f, 2f);
                float freq    = Mathf.Pow(2f, Mathf.Lerp(minLog, maxLog, normalized));

                updateCoefficients(Mathf.Clamp(freq, 20f, 22000f), resonance);
                */
            }



            float output = b0 * audio[i] + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;

            x2 = x1;
            x1 = audio[i];
            y2 = y1;
            y1 = output;

            data[i] = output;
        }

        SaveToFrameCache(data);

        return data;
    }


    //Robert Bristow-Johnson - BiQuad
    private void updateCoefficients(float cutoffFreq, float res)
    {
        //Prevent invalid freq
        cutoffFreq = Mathf.Clamp(cutoffFreq, 20f, (float)sampling_freq * 0.49f);

        //Prevent invalid res
        res = Mathf.Max(res, 0.001f);

        float omega = 2.0f * Mathf.PI * cutoffFreq / (float)sampling_freq;
        float sinOmega = Mathf.Sin(omega);
        float cosOmega = Mathf.Cos(omega);
        float alpha = sinOmega / (2.0f * res);


        //Debug.Log("Omega: " + omega + ", SinOmega: " + sinOmega + ", CosOmega: " + cosOmega + ", Alpha: " + alpha);

        float tempB0, tempB1, tempB2, tempA0, tempA1, tempA2;

        switch (filter)
        {
            case FilterType.LowPass:
                tempB0 = (1f - cosOmega) / 2f;
                tempB1 = 1f - cosOmega;
                tempB2 = (1f - cosOmega) / 2f;
                tempA0 = 1f + alpha;
                tempA1 = -2f * cosOmega;
                tempA2 = 1f - alpha;
                break;

            case FilterType.HighPass:
                tempB0 = (1f + cosOmega) / 2f;
                tempB1 = -(1f + cosOmega);
                tempB2 = (1f + cosOmega) / 2f;
                tempA0 = 1f + alpha;
                tempA1 = -2f * cosOmega;
                tempA2 = 1f - alpha;
                break;

            case FilterType.BandPass:
                tempB0 = alpha;
                tempB1 = 0f;
                tempB2 = -alpha;
                tempA0 = 1f + alpha;
                tempA1 = -2f * cosOmega;
                tempA2 = 1f - alpha;
                break;

            default: //pass-through
                tempB0 = 1f;
                tempB1 = 0f;
                tempB2 = 0f;
                tempA0 = 1f;
                tempA1 = 0f;
                tempA2 = 0f;
                break;

        }

        //Normalize coeficients
        b0 = tempB0 / tempA0;
        b1 = tempB1 / tempA0;
        b2 = tempB2 / tempA0;
        a1 = tempA1 / tempA0;
        a2 = tempA2 / tempA0;

        lastComputedCutoff = cutoffFreq;

        /*
        if (Mathf.Abs(a2) >= 1f || Mathf.Abs(a1) >= 1f + a2)
        {
            Debug.LogWarning($"Filter unstable - a1={a1}, a2={a2}, freq={cutoffFreq}, res={res}");
        }
        */
    }


    public enum FilterType
    {
        LowPass,
        HighPass,
        BandPass
    }
}