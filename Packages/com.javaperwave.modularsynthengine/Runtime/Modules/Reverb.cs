using System.Collections.Generic;
using UnityEngine;

public class Reverb : Module
{

    [Range(0f, 1f)]
    public float size = 0.5f;

    [Range(0f, 1f)]
    public float damping = 0.3f;

    [Range(0f, 1f)]
    public float drywet = 0.3f;


    private class CombFilter
    {
        public float[] buffer;
        public int index;
        public float feedback;
        public float damp;
        public float lastFiltered;

        public CombFilter(int bufferLength)
        {
            buffer = new float[bufferLength];
            index = 0;
            feedback = 0.84f;
            damp = 0.2f;
            lastFiltered = 0f;
        }

        public float Process(float input)
        {
            float output = buffer[index];

            lastFiltered = output * (1f - damp) + lastFiltered * damp;

            buffer[index] = input + lastFiltered * feedback;

            index++;
            if (index >= buffer.Length) index = 0;

            return output;
        }
    }

    private class AllPassFilter
    {
        public float[] buffer;
        public int index;
        public const float feedback = 0.5f;

        public AllPassFilter(int bufferLength)
        {
            buffer = new float[bufferLength];
            index = 0;
        }

        public float Process(float input)
        {
            float bufOut = buffer[index];
            float output = -input + bufOut;

            buffer[index] = input + bufOut * feedback;

            index++;
            if (index >= buffer.Length) index = 0;

            return output;
        }
    }

    private CombFilter[] combs;
    private AllPassFilter[] allpasses;

    private double sampling_freq;

    private static readonly int[] combLengths = { 1116, 1188, 1277, 1356 };
    private static readonly int[] allpassLengths = { 556, 441 };

    public override List<ModuleParameter> GetParameters() => new List<ModuleParameter>
    {
        ModuleParameter.Knob("size", "Size", size, 0f, 1f, v => { size = v; updateFeedback(); }),
        ModuleParameter.Knob("damping", "Damping", damping, 0f, 1f, v => { damping = v; updateDamping();  }),
        ModuleParameter.Knob("drywet", "Dry/Wet", drywet,  0f, 1f, v => drywet = v)
    };

    protected override void Initialize()
    {
        AddPort("audio_in", "AUDIO IN", PortType.AUDIO, PortDir.INPUT);
        AddPort("size_cv", "SIZE CV", PortType.MODCV, PortDir.INPUT);
        AddPort("damp_cv", "DAMP CV", PortType.MODCV, PortDir.INPUT);
        AddPort("drywet_cv", "MIX CV", PortType.MODCV, PortDir.INPUT);
        AddPort("audio_out", "AUDIO OUT", PortType.AUDIO, PortDir.OUTPUT);

        sampling_freq = AudioSettings.outputSampleRate;

        float scale = (float)sampling_freq / 44100f;

        combs = new CombFilter[combLengths.Length];
        for (int i = 0; i < combLengths.Length; i++)
        {
            combs[i] = new CombFilter(Mathf.Max(1, Mathf.RoundToInt(combLengths[i] * scale)));
        }

        allpasses = new AllPassFilter[allpassLengths.Length];
        for (int i = 0; i < allpassLengths.Length; i++)
        {
            allpasses[i] = new AllPassFilter(Mathf.Max(1, Mathf.RoundToInt(allpassLengths[i] * scale)));
        }

        updateFeedback();
        updateDamping();
    }

    public override float[] execute(float[] data, CV cv)
    {
        if (TryGetFrameCache(data.Length, out float[] cache))
        {
            System.Array.Copy(cache, data, data.Length);
            return data;
        }

        float[] audio  = ReadInputPort("audio_in", data.Length);
        float[] sizeCV = ReadInputPort("size_cv", data.Length);
        float[] dampCV = ReadInputPort("damp_cv", data.Length);
        float[] mixCV  = ReadInputPort("drywet_cv", data.Length);

        if (audio == null)
        {
            System.Array.Clear(data, 0, data.Length);
            SaveToFrameCache(data);

            return data;
        }

        for (int i = 0; i < data.Length; i++)
        {
            if (sizeCV != null)
            {
                float effectiveSize = Mathf.Clamp01(size + sizeCV[i] / (CVStandard.MOD_CV_MAX * 2f));
                float fb = Mathf.Lerp(0.7f, 0.98f, effectiveSize);
                for (int c = 0; c < combs.Length; c++) combs[c].feedback = fb;
            }

            if (dampCV != null)
            {
                float effectiveDamp = Mathf.Clamp01(damping + dampCV[i] / (CVStandard.MOD_CV_MAX * 2f));
                for (int c = 0; c < combs.Length; c++) combs[c].damp = effectiveDamp * 0.4f;
            }

            float input = audio[i];

            //4 combs en paralelo
            float combSum = 0f;
            for (int c = 0; c < combs.Length; c++) combSum += combs[c].Process(input);
            combSum *= 0.25f;

            //2 all-pass en serie
            float wet = combSum;
            for (int a = 0; a < allpasses.Length; a++) wet = allpasses[a].Process(wet);

            //Mezcla dry/wet
            float mix = drywet;

            if (mixCV != null)
            {
                mix = Mathf.Clamp01(drywet + mixCV[i] / (CVStandard.MOD_CV_MAX * 2f));
            }

            data[i] = Mathf.Lerp(audio[i], wet, mix);
        }

        SaveToFrameCache(data);

        return data;
    }

    private void updateFeedback()
    {
        if (combs == null) return;

        float fb = Mathf.Lerp(0.7f, 0.98f, size);

        for (int i = 0; i < combs.Length; i++) combs[i].feedback = fb;
    }

    private void updateDamping()
    {
        if (combs == null) return;

        for (int i = 0; i < combs.Length; i++) combs[i].damp = damping * 0.4f;
    }
}