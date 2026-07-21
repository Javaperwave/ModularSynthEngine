using System.Collections.Generic;
using UnityEngine;

public class Delay : Module
{
    //ENTRADAS
    //Audio In - señal a retardar
    //CV In - tiempo de delay (en ms)
    //CV In - feedback
    //CV In - dry/wet

    //SALIDAS
    //Audio Out - señal mezclada con la cola del delay

    [Range(1f, 2000f)]
    public float timeMs = 250f;

    [Range(0f, 0.95f)]
    public float feedback = 0.4f;

    [Range(0f, 1f)]
    public float drywet = 0.5f;

    private float[] delayBuffer;
    private int writeIndex;

    private double sampling_freq;

    private const float MAX_DELAY_SECONDS = 2f;

    public override List<ModuleParameter> GetParameters() => new List<ModuleParameter>
    {
        ModuleParameter.Knob("time", "Time (ms)", timeMs, 1f, 2000f, v => timeMs = v, curve: ParameterCurve.LOGARITHMIC),
        ModuleParameter.Knob("feedback", "Feedback", feedback, 0f, 0.95f, v => feedback = v),
        ModuleParameter.Knob("drywet", "Dry/Wet", drywet, 0f, 1f, v => drywet = v)
    };

    protected override void Initialize()
    {
        AddPort("audio_in", "AUDIO IN", PortType.AUDIO, PortDir.INPUT);
        AddPort("time_cv", "TIME CV", PortType.MODCV, PortDir.INPUT);
        AddPort("fb_cv", "FB CV", PortType.MODCV, PortDir.INPUT);
        AddPort("drywet_cv", "MIX CV", PortType.MODCV, PortDir.INPUT);
        AddPort("audio_out", "AUDIO OUT", PortType.AUDIO, PortDir.OUTPUT);

        sampling_freq = AudioSettings.outputSampleRate;

        int bufferLength = Mathf.CeilToInt(MAX_DELAY_SECONDS * (float)sampling_freq);
        delayBuffer = new float[bufferLength];
        writeIndex = 0;
    }

    public override float[] execute(float[] data, CV cv)
    {
        if (TryGetFrameCache(data.Length, out float[] cache))
        {
            System.Array.Copy(cache, data, data.Length);
            return data;
        }

        float[] audio = ReadInputPort("audio_in", data.Length);
        float[] timeCV = ReadInputPort("time_cv", data.Length);
        float[] fbCV = ReadInputPort("fb_cv", data.Length);
        float[] mixCV = ReadInputPort("drywet_cv", data.Length);

        if (audio == null)
        {
            System.Array.Clear(data, 0, data.Length);
            SaveToFrameCache(data);

            return data;
        }

        int bufferLength = delayBuffer.Length;

        for (int i = 0; i < data.Length; i++)
        {
            float effectiveTime = timeMs;

            if (timeCV != null)
            {
                effectiveTime = Mathf.Clamp(timeMs + timeCV[i] * (1000f / CVStandard.MOD_CV_MAX), 1f, 2000f);
            }

            float delaySamplesF = (effectiveTime / 1000f) * (float)sampling_freq;
            int delaySamples = (int)delaySamplesF;
            float frac = delaySamplesF - delaySamples;


            int readIndex = writeIndex - delaySamples;
            if (readIndex < 0) readIndex += bufferLength;

            int readIndexNext = readIndex - 1;
            if (readIndexNext < 0) readIndexNext += bufferLength;

            float delayed = Mathf.Lerp(delayBuffer[readIndex], delayBuffer[readIndexNext], frac);

            float effectiveFeedback = feedback;

            if (fbCV != null)
            {
                effectiveFeedback = Mathf.Clamp(feedback + fbCV[i] / (CVStandard.MOD_CV_MAX * 2f), 0f, 0.95f);
            }

            delayBuffer[writeIndex] = audio[i] + delayed * effectiveFeedback;

            writeIndex++;
            if (writeIndex >= bufferLength) writeIndex = 0;

            float mix = drywet;

            if (mixCV != null)
            {
                mix = Mathf.Clamp01(drywet + mixCV[i] / (CVStandard.MOD_CV_MAX * 2f));
            }

            data[i] = Mathf.Lerp(audio[i], delayed, mix);
        }

        SaveToFrameCache(data);

        return data;
    }
}