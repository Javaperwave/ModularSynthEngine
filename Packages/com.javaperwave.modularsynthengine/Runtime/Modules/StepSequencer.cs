using System.Collections.Generic;
using UnityEngine;

public class StepSequencer : Module
{
    //ENTRADAS
    //Clock - Controla tiempo/paso del secuenciador
    //Reset/Trigger - Reciniciar secuenciador a inicio
    //(OP) CV In - Modular parametros como la velocidad

    //SALIDAS
    //CV Out - Valores de pasos del secuenciador
    //Gate Out - Nota activa o inactiva
    //Trigger Out - Seal de inicio en cada paso

    public int steps = 16;
    public float bpm = 120f;

    //[Range(-3f, 7f)] 
    public float[] stepCVs; //CV values for each step
    public bool[] stepActive;
    public bool[] slide; //TB-303

    [Range(0f, 1f)]
    public float glide = 0f; //Glide duration

    [Range(0.05f, 1f)]
    public float gateLength = 0.5f;

    private int currentStep = 0;
    private int previousStep = 0;

    private double samplePosition = 0.0;
    private double samplesPerStep;
    private double sampleRate;

    private double externalClockPeriodSamples;
    private double samplesSinceLastRisingEdge = 0.0;

    private int[] stepAtSample;
    private int[] prevStepAtSample;
    private float[] phaseAtSample;

    private double lastDspTime = -1.0;

    private bool wasClockHigh = false;
    private bool wasResetHigh = false;

    private float lastActiveCV = 0f;

    private const float SLIDE_TIME_CONSTANT = 0.06f; //~60ms fijo
    private float slideCV = 0f;


    protected override void Initialize()
    {
        sampleRate = AudioSettings.outputSampleRate;

        stepCVs = new float[steps];
        stepActive = new bool[steps];

        for (int i = 0; i < steps; i++)
        {
            stepCVs[i] = 0f;
            stepActive[i] = true;
        }

        //slide se controla desde el inspector: solo se inicializa si no existe
        if (slide == null || slide.Length != steps)
            slide = new bool[steps];

        AddPort("clock_in", "CLOCK IN", PortType.GATE, PortDir.INPUT);
        AddPort("reset_in", "RESET IN", PortType.TRIGGER, PortDir.INPUT);
        AddPort("cv_out", "CV OUT", PortType.PITCHCV, PortDir.OUTPUT);
        AddPort("gate_out", "GATE OUT", PortType.GATE, PortDir.OUTPUT);
        AddPort("trig_out", "TRIG OUT", PortType.TRIGGER, PortDir.OUTPUT);

        UpdateTimingParameters();

        externalClockPeriodSamples = samplesPerStep;
    }

    public override List<ModuleParameter> GetParameters()
    {
        var list = new List<ModuleParameter>
        {
            ModuleParameter.Knob("bpm", "BPM", bpm, 10f, 300f, v => { bpm = v; UpdateTimingParameters(); }, curve: ParameterCurve.LOGARITHMIC),
            //ModuleParameter.Knob("steps", "Steps", steps, 1f, 32f, v => SetStepCount(Mathf.RoundToInt(v)), isInt: true),
            ModuleParameter.Knob("glide", "Glide", glide, 0f, 1f, v => glide = v),
            ModuleParameter.Knob("gatelen", "Gate Len", gateLength, 0.05f, 1f, v => gateLength = v),
        };

        for (int i = 0; i < steps; i++)
        {
            int idx = i;
            list.Add(ModuleParameter.Group($"step_{idx}",
                ModuleParameter.Toggle($"step_{idx}_active", $"S{idx + 1}", stepActive[idx], v => stepActive[idx] = v),
                ModuleParameter.Slider($"step_{idx}_cv", $"CV", stepCVs[idx], CVStandard.PITCH_CV_MIN, CVStandard.PITCH_CV_MAX, v => stepCVs[idx] = v),
                ModuleParameter.Toggle($"step_{idx}_slide", $"SLD", slide[idx], v => slide[idx] = v, hidden: true)
            ));
        }

        return list;
    }


    private void UpdateTimingParameters()
    {
        double beatsPerSecond = bpm / 60.0;
        double stepsPerBeat = 4.0;
        samplesPerStep = sampleRate / (beatsPerSecond * stepsPerBeat);
    }


    /*
    private void SetStepCount(int newCount)
    {
        newCount = Mathf.Clamp(newCount, 1, 32);
        if (newCount == steps) return;

        float[] newCVs = new float[newCount];
        bool[] newActive = new bool[newCount];
        for (int i = 0; i < newCount; i++)
        {
            newCVs[i] = i < steps ? stepCVs[i] : 0f;
            newActive[i] = i < steps ? stepActive[i] : true;
        }

        steps = newCount;
        stepCVs = newCVs;
        stepActive = newActive;
        currentStep = currentStep % steps;
    }
    */


    public override float[] execute(float[] data, CV cv)
    {
        double currentDspTime = AudioSettings.dspTime;

        if (lastDspTime != currentDspTime)
        {
            PrecomputeBlock(data.Length);
            lastDspTime = currentDspTime;
        }

        switch (cv.portId)
        {
            case "cv_out":
                return generateCVOutput(data);

            case "gate_out":
                return generateGateOutput(data);

            case "trig_out":
                return generateTriggerOutput(data);

            default:
                return data;
        }
    }

