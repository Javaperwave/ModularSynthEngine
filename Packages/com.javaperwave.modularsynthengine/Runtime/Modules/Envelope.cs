using System.Collections.Generic;
using UnityEngine;

public class Envelope : Module
{
    //ENTRADAS
    //Gate In
    //Trigger In
    //(OP) CV In

    //SALIDAS
    //CV Out

    //ADSR
    [Range(0.001f, 10f)]
    public float attackTime = 0.1f;

    [Range(0.001f, 10f)]
    public float decayTime = 0.01f;

    [Range(0f, 1f)]
    public float sustainLevel = 1f;

    [Range(0.001f, 10f)]
    public float releaseTime = 0.01f;

    private double sampling_freq;

    //Exponential curve control
    public float AttackCurve = 1f;
    public float DecayCurve = 1f;
    public float ReleaseCurve = 1f;

    private int attackSamples;
    private int decaySamples;
    private int releaseSamples;

    private int sampleCounter;

    private float envValue;

    private EnvelopeState state = EnvelopeState.IDLE;

    private float releaseFrom;

    private float attackFrom = 0f;

    private bool wasGateHigh;
    private bool wasTrigHigh;


    public override List<ModuleParameter> GetParameters()
    {
        return new List<ModuleParameter>
        {
            ModuleParameter.Knob("attack", "Attack", attackTime, 0.001f, 10f, v => { attackTime = v; UpdateSamples(); }, curve: ParameterCurve.LOGARITHMIC),
            ModuleParameter.Knob("decay", "Decay", decayTime, 0.001f, 10f, v => { decayTime = v; UpdateSamples(); }, curve: ParameterCurve.LOGARITHMIC),
            ModuleParameter.Knob("sustain", "Sustain", sustainLevel, 0f, 1f, v => sustainLevel = v),
            ModuleParameter.Knob("release", "Release", releaseTime, 0.001f, 10f, v => { releaseTime = v; UpdateSamples(); }, curve: ParameterCurve.LOGARITHMIC),
            ModuleParameter.Knob("attack_curve", "A Curve", 1f, 0.1f, 10f, v => AttackCurve = v, curve: ParameterCurve.LOGARITHMIC),
            ModuleParameter.Knob("decay_curve", "D Curve", 1f, 0.1f, 10f, v => DecayCurve = v, curve: ParameterCurve.LOGARITHMIC),
            ModuleParameter.Knob("release_curve", "R Curve", 1f, 0.1f, 10f, v => ReleaseCurve = v, curve: ParameterCurve.LOGARITHMIC),
        };
    }

    protected override void Initialize()
    {
        AddPort("gate_in", "GATE IN", PortType.GATE, PortDir.INPUT);
        AddPort("trig_in", "TRIG IN", PortType.TRIGGER, PortDir.INPUT);
        AddPort("cv_out", "CV OUT", PortType.MODCV, PortDir.OUTPUT);

        sampling_freq = AudioSettings.outputSampleRate;

        UpdateSamples();
    }

    private void UpdateSamples()
    {
        attackSamples = Mathf.Max(1, (int)(attackTime * sampling_freq));
        decaySamples = Mathf.Max(1, (int)(decayTime * sampling_freq));
        releaseSamples = Mathf.Max(1, (int)(releaseTime * sampling_freq));
    }


    public override float[] execute(float[] data, CV cv)
    {
        if (TryGetFrameCache(data.Length, out float[] cache))
        {
            System.Array.Copy(cache, data, data.Length);
            return data;
        }

        float[] gate = ReadInputPort("gate_in", data.Length);
        float[] trig = ReadInputPort("trig_in", data.Length);

        if (gate == null && trig == null)
        {
            System.Array.Clear(data, 0, data.Length);

            SaveToFrameCache(data);

            return data;
        }

        for (int i = 0; i < data.Length; i++)
        {
            float gateSample = (gate != null) ? gate[i] : CVStandard.GATE_LOW;
            float trigSample = (trig != null) ? trig[i] : CVStandard.GATE_LOW;

            AdvanceState(gateSample, trigSample);
            data[i] = envValue * CVStandard.UNIPOLAR_MAX;
        }

        SaveToFrameCache(data);

        return data;
    }

