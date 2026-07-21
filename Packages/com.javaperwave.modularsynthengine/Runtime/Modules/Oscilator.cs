using System.Collections.Generic;
using UnityEngine;

public class Oscilator : Module
{
    [Range(-24, 24)]
    public int coarse = 0; //Semitones (-24..+24)
 
    [Range(-100f, 100f)]
    public float fine = 0f; //Fine tune(-100..+100)

    private float phase;
    private float increment;

    private double sampling_freq;

    [Range(0.01f, 0.99f)]
    public float pwm = 0.5f;

    private const float audio_peak = CVStandard.UNIPOLAR_MAX; // +-5V

    public WaveformType waveform;

    private float prevSyncValue = 0f;

    private readonly System.Random rng = new System.Random();

    public override List<ModuleParameter> GetParameters()
    {
        return new List<ModuleParameter>
        {
            ModuleParameter.Knob("coarse", "Coarse", coarse, -24f, 24f, v => coarse = Mathf.RoundToInt(v), isInt: true),
            ModuleParameter.Knob("fine", "Fine", fine, -100f, 100f, v => fine = v),
            ModuleParameter.Knob("pwm", "PWM", pwm, 0.01f, 0.99f, v => pwm = v),
            ModuleParameter.Dropdown("waveform", "Wave", new string[] { "SIN", "TRI", "SAW", "SQR", "NOISE" }, (int)waveform, i => waveform = (WaveformType)i)
        };
    }


    protected override void Initialize()
    {
        AddPort("freq_cv", "FREQ CV", PortType.PITCHCV, PortDir.INPUT);
        AddPort("pwm_cv", "PWM CV", PortType.MODCV, PortDir.INPUT);
        AddPort("sync_in",  "SYNC IN",  PortType.TRIGGER,  PortDir.INPUT);  //Hard sync
        AddPort("audio_out", "AUDIO OUT", PortType.AUDIO, PortDir.OUTPUT);

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

        float[] freqCV = ReadInputPort("freq_cv", data.Length);
        float[] pwmCV = ReadInputPort("pwm_cv",  data.Length);
        float[] syncCV = ReadInputPort("sync_in", data.Length);

        generateWave(data, freqCV, pwmCV, syncCV);

        SaveToFrameCache(data);

        return data;
    }

    private void generateWave(float[] data, float[] freqCV, float[] pwmCV, float[] syncCV)
    {
        float nyquist = (float)(sampling_freq * 0.5);

        for (int i = 0; i < data.Length; i++)
        {
            float pitchCV = (freqCV != null) ? freqCV[i] : 0f;

            //freq = C0 * 2^(coarse_oct + fine_oct + pitchCV)
            //CV = 0V - coarse = 0 - fine = 0 => C0 = 16.35 Hz (Do0)

            //float frequency = CVStandard.C0_FREQ * Mathf.Pow(2f, coarse / 12f + fine / 1200f + pitchCV);
            float frequency = CVStandard.C3_FREQ * Mathf.Pow(2f, coarse / 12f + fine / 1200f + pitchCV);
            frequency = Mathf.Clamp(frequency, 0.01f, nyquist - 1f);

            float pwmValue = pwm;

            if (pwmCV != null)
            {

                pwmValue = Mathf.Clamp(pwm + pwmCV[i] / (CVStandard.MOD_CV_MAX * 2f), 0.01f, 0.99f);

                //pwmValue = Mathf.Clamp01(pwmCV[i] / CVStandard.UNIPOLAR_MAX);
                //pwmValue = Mathf.Clamp(pwmCV[i], 0f, 1f);
                //Debug.Log(pwmValue);
            }

            increment = (float)(frequency * 2.0 * Mathf.PI / sampling_freq);
            phase += increment;

            if (syncCV != null)
            {
                float currSyncValue = syncCV[i];
                
                if (prevSyncValue < 0f && currSyncValue >= 0f)
                {
                    float frac = -prevSyncValue / (currSyncValue - prevSyncValue);

                    frac = Mathf.Clamp01(frac);

                    phase = (1f - frac) * increment;
                }
                prevSyncValue = currSyncValue;
            }

            if (phase > (Mathf.PI * 2))
            {
                phase -= (Mathf.PI * 2);
            }

            float t  = phase / (2f * Mathf.PI);
            float dt = (float)(frequency / sampling_freq);

            float sample = 0f;

            //Wave type
            switch (waveform)
            {
                case WaveformType.SIN:
                    //data[i] = (float)(Mathf.Sin((float)phase) * amplitude);

                    //data[i] = Mathf.Sin(phase);
                    sample = Mathf.Sin(phase);
                    break;

                case WaveformType.SAW:
                    //data[i] = (float)(amplitude - (amplitude / Mathf.PI * phase)); //original
                    //data[i] = (float)(2f * amplitude * (phase / (2f * Mathf.PI) - Mathf.Floor(phase / (2f * Mathf.PI) + 0.5f)));
                    //data[i] = (float)(Mathf.Sin((float)(phase * amplitude)) / amplitude);
                    //data[i] = 2f * (phase / (2f * Mathf.PI) - Mathf.Floor(phase / (2f * Mathf.PI) + 0.5f));
                    
                    //data[i] = 2f * (phase / (2f * Mathf.PI)) - 1f;
                    sample = 2f * t - 1f;
                    sample -= PolyBLEP(t, dt);
                    break;

                case WaveformType.TRIANGLE:
                    //data[i] = (float)(double)Mathf.PingPong(phase, 1.0f); //original
                    //data[i] = (float)((2.0 / Mathf.PI) * Mathf.Asin(Mathf.Sin((float)phase)) * amplitude);

                    //data[i] = (2f / Mathf.PI) * Mathf.Asin(Mathf.Sin(phase));
                    sample = (2f / Mathf.PI) * Mathf.Asin(Mathf.Sin(phase));
                    break;

                case WaveformType.SQUARE:
                    /*
                    if ((phase / (2 * Mathf.PI)) % 1.0f < pwmValue)
                    {
                        //data[i] = (float)amplitude;
                        data[i] = 1f;
                    }
                    else
                    {
                        //data[i] = -(float)amplitude;
                        data[i] = -1f;
                    }
                    */

                    sample = (t < pwmValue) ? 1f : -1f;
                    sample += PolyBLEP(t, dt); //flanco de subida
                    float tDown = t - pwmValue; if (tDown < 0f) tDown += 1f; //flanco de bajada
                    sample -= PolyBLEP(tDown, dt);
                    break;

                case WaveformType.NOISE:
                    //data[i] = (float)(Random.value * 2.0 - 1.0) * (float)amplitude;
                    //data[i] = (float)(rng.NextDouble() * 2.0 - 1.0) * (float)amplitude;

                    //data[i] = (float)(rng.NextDouble() * 2.0 - 1.0);
                    sample = (float)(rng.NextDouble() * 2.0 - 1.0);
                    break;

            }

            data[i] = sample * audio_peak * 0.5f;
        }
    }

    //PolyBLEP - Valimaki & Huovilainen
    private static float PolyBLEP(float t, float dt)
    {
        if (t < dt)
        {
            t /= dt;
            return t + t - t * t - 1f;
        }
        else if (t > 1f - dt)
        {
            t = (t - 1f) / dt;
            return t * t + t + t + 1f;
        }
        return 0f;
    }

    public enum WaveformType
    {
        SIN,
        TRIANGLE,
        SAW,
        SQUARE,
        NOISE
    }
}
