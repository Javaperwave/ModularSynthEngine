using System.Collections.Generic;
using UnityEngine;

public class LFO : Module
{
    //ENTRADAS
    //freq_cv  - Frecuencia del LFO (1V/Oct, MODCV ±5V)
    //reset_in - Rising edge: reinicia fase y ciclo de delay/fade (TRIGGER 5V)+

    //SALIDAS
    //cv_out - Forma de onda (MODCV ±5V bipolar)

    public float freq = 1f; //Frequency in Hz

    private float phase;
    private float increment;

    private double sampling_freq;

    //[Range(0, 12)]
    //private double amplitude = 1;


    private const float amplitude = CVStandard.MOD_CV_MAX; //Bipolar CV in +/-5 V range

    [Range(0.01f, 0.99f)]
    public float pwm = 0.5f;

    public WaveformType waveform;

    private bool wasResetHigh = false;

    //Random
    private readonly System.Random rng = new System.Random();
    private float shValue = 0f;
    private int shCycleCount = 0;

    public float delayTime = 0f;
    private float delayCounter = 0f;
    private bool inDelay = false;

    public float fadeTime = 0f;
    private float fadeCounter = 0f;
    private float fadeGain = 1f;
    private bool inFade = false;


    public override List<ModuleParameter> GetParameters()
    {
        return new List<ModuleParameter>
        {
            ModuleParameter.Knob("freq", "Freq", freq, 0.01f, 20f, v => freq = v, curve: ParameterCurve.LOGARITHMIC),
            ModuleParameter.Knob("pwm", "PWM", pwm, 0.01f, 0.99f, v => pwm = v),
            ModuleParameter.Knob("delayTime", "Delay", delayTime, 0f, 10f, v => delayTime = v),
            ModuleParameter.Knob("fadeTime",  "Fade",  fadeTime,  0f, 10f, v => fadeTime  = v),
            ModuleParameter.Dropdown("waveform", "Wave", new string[] { "SIN", "TRI", "SAW", "SQR", "RND" }, (int)waveform, i => waveform = (WaveformType)i)
        };
    }


    protected override void Initialize()
    {
        AddPort("freq_cv", "FREQ CV", PortType.MODCV, PortDir.INPUT);
        AddPort("reset_in", "RESET IN", PortType.TRIGGER, PortDir.INPUT);  //Sync/Reset
        AddPort("cv_out", "CV OUT", PortType.MODCV, PortDir.OUTPUT);

        fadeGain = 1f;
        inDelay = false;
        inFade = false;

        sampling_freq = AudioSettings.outputSampleRate;
        //squareWaveText.enabled = false;
    }

    public override float[] execute(float[] data, CV cv)
    {
        if (TryGetFrameCache(data.Length, out float[] cache))
        {
            System.Array.Copy(cache, data, data.Length);
            return data;
        }

        /*
        float[] freqCV = null;
        float[] resetCV = null;

        if (inputsCV.Length > 0 && inputsCV[0] != null)
        {
            freqCV = inputsCV[0].source.execute(new float[data.Length], inputsCV[0]);
        }

        if (inputsCV.Length > 1 && inputsCV[1] != null)
        {
            resetCV = inputsCV[1].source.execute(new float[data.Length], inputsCV[1]);
        }
        */

        float[] freqCV = ReadInputPort("freq_cv", data.Length);
        float[] resetCV = ReadInputPort("reset_in", data.Length);

        generateWave(data, freqCV, resetCV);

        SaveToFrameCache(data);

        return data;
    }