    private void AdvanceState(float gateSample, float trigSample)
    {
        bool isGateHigh = CVStandard.IsGateActive(gateSample);
        bool isTrigHigh = CVStandard.IsGateActive(trigSample);

        bool gateTriggered = isGateHigh && !wasGateHigh;
        bool trigTriggered = isTrigHigh && !wasTrigHigh;
        bool gateReleased = !isGateHigh && wasGateHigh;

        bool triggered = gateTriggered || trigTriggered;

        wasGateHigh = isGateHigh;
        wasTrigHigh = isTrigHigh;

        //float deltaTime = 1f / (float)sampling_freq;

        if (triggered)
        {
            attackFrom = envValue;
            state = EnvelopeState.ATTACK;
            sampleCounter = 0;
        }
        else if (gateReleased && state != EnvelopeState.RELEASE && state != EnvelopeState.IDLE)
        {
            state = EnvelopeState.RELEASE;
            sampleCounter = 0;
            releaseFrom = envValue;
        }

        switch (state)
        {
            case EnvelopeState.IDLE:
                //Debug.Log("Idle");
                envValue = 0f;
                break;

            case EnvelopeState.ATTACK:
                //Debug.Log("Attack");
                if (attackSamples <= 1)
                {
                    envValue = 1f;
                    state = EnvelopeState.DECAY;
                    sampleCounter = 0;
                }
                else
                {
                    float t = (float)sampleCounter / attackSamples;

                    envValue = Mathf.Lerp(attackFrom, 1f, ExponentialAttack(t, AttackCurve));

                    sampleCounter++;

                    if (sampleCounter >= attackSamples)
                    {
                        envValue = 1f;
                        state = EnvelopeState.DECAY;
                        sampleCounter = 0;
                    }
                }
                break;

            case EnvelopeState.DECAY:
                //Debug.Log("Decay");
                if (decaySamples <= 1)
                {
                    envValue = sustainLevel;
                    state = EnvelopeState.SUSTAIN;
                }
                else
                {
                    float t = (float)sampleCounter / decaySamples;

                    envValue = ExponentialDecay(t, DecayCurve, 1f, sustainLevel);

                    sampleCounter++;

                    if (sampleCounter >= decaySamples)
                    {
                        envValue = sustainLevel;
                        state = EnvelopeState.SUSTAIN;
                    }
                }
                break;

            case EnvelopeState.SUSTAIN:
                //Debug.Log("Sustain");
                envValue = sustainLevel;

                if (!isGateHigh)
                {
                    state = EnvelopeState.RELEASE;
                    sampleCounter = 0;
                    releaseFrom = envValue;
                }

                break;

            case EnvelopeState.RELEASE:
                //Debug.Log("Release");
                if (releaseSamples <= 1)
                {
                    envValue = 0f;
                    state = EnvelopeState.IDLE;
                }
                else
                {
                    float t = (float)sampleCounter / releaseSamples;

                    envValue = ExponentialDecay(t, ReleaseCurve, releaseFrom, 0f);

                    sampleCounter++;

                    if (envValue <= 0.0001f || sampleCounter >= releaseSamples)
                    {
                        envValue = 0f;
                        state = EnvelopeState.IDLE;
                        sampleCounter = 0;
                    }
                }
                break;
        }

        envValue = Mathf.Clamp01(envValue);
    }


    private float ExponentialAttack(float t, float curve)
    {
        //1 - e^(-t/curve) normalizaded
        return (1f - Mathf.Exp(-t / curve)) / (1f - Mathf.Exp(-1f / curve));
    }


    private float ExponentialDecay(float t, float curve, float from, float to)
    {
        float factor = (1f - Mathf.Exp(-t / curve)) / (1f - Mathf.Exp(-1f / curve));

        return Mathf.Lerp(from, to, factor);
    }


    public void Trigger()
    {
        attackFrom = envValue;
        state = EnvelopeState.ATTACK;
        sampleCounter = 0;
    }

    public void Reset()
    {
        state = EnvelopeState.IDLE;
        envValue = 0f;
        attackFrom = 0f;
        sampleCounter = 0;
        wasGateHigh = false;
        wasTrigHigh = false;
    }


    private enum EnvelopeState
    {
        IDLE,
        ATTACK,
        DECAY,
        SUSTAIN,
        RELEASE
    }
}