    private void PrecomputeBlock(int bufferSize)
    {
        if (stepAtSample == null || stepAtSample.Length != bufferSize)
        {
            stepAtSample = new int[bufferSize];
            prevStepAtSample = new int[bufferSize];
            phaseAtSample = new float[bufferSize];
        }

        float[] clockSignal = ReadInputPort("clock_in", bufferSize);
        float[] resetSignal = ReadInputPort("reset_in", bufferSize);

        bool useExternalClock = clockSignal != null;

        int tempCurrent = currentStep;
        int tempPrevious = previousStep;
        double tempPos = samplePosition;
        bool tempClockHigh = wasClockHigh;
        bool tempWasReset = wasResetHigh;
        double tempExternalPeriod = externalClockPeriodSamples;
        double tempClockCounter = samplesSinceLastRisingEdge;

        for (int i = 0; i < bufferSize; i++)
        {
            bool resetHigh = resetSignal != null && CVStandard.IsGateActive(resetSignal[i]);

            if (resetHigh && !tempWasReset)
            {
                tempCurrent = 0;
                tempPrevious = steps - 1;
                tempPos = 0.0;
                tempClockHigh = false;
                tempClockCounter = 0.0;
            }

            tempWasReset = resetHigh;

            stepAtSample[i] = tempCurrent;
            prevStepAtSample[i] = tempPrevious;

            double periodForPhase = useExternalClock ? tempExternalPeriod : samplesPerStep;

            phaseAtSample[i] = Mathf.Clamp01((float)(tempPos / periodForPhase));

            if (useExternalClock)
            {
                bool isHigh = CVStandard.IsGateActive(clockSignal[i]);
                if (isHigh && !tempClockHigh)
                {
                    if (tempClockCounter > 0)
                        tempExternalPeriod = tempClockCounter;
                    tempClockCounter = 0.0;

                    tempPrevious = tempCurrent;
                    tempCurrent = (tempCurrent + 1) % steps;
                    tempPos = 0.0;
                }
                else
                {
                    tempPos += 1.0;
                    tempClockCounter += 1.0;
                }
                tempClockHigh = isHigh;
            }
            else
            {
                tempPos += 1.0;
                if (tempPos >= samplesPerStep)
                {
                    tempPrevious = tempCurrent;
                    tempCurrent = (tempCurrent + 1) % steps;
                    tempPos -= samplesPerStep;
                }
            }
        }

        currentStep = tempCurrent;
        previousStep = tempPrevious;
        samplePosition = tempPos;
        wasClockHigh = tempClockHigh;
        wasResetHigh = tempWasReset;
        externalClockPeriodSamples = tempExternalPeriod;
        samplesSinceLastRisingEdge = tempClockCounter;
    }

    private float[] generateCVOutput(float[] data)
    {
        float slideCoeff = 1f - Mathf.Exp(-1f / (SLIDE_TIME_CONSTANT * (float)sampleRate));

        for (int i = 0; i < data.Length; i++)
        {
            int step = stepAtSample[i];

            if (!stepActive[step]) { data[i] = lastActiveCV; continue; }

            if (glide < 0.001f)
            {
                data[i] = stepCVs[step];
                //Debug.Log(data[i]);
            }
            else
            {
                float prevCV = stepActive[prevStepAtSample[i]] ? stepCVs[prevStepAtSample[i]] : lastActiveCV;
                float glideFrac = Mathf.Clamp01(phaseAtSample[i] / glide);
                data[i] = Mathf.Lerp(prevCV, stepCVs[step], glideFrac);
            }

            if (slide[prevStepAtSample[i]])
            {
                slideCV += (stepCVs[step] - slideCV) * slideCoeff;
                data[i] = slideCV;
            }
            else
            {
                slideCV = data[i];
            }

            lastActiveCV = data[i];
        }
        return data;
    }

    private float[] generateGateOutput(float[] data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            int step = stepAtSample[i];

            bool gateOn = stepActive[step] && (gateLength >= 1f || phaseAtSample[i] < gateLength);

            if (slide[step] && stepActive[step]) gateOn = true;

            data[i] = gateOn ? CVStandard.GATE_HIGH : CVStandard.GATE_LOW;
        }
        return data;
    }

    private float[] generateTriggerOutput(float[] data)
    {
        double triggerDurationMs = CVStandard.TRIGGER_TYPICAL_DURATION_MS;
        int triggerDurationSamples = (int)((triggerDurationMs / 1000.0) * sampleRate);

        for (int i = 0; i < data.Length; i++)
        {
            int step = stepAtSample[i];
            float phase = phaseAtSample[i];

            int samplesIntoStep = (int)(phase * samplesPerStep);

            if (stepActive[step] && samplesIntoStep < triggerDurationSamples)
            {
                data[i] = CVStandard.GATE_HIGH;
            }
            else
            {
                data[i] = CVStandard.GATE_LOW;
            }

            //TB-303 slide: la nota ligada (paso anterior con slide) no re-dispara la envolvente
            if (slide[prevStepAtSample[i]]) data[i] = CVStandard.GATE_LOW;
        }

        return data;
    }

    public void Reset()
    {
        currentStep = 0;
        previousStep = steps - 1;
        samplePosition = 0.0;
        wasClockHigh = false;
        wasResetHigh = false;
    }

    public void SetStepCV(int step, float cv)
    {
        if (step >= 0 && step < steps)
            stepCVs[step] = Mathf.Clamp(cv, CVStandard.PITCH_CV_MIN, CVStandard.PITCH_CV_MAX);
    }

    public void SetStepActive(int step, bool active)
    {
        if (step >= 0 && step < steps)
            stepActive[step] = active;
    }

    public int CurrentStep => currentStep;

}