    private void generateWave(float[] data, float[] freqCV, float[] resetCV)
    {
        float nyquist = (float)(sampling_freq * 0.5);

        float delaySamples = delayTime * (float)sampling_freq;
        float fadeSamples = fadeTime * (float)sampling_freq;

        for (int i = 0; i < data.Length; i++)
        {
            if (resetCV != null)
            {
                bool isResetHigh = CVStandard.IsGateActive(resetCV[i]);

                if (isResetHigh && !wasResetHigh)
                {
                    phase = 0f;

                    delayCounter = 0f;
                    fadeCounter = 0f;
                    fadeGain = 0f;

                    inDelay = delayTime > 0f;
                    inFade = !inDelay && fadeTime > 0f;


                    if (!inDelay && !inFade)
                        fadeGain = 1f;
                }

                wasResetHigh = isResetHigh;
            }

            if (inDelay)
            {
                delayCounter++;
                if (delayCounter >= delaySamples)
                {
                    inDelay = false;

                    if (fadeTime > 0f)
                    {
                        inFade = true;
                        fadeCounter = 0f;
                        fadeGain = 0f;
                    }
                    else
                    {
                        fadeGain = 1f;
                    }
                }
            }

            if (inFade)
            {
                fadeCounter++;
                fadeGain = Mathf.Clamp01(fadeCounter / fadeSamples);
                if (fadeCounter >= fadeSamples)
                {
                    inFade = false;
                    fadeGain = 1f;
                }
            }

            float frequency = freq;

            if (freqCV != null)
            {
                frequency = freq * Mathf.Pow(2f, freqCV[i] / CVStandard.VOLTS_PER_OCTAVE);

                //frequency = Mathf.Max(0.01f, freq * (1f + freqCV[i] / CVStandard.MOD_CV_MAX));
                //frequency = freq * CVStandard.CVToFrequency(freqCV[i]); //CV a 1V/Ocatve
                //frequency = freq * Mathf.Pow(2f, freqCV[i]); //Modify base frequency
                //frequency = freq * Mathf.Pow(2f, freqCV[i] / 12f); //Semitone modification
            }

            frequency = Mathf.Clamp(frequency, 0.001f, nyquist - 1f);

            increment = (float)(frequency * 2.0 * Mathf.PI / sampling_freq);
            phase += increment;

            if (phase > (Mathf.PI * 2))
            {
                phase -= (Mathf.PI * 2);

                if (waveform == WaveformType.RANDOM)
                {
                    shCycleCount++;
                    if (shCycleCount >= 2)
                    {
                        shCycleCount = 0;
                        shValue = (float)(rng.NextDouble() * 2.0 - 1.0) * amplitude;
                    }
                }
            }


            float sample = 0f;

            switch (waveform)
            {
                case WaveformType.SIN:
                    //data[i] = Mathf.Sin(phase) * amplitude;                
                    sample = Mathf.Sin(phase) * amplitude;
                    break;

                case WaveformType.SAW:
                    //data[i] = 2f * (phase / (2f * Mathf.PI) - Mathf.Floor(phase / (2f * Mathf.PI) + 0.5f)) * amplitude;
                    //data[i] = (2f * (phase / (2f * Mathf.PI)) - 1f) * amplitude;
                    sample = (2f * (phase / (2f * Mathf.PI)) - 1f) * amplitude;
                    break;

                case WaveformType.TRIANGLE:
                    //data[i] = (2f / Mathf.PI) * Mathf.Asin(Mathf.Sin(phase)) * amplitude;
                    sample = (2f / Mathf.PI) * Mathf.Asin(Mathf.Sin(phase)) * amplitude;
                    break;

                case WaveformType.SQUARE:
                    if ((phase / (2 * Mathf.PI)) % 1.0f < pwm)
                    {
                        //data[i] = (float)amplitude;
                        sample = (float)amplitude;
                    }
                    else
                    {
                        //data[i] = -(float)amplitude;
                        sample = -(float)amplitude;
                    }
                    break;

                case WaveformType.RANDOM:
                    sample = shValue;
                    break;
            }

            data[i] = sample * (inDelay ? 0f : fadeGain);

        }
    }

    public enum WaveformType
    {
        SIN,
        TRIANGLE,
        SAW,
        SQUARE,
        RANDOM  //S&H
    }